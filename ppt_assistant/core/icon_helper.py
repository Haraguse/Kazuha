import os
import io
import base64
import mimetypes
from PIL import Image
from PySide6.QtGui import QIcon, QPixmap, QImage
from PySide6.QtCore import QBuffer, QIODevice, QSize, QFileInfo
from PySide6.QtWidgets import QFileIconProvider

# Try importing mutagen for audio metadata
try:
    import mutagen
    from mutagen.id3 import ID3, APIC
    from mutagen.flac import FLAC, Picture
    from mutagen.mp4 import MP4, MP4Cover
    HAS_MUTAGEN = True
except ImportError:
    HAS_MUTAGEN = False

# Try importing pywin32 for system thumbnails
try:
    import pythoncom
    from win32com.shell import shell, shellcon
    import win32gui
    import win32ui
    import win32con
    HAS_WIN32 = True
except ImportError:
    HAS_WIN32 = False

def get_file_icon_base64(path: str) -> str | None:
    """
    Get the base64 encoded icon/thumbnail for a file.
    - Audio: Album art
    - Video: Thumbnail
    - Exe/Others: System Icon
    """
    if not os.path.exists(path):
        return None

    mime_type, _ = mimetypes.guess_type(path)
    if mime_type:
        if mime_type.startswith('audio/'):
            icon = get_audio_cover(path)
            if icon: return icon
        elif mime_type.startswith('video/'):
            # Try to get thumbnail for video
            if HAS_WIN32:
                icon = get_windows_thumbnail(path)
                if icon: return icon

    # Fallback to system icon (exe, or failed audio/video extraction)
    return get_system_icon(path)

def get_audio_cover(path: str) -> str | None:
    if not HAS_MUTAGEN:
        return None
    
    try:
        f = mutagen.File(path)
        if not f:
            return None

        art_data = None
        
        # ID3 (MP3)
        if hasattr(f, 'tags') and isinstance(f.tags, ID3):
            for tag in f.tags.values():
                if isinstance(tag, APIC):
                    art_data = tag.data
                    break
        
        # FLAC
        elif isinstance(f, FLAC):
            if f.pictures:
                art_data = f.pictures[0].data
        
        # MP4 / M4A
        elif isinstance(f, MP4):
            if 'covr' in f.tags:
                covers = f.tags['covr']
                if covers:
                    art_data = covers[0] if isinstance(covers[0], bytes) else bytes(covers[0])

        if art_data:
            return _image_data_to_base64(art_data)
            
    except Exception as e:
        print(f"Error extracting audio cover: {e}")
    
    return None

def get_windows_thumbnail(path: str) -> str | None:
    if not HAS_WIN32:
        return None
    
    try:
        # Initialize COM (needed if running in a fresh thread)
        # pythoncom.CoInitialize() 
        
        # Use IShellItemImageFactory to get thumbnail
        # We ask for a larger size (e.g. 64 or 128) for better quality, then resize if needed
        si = shell.SHCreateItemFromParsingName(path, None, shell.IID_IShellItemImageFactory)
        hbitmap = si.GetImage((128, 128), shellcon.SIIGBF_RESIZETOFIT)
        
        # Convert HBITMAP to PIL Image
        bmp_info = win32gui.GetObject(hbitmap)
        
        bitmap = win32ui.CreateBitmapFromHandle(hbitmap)
        save_dc = win32ui.CreateDCFromHandle(win32gui.GetDC(0))
        save_bitmap_dc = save_dc.CreateCompatibleDC()
        save_bitmap_dc.SelectObject(bitmap)
        
        bmp_str = bitmap.GetBitmapBits(True)
        img = Image.frombuffer(
            'RGB',
            (bmp_info.bmWidth, bmp_info.bmHeight),
            bmp_str, 'raw', 'BGRX', 0, 1
        )
        
        # Cleanup win32 handles
        win32gui.DeleteObject(hbitmap)
        save_bitmap_dc.DeleteDC()
        # save_dc.DeleteDC() # Usually not deleted if obtained from GetDC(0)? Check docs. 
        # Actually CreateDCFromHandle wraps it, so deleting the wrapper is fine.
        
        # Convert PIL image to base64
        buf = io.BytesIO()
        img.save(buf, format='PNG')
        return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode('ascii')
        
    except Exception as e:
        print(f"Error extracting windows thumbnail: {e}")
        return None

def get_system_icon(path: str) -> str | None:
    try:
        provider = QFileIconProvider()
        info = QFileInfo(path)
        icon = provider.icon(info)
        
        if icon.isNull():
            return None
            
        # Get a reasonable size pixmap
        pixmap = icon.pixmap(48, 48)
        
        # Convert to Base64
        byte_array = QBuffer()
        byte_array.open(QIODevice.WriteOnly)
        pixmap.save(byte_array, "PNG")
        data = byte_array.data().toBase64().data().decode('ascii')
        
        return "data:image/png;base64," + data
        
    except Exception as e:
        print(f"Error extracting system icon: {e}")
        return None

def _image_data_to_base64(data: bytes) -> str:
    try:
        # Verify it's an image
        img = Image.open(io.BytesIO(data))
        # Resize if too huge? 
        if img.width > 256 or img.height > 256:
            img.thumbnail((256, 256))
            buf = io.BytesIO()
            img.save(buf, format='PNG')
            data = buf.getvalue()
            
        b64 = base64.b64encode(data).decode('ascii')
        # Guess mime type? Or just use png/jpeg
        # PIL can tell us the format
        fmt = img.format.lower() if img.format else 'png'
        return f"data:image/{fmt};base64,{b64}"
    except Exception:
        return "data:image/png;base64," + base64.b64encode(data).decode('ascii')
