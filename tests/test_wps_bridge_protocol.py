import json
import unittest

from jsonschema import Draft202012Validator

from ppt_assistant.core.wps_bridge import ProtocolError, WpsBridgeProtocol


class WpsBridgeProtocolTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.protocol = WpsBridgeProtocol()

    def test_all_schemas_are_valid_draft_2020_12(self):
        for schema in self.protocol.schemas.values():
            Draft202012Validator.check_schema(schema)
        self.protocol.check_all_schemas()

    def test_discovers_protocol_message_types(self):
        self.assertIn("hello", self.protocol.message_types)
        self.assertIn("presentation_state", self.protocol.message_types)
        self.assertIn("presentation_next", self.protocol.message_types)
        self.assertIn("command_result", self.protocol.message_types)

    def test_hello_round_trip(self):
        raw = self.protocol.encode(
            "hello",
            {
                "plugin_version": "1.0.0",
                "protocol_version": 1,
                "host": {"platform": "linux", "app": "wps-presentation"},
                "capabilities": {
                    "can_next": True,
                    "can_prev": True,
                    "can_goto": True,
                    "can_get_state": True,
                    "can_set_pen_color": True,
                    "can_get_thumbnails": True,
                },
            },
            message_id="hello-1",
            ts=123,
        )
        message = self.protocol.decode(raw)
        self.assertEqual(message["message_type"], "hello")
        self.assertEqual(message["message_id"], "hello-1")
        self.assertEqual(message["payload"]["host"]["platform"], "linux")

    def test_presentation_state_round_trip(self):
        raw = self.protocol.encode(
            "presentation_state",
            {
                "document": {"name": "deck.pptx", "slide_count": 3},
                "presentation": {
                    "current_slide": 2,
                    "is_slide_show": True,
                    "pointer_type": "pen",
                    "pen_color": "#3366FF",
                },
            },
        )
        message = self.protocol.decode(raw)
        self.assertEqual(message["payload"]["presentation"]["current_slide"], 2)

    def test_command_messages_round_trip(self):
        commands = {
            "presentation_next": {},
            "presentation_prev": {},
            "presentation_state_get": {},
            "presentation_goto": {"slide_index": 2},
            "presentation_pen_color_set": {"color": "#FF0000"},
            "presentation_thumbnails_get": {
                "range": "all",
                "format": "png",
                "max_width": 320,
            },
        }
        for message_type, payload in commands.items():
            with self.subTest(message_type=message_type):
                raw = self.protocol.encode(message_type, payload)
                self.assertEqual(
                    self.protocol.decode(raw)["message_type"], message_type
                )

    def test_command_result_round_trip(self):
        raw = self.protocol.encode(
            "command_result",
            {"command_id": "cmd-1", "ok": True, "code": "OK"},
        )
        self.assertEqual(self.protocol.decode(raw)["payload"]["code"], "OK")

    def test_rejects_unknown_message_type(self):
        with self.assertRaises(ProtocolError):
            self.protocol.decode(json.dumps({"message_type": "wat", "payload": {}}))

    def test_rejects_bad_color(self):
        with self.assertRaises(ProtocolError):
            self.protocol.encode(
                "presentation_pen_color_set",
                {"color": "red"},
            )

    def test_rejects_non_object_json(self):
        with self.assertRaises(ProtocolError):
            self.protocol.decode("[]")


if __name__ == "__main__":
    unittest.main()
