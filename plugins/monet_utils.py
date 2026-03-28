import ctypes
import os
import sys
from PySide6.QtCore import Qt
from PySide6.QtGui import QImage, QColor

def get_wallpaper_path():
    try:
        if sys.platform != "win32":
            return None
        SPI_GETDESKWALLPAPER = 0x0073
        path = ctypes.create_unicode_buffer(260)
        ctypes.windll.user32.SystemParametersInfoW(SPI_GETDESKWALLPAPER, 260, path, 0)
        p = path.value
        if os.path.exists(p):
            return p
    except Exception:
        pass
    return None

def extract_colors(image_path):
    if not image_path:
        return None
    try:
        img = QImage(image_path)
        if img.isNull():
            return None
        
        # Scale down for performance
        small = img.scaled(100, 100, aspectRatioMode=Qt.KeepAspectRatio)
        width = small.width()
        height = small.height()
        
        # Get dominant color instead of simple average
        # We'll use a simple bucketed approach for better "Monet" feel
        color_counts = {}
        for y in range(0, height, 2): # Sample every 2nd pixel
            for x in range(0, width, 2):
                c = small.pixelColor(x, y)
                if c.alpha() < 128: continue
                # Quantize color to find dominant hue
                h, s, l, _ = c.getHsl()
                if s < 20 or l < 30 or l > 220: continue # Skip desaturated or extreme colors
                
                # Round hue to nearest 10 for bucketing
                bucket = (h // 10) * 10
                color_counts[bucket] = color_counts.get(bucket, 0) + 1

        if not color_counts:
            # Fallback to average if no colorful pixels found
            r_sum = g_sum = b_sum = count = 0
            for y in range(height):
                for x in range(width):
                    c = small.pixelColor(x, y)
                    r_sum += c.red()
                    g_sum += c.green()
                    b_sum += c.blue()
                    count += 1
            if count == 0: return None
            primary = QColor(r_sum // count, g_sum // count, b_sum // count)
        else:
            # Find the most frequent hue bucket
            best_hue = max(color_counts, key=color_counts.get)
            # Find average of colors in that bucket
            r_sum = g_sum = b_sum = count = 0
            for y in range(height):
                for x in range(width):
                    c = small.pixelColor(x, y)
                    if (c.hue() // 10) * 10 == best_hue:
                        r_sum += c.red()
                        g_sum += c.green()
                        b_sum += c.blue()
                        count += 1
            primary = QColor(r_sum // count, g_sum // count, b_sum // count)

        # Implementation of Monet-style weighting
        # Android Monet logic: Tonal palettes (L* value based)
        # We use HSL as a proxy for CIELAB for simplicity in this environment
        
        def get_monet_colors(base_color, is_dark):
            h, s, l, _ = base_color.getHsl()
            
            # Boost saturation slightly for better accent feel if too dull
            s = max(s, 60) if s > 10 else s 
            
            if is_dark:
                # Dark mode: Lighten the accent, darken the background
                # Accent needs to be vibrant enough (Lightness ~70-80)
                accent_l = max(160, min(200, l + 50)) 
                accent = QColor.fromHsl(h, s, accent_l).name()
                
                # Background: Very dark, derived from primary hue
                # Low saturation (chroma) for background surfaces
                bg = QColor.fromHsl(h, min(s, 20), 20).name()
                surface = QColor.fromHsl(h, min(s, 15), 30).name()
                text = "#FFFFFF"
            else:
                # Light mode: Darken the accent, lighten the background
                # Accent needs to be visible (Lightness ~30-50)
                accent_l = max(80, min(130, l - 40 if l > 128 else l))
                accent = QColor.fromHsl(h, s, accent_l).name()
                
                # Background: Very light
                bg = QColor.fromHsl(h, min(s, 15), 242).name()
                surface = "#FFFFFF"
                text = "#000000"
                
            return accent, bg, surface, text

        accent_l, bg_l, surf_l, text_l = get_monet_colors(primary, False)
        accent_d, bg_d, surf_d, text_d = get_monet_colors(primary, True)

        return {
            "light": {
                "primary": accent_l,
                "background": bg_l,
                "surface": surf_l,
                "text": text_l
            },
            "dark": {
                "primary": accent_d,
                "background": bg_d,
                "surface": surf_d,
                "text": text_d
            },
            "seed": primary.name() # Keep the original for reference
        }
    except Exception:
        return None
