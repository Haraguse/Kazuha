from __future__ import annotations

import json
import sys
import time
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from jsonschema import Draft202012Validator
from jsonschema.exceptions import ValidationError
from referencing import Registry, Resource


class ProtocolError(ValueError):
    """Raised when a WPS bridge message cannot be parsed or validated."""


@dataclass(frozen=True)
class MessageSchema:
    name: str
    path: Path
    schema: dict[str, Any]
    validator: Draft202012Validator


def default_protocol_dir() -> Path:
    candidates: list[Path] = []
    if getattr(sys, "frozen", False):
        meipass = getattr(sys, "_MEIPASS", None)
        if meipass:
            candidates.append(Path(meipass) / "Luminalium2WPS" / "protocol")
        candidates.append(Path(sys.executable).resolve().parent / "Luminalium2WPS" / "protocol")

    candidates.append(Path(__file__).resolve().parents[3] / "Luminalium2WPS" / "protocol")
    for candidate in candidates:
        if candidate.is_dir():
            return candidate
    return candidates[-1]


class WpsBridgeProtocol:
    """Schema-backed JSON codec for the Luminalium2WPS WebSocket protocol."""

    def __init__(self, protocol_dir: str | Path | None = None):
        self.protocol_dir = Path(protocol_dir) if protocol_dir is not None else default_protocol_dir()
        self._schemas = self._load_schemas(self.protocol_dir)
        self._registry = self._build_registry(self._schemas)
        self._message_schemas = self._build_message_schemas()

    @property
    def message_types(self) -> tuple[str, ...]:
        return tuple(sorted(self._message_schemas))

    @property
    def schemas(self) -> dict[str, dict[str, Any]]:
        return dict(self._schemas)

    def new_message_id(self) -> str:
        return uuid.uuid4().hex

    def make_message(
        self,
        message_type: str,
        payload: dict[str, Any] | None = None,
        *,
        message_id: str | None = None,
        ts: int | None = None,
    ) -> dict[str, Any]:
        message = {
            "message_type": str(message_type),
            "message_id": message_id or self.new_message_id(),
            "ts": int(ts if ts is not None else time.time() * 1000),
            "payload": payload or {},
        }
        self.validate(message)
        return message

    def encode(
        self,
        message_type: str,
        payload: dict[str, Any] | None = None,
        *,
        message_id: str | None = None,
        ts: int | None = None,
    ) -> str:
        message = self.make_message(
            message_type,
            payload,
            message_id=message_id,
            ts=ts,
        )
        return json.dumps(message, ensure_ascii=False, separators=(",", ":"))

    def decode(self, raw_text: str | bytes) -> dict[str, Any]:
        try:
            if isinstance(raw_text, bytes):
                raw_text = raw_text.decode("utf-8")
            message = json.loads(raw_text)
        except Exception as exc:
            raise ProtocolError(f"Invalid JSON: {exc}") from exc
        if not isinstance(message, dict):
            raise ProtocolError("Message must be a JSON object")
        self.validate(message)
        return message

    def validate(self, message: dict[str, Any]) -> None:
        message_type = message.get("message_type")
        if not isinstance(message_type, str):
            raise ProtocolError("Message is missing string field 'message_type'")

        schema = self._message_schemas.get(message_type)
        if schema is None:
            raise ProtocolError(f"Unknown message_type: {message_type}")

        errors = sorted(schema.validator.iter_errors(message), key=lambda err: err.path)
        if errors:
            raise ProtocolError(self._format_errors(errors))

    def check_all_schemas(self) -> None:
        for schema in self._schemas.values():
            Draft202012Validator.check_schema(schema)

    def _load_schemas(self, protocol_dir: Path) -> dict[str, dict[str, Any]]:
        if not protocol_dir.is_dir():
            raise ProtocolError(f"WPS bridge protocol directory not found: {protocol_dir}")

        schemas: dict[str, dict[str, Any]] = {}
        for path in sorted(protocol_dir.glob("*.schema.json")):
            try:
                with path.open("r", encoding="utf-8") as file:
                    schema = json.load(file)
            except Exception as exc:
                raise ProtocolError(f"Failed to load schema {path}: {exc}") from exc
            if not isinstance(schema, dict):
                raise ProtocolError(f"Schema must be a JSON object: {path}")
            schemas[path.name] = schema
        if not schemas:
            raise ProtocolError(f"No schema files found in {protocol_dir}")
        return schemas

    def _build_registry(self, schemas: dict[str, dict[str, Any]]) -> Registry:
        resources: list[tuple[str, Resource]] = []
        for name, schema in schemas.items():
            resource = Resource.from_contents(schema)
            resources.append((name, resource))
            resources.append((f"./{name}", resource))
        return Registry().with_resources(resources)

    def _build_message_schemas(self) -> dict[str, MessageSchema]:
        message_schemas: dict[str, MessageSchema] = {}
        for name, schema in self._schemas.items():
            if name == "common.schema.json":
                continue
            message_types = sorted(set(self._iter_message_types(schema)))
            if not message_types:
                continue
            for message_type in message_types:
                if message_type in message_schemas:
                    other = message_schemas[message_type].name
                    raise ProtocolError(
                        f"Duplicate schema for message_type {message_type}: {other}, {name}"
                    )
                message_schemas[message_type] = MessageSchema(
                    name=name,
                    path=self.protocol_dir / name,
                    schema=schema,
                    validator=Draft202012Validator(schema, registry=self._registry),
                )
        if not message_schemas:
            raise ProtocolError("No message schemas could be discovered")
        return message_schemas

    def _iter_message_types(self, node: Any):
        if isinstance(node, dict):
            message_type = node.get("properties", {}).get("message_type")
            if isinstance(message_type, dict):
                const = message_type.get("const")
                if isinstance(const, str):
                    yield const
                enum = message_type.get("enum")
                if isinstance(enum, list):
                    for value in enum:
                        if isinstance(value, str):
                            yield value
            for value in node.values():
                yield from self._iter_message_types(value)
        elif isinstance(node, list):
            for value in node:
                yield from self._iter_message_types(value)

    def _format_errors(self, errors: list[ValidationError]) -> str:
        parts = []
        for error in errors[:5]:
            location = error.json_path or "$"
            parts.append(f"{location}: {error.message}")
        if len(errors) > 5:
            parts.append(f"... {len(errors) - 5} more validation errors")
        return "; ".join(parts)
