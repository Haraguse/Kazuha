from __future__ import annotations

from typing import Any

from PySide6.QtCore import QObject, Signal
from PySide6.QtNetwork import QHostAddress
from PySide6.QtWebSockets import QWebSocket, QWebSocketServer

from .protocol import ProtocolError, WpsBridgeProtocol


class WpsBridgeHost(QObject):
    connected = Signal()
    disconnected = Signal()
    message_received = Signal(dict)
    log_message = Signal(str)
    protocol_error = Signal(str)

    def __init__(
        self,
        protocol: WpsBridgeProtocol | None = None,
        *,
        path: str = "/ws",
        parent: QObject | None = None,
    ):
        super().__init__(parent)
        self.protocol = protocol or WpsBridgeProtocol()
        self.path = path
        self._server: QWebSocketServer | None = None
        self._client: QWebSocket | None = None
        self._port = 0

    @property
    def port(self) -> int:
        return int(self._port or 0)

    def is_running(self) -> bool:
        return self._server is not None and self._server.isListening()

    def is_connected(self) -> bool:
        return self._client is not None and self._client.isValid()

    def start(self, port_start: int = 3892, port_end: int = 3902) -> bool:
        if self.is_running():
            return True

        server = QWebSocketServer(
            "Luminalium WPS Bridge",
            QWebSocketServer.SslMode.NonSecureMode,
            self,
        )
        server.newConnection.connect(self._on_new_connection)
        address = QHostAddress(QHostAddress.SpecialAddress.LocalHost)

        for port in range(int(port_start), int(port_end) + 1):
            if server.listen(address, port):
                self._server = server
                self._port = int(port)
                self.log_message.emit(
                    f"WPS bridge host listening on ws://127.0.0.1:{port}{self.path}"
                )
                return True

        message = server.errorString()
        server.deleteLater()
        self.protocol_error.emit(f"WPS bridge host failed to listen: {message}")
        return False

    def stop(self) -> None:
        self._close_client()
        if self._server is not None:
            self._server.close()
            self._server.deleteLater()
            self._server = None
        self._port = 0

    def send(
        self,
        message_type: str,
        payload: dict[str, Any] | None = None,
        *,
        message_id: str | None = None,
    ) -> str:
        if self._client is None:
            raise ProtocolError("WPS bridge is not connected")
        message_id = message_id or self.protocol.new_message_id()
        raw = self.protocol.encode(message_type, payload or {}, message_id=message_id)
        self._client.sendTextMessage(raw)
        return message_id

    def _on_new_connection(self) -> None:
        if self._server is None:
            return

        socket = self._server.nextPendingConnection()
        if socket is None:
            return

        request_path = socket.requestUrl().path()
        if request_path != self.path:
            self.protocol_error.emit(
                f"Rejected WPS bridge connection for path {request_path!r}"
            )
            socket.close()
            socket.deleteLater()
            return

        if self._client is not None and self._client is not socket:
            self.log_message.emit("Replacing existing WPS bridge client")
            self._close_client()

        self._client = socket
        socket.textMessageReceived.connect(self._on_text_message)
        socket.disconnected.connect(self._on_client_disconnected)
        self.log_message.emit("WPS bridge client connected")
        self.connected.emit()

    def _on_text_message(self, raw_text: str) -> None:
        try:
            message = self.protocol.decode(raw_text)
        except ProtocolError as exc:
            text = str(exc)
            self.protocol_error.emit(text)
            self._send_protocol_error(text)
            return
        self.message_received.emit(message)

    def _on_client_disconnected(self) -> None:
        socket = self.sender()
        if socket is self._client:
            self._client = None
            self.log_message.emit("WPS bridge client disconnected")
            self.disconnected.emit()
        if isinstance(socket, QWebSocket):
            socket.deleteLater()

    def _close_client(self) -> None:
        if self._client is None:
            return
        socket = self._client
        self._client = None
        try:
            socket.close()
        finally:
            socket.deleteLater()
            self.disconnected.emit()

    def _send_protocol_error(self, message: str) -> None:
        if self._client is None:
            return
        try:
            self.send(
                "error",
                {
                    "code": "INVALID_STATE",
                    "message": "Invalid WPS bridge message",
                    "detail": {"validation_error": message},
                },
            )
        except Exception:
            pass
