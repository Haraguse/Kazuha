import os
import unittest

from ppt_assistant.ui.linux_qml_overlay import _qml_color, _thumbnail_source_to_url


class LinuxQmlOverlayHelperTests(unittest.TestCase):
    def test_qml_color_converts_css_rgba_to_argb_hex(self):
        self.assertEqual(_qml_color("rgba(0, 0, 0, 0.12)"), "#1F000000")
        self.assertNotEqual(_qml_color("rgba(0, 0, 0, 0.12)"), "#FF000000")

    def test_qml_color_converts_css_rgb_and_hex(self):
        self.assertEqual(_qml_color("rgb(255, 255, 255)"), "#FFFFFF")
        self.assertEqual(_qml_color("#ffffff"), "#FFFFFF")

    def test_qml_color_uses_normalized_fallback_for_invalid_values(self):
        self.assertEqual(_qml_color("not-a-color", "rgba(255, 0, 0, 0.5)"), "#80FF0000")
        self.assertEqual(_qml_color("not-a-color", "also-not-a-color"), "#000000")

    def test_thumbnail_source_url_keeps_bridge_urls(self):
        for source in (
            "data:image/png;base64,abc",
            "file:///tmp/slide.png",
            "https://example.test/slide.png",
            "blob:slide-thumb",
        ):
            with self.subTest(source=source):
                self.assertEqual(_thumbnail_source_to_url(source), source)

    def test_thumbnail_source_url_converts_plain_paths(self):
        path = os.path.abspath(os.path.join("tmp", "slide.png"))
        url = _thumbnail_source_to_url(path)
        self.assertTrue(url.startswith("file:"))
        self.assertIn("slide.png", url)


if __name__ == "__main__":
    unittest.main()
