import os
from PySide6.QtGui import QFont, QPixmap, QPainter, QColor, QFontMetrics


def apply(splash):
    current_dir = os.path.dirname(os.path.abspath(__file__))
    bg_path = os.path.join(current_dir, "1.4_Splash.png")

    _pixmap = None
    if os.path.exists(bg_path):
        original_pixmap = QPixmap(bg_path)
        if not original_pixmap.isNull():
            _pixmap = original_pixmap
            target_width = 860
            aspect_ratio = original_pixmap.height() / original_pixmap.width()
            target_height = int(target_width * aspect_ratio)
            splash.resize(target_width, target_height)
        else:
            splash.resize(860, 480)
    else:
        splash.resize(860, 480)

    splash._pixmap_momokan = _pixmap

    original_paint_event = splash.paintEvent

    def custom_paint_event(event):
        pixmap = getattr(splash, "_pixmap_momokan", None)
        if pixmap and not pixmap.isNull():
            painter = QPainter(splash)
            painter.setRenderHint(QPainter.Antialiasing)
            painter.setRenderHint(QPainter.TextAntialiasing)
            painter.setRenderHint(QPainter.SmoothPixmapTransform)

            painter.drawPixmap(splash.rect(), pixmap)

            # Scale factor from original image to window
            scale = splash.width() / pixmap.width() if pixmap.width() > 0 else 1.0

            # Anchor point in image coordinates (287, 120) -> window coordinates
            anchor_x = int(287 * scale)
            anchor_y = int(120 * scale)

            title_font = QFont()
            title_font.setStyleHint(QFont.SansSerif)
            title_font.setPixelSize(28)
            title_font.setBold(True)

            sub_font = QFont()
            sub_font.setStyleHint(QFont.SansSerif)
            sub_font.setPixelSize(11)

            w = splash.width()

            content_width = w * 0.65

            painter.setFont(title_font)
            fm_title = QFontMetrics(title_font)
            title_height = fm_title.capHeight()

            painter.setFont(sub_font)
            fm_sub = QFontMetrics(sub_font)
            sub_height = fm_sub.capHeight()

            # Anchor is top-left of the text block
            title_baseline_y = anchor_y + title_height
            subtitle_baseline_y = title_baseline_y + 10 + sub_height

            painter.setFont(title_font)
            painter.setPen(QColor("#ffffff"))
            brand_name_map = {
                "zh-CN": "Luminalium",
                "zh-TW": "Luminalium",
                "yue-HK": "Luminalium",
                "ja-JP": "ルマイナリウム",
                "en-US": "Luminalium",
            }
            brand_name = brand_name_map.get(
                getattr(splash, "_language", ""), "Luminalium"
            )
            painter.drawText(anchor_x, title_baseline_y, brand_name)

            painter.setFont(sub_font)
            painter.setPen(QColor("#cccccc"))
            code_name = getattr(splash, "_code_name_en", "") or ""
            subtitle = f"{splash._version_text} // {code_name}"
            painter.drawText(anchor_x, subtitle_baseline_y, subtitle)

            painter.end()
        else:
            original_paint_event(event)

    splash.paintEvent = custom_paint_event

    original_set_progress = splash.set_progress

    def custom_set_progress(value, text_key="initializing"):
        value = min(max(value, 0), 100)
        splash._progress_value = value
        # Localize status text
        try:
            import main as _main_mod
            i18n = getattr(_main_mod, "SPLASH_I18N", {})
            lang = getattr(splash, "_language", "zh-CN")
            localized = i18n.get(lang, i18n.get("zh-CN", {})).get(text_key, text_key)
        except Exception:
            localized = text_key
        splash._status_text = localized
        splash.update()

    splash.set_progress = custom_set_progress
