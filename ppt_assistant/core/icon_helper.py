import os
import io
import base64
import mimetypes
import sys
import tempfile
import hashlib
from PIL import Image
from PySide6.QtCore import QBuffer, QIODevice, QFileInfo
from PySide6.QtWidgets import QFileIconProvider

# Try importing mutagen for audio metadata
try:
    import mutagen
    from mutagen.id3 import ID3, APIC
    from mutagen.flac import FLAC
    from mutagen.mp4 import MP4
    HAS_MUTAGEN = True
except ImportError:
    HAS_MUTAGEN = False

# Try importing pywin32 for system thumbnails
try:
    from win32com.shell import shell, shellcon
    import win32gui
    import win32ui
    HAS_WIN32 = True
except ImportError:
    HAS_WIN32 = False

# Linux-specific imports - functions are imported locally where needed
HAS_LINUX_THUMBNAIL = sys.platform.startswith('linux')

# Thumbnail cache: {cache_key: (base64_data, timestamp)}
_thumbnail_cache: dict[str, tuple[str, float]] = {}


def _get_cache_key(path: str, slide_index: int) -> str:
    """Generate a cache key for a presentation file."""
    try:
        stat = os.stat(path)
        # Cache key includes: file path, modification time, size, and slide index
        key_data = f"{path}:{stat.st_mtime}:{stat.st_size}:{slide_index}"
        return hashlib.md5(key_data.encode()).hexdigest()
    except Exception:
        # Fallback to simple path-based key
        return hashlib.md5(f"{path}:{slide_index}".encode()).hexdigest()


def _get_cached_thumbnail(path: str, slide_index: int) -> str | None:
    """Get cached thumbnail if available and not expired."""
    try:
        cache_key = _get_cache_key(path, slide_index)
        if cache_key in _thumbnail_cache:
            base64_data, timestamp = _thumbnail_cache[cache_key]
            # Check if file has been modified since caching
            current_mtime = os.path.getmtime(path)
            if timestamp >= current_mtime:
                return base64_data
            # Cache is stale, remove it
            del _thumbnail_cache[cache_key]
    except Exception:
        pass
    return None


def _set_cached_thumbnail(path: str, slide_index: int, base64_data: str) -> None:
    """Cache a thumbnail."""
    try:
        cache_key = _get_cache_key(path, slide_index)
        _thumbnail_cache[cache_key] = (base64_data, os.path.getmtime(path))
    except Exception:
        pass


def clear_thumbnail_cache() -> None:
    """Clear the thumbnail cache."""
    _thumbnail_cache.clear()

def get_file_icon_base64(path: str) -> str | None:
    """
    Get the base64 encoded icon/thumbnail for a file.
    - Audio: Album art
    - Video: Thumbnail
    - PPT/PPTX: Slide thumbnail (Linux only, via LibreOffice)
    - Exe/Others: System Icon
    """
    if not os.path.exists(path):
        return None

    mime_type, _ = mimetypes.guess_type(path)
    if mime_type:
        if mime_type.startswith('audio/'):
            icon = get_audio_cover(path)
            if icon:
                return icon
        elif mime_type.startswith('video/'):
            # Try to get thumbnail for video
            if HAS_WIN32:
                icon = get_windows_thumbnail(path)
                if icon:
                    return icon

    # Check if it's a presentation file
    ext = os.path.splitext(path)[1].lower()
    if ext in ('.ppt', '.pptx', '.pps', '.ppsx', '.dps', '.dpt'):
        # Try to get thumbnail for presentation
        icon = get_presentation_thumbnail(path)
        if icon:
            return icon

    # Fallback to system icon (exe, or failed audio/video extraction)
    return get_system_icon(path)


def get_presentation_thumbnail(path: str, slide_index: int = 1) -> str | None:
    """
    Get thumbnail for a presentation file (PPT/PPTX/etc.).

    On Windows: Uses system thumbnail (if available).
    On Linux: Uses LibreOffice to generate thumbnail.

    Args:
        path: Path to the presentation file
        slide_index: Which slide to thumbnail (1-based)

    Returns:
        Base64 encoded image data, or None if failed
    """
    if not os.path.exists(path):
        return None

    # Windows: try system thumbnail first
    if HAS_WIN32 and sys.platform == 'win32':
        try:
            icon = get_windows_thumbnail(path)
            if icon:
                return icon
        except Exception:
            pass

    # Linux: use LibreOffice
    if sys.platform.startswith('linux') and HAS_LINUX_THUMBNAIL:
        return _get_linux_presentation_thumbnail(path, slide_index)

    return None


def _get_linux_presentation_thumbnail(path: str, slide_index: int = 1) -> str | None:
    """
    Generate presentation thumbnail on Linux using LibreOffice.

    Args:
        path: Path to the presentation file
        slide_index: Which slide to thumbnail (1-based)

    Returns:
        Base64 encoded PNG image data, or None if failed
    """
    # Import here to avoid issues on non-Linux platforms
    from .system.linux import has_libreoffice, generate_thumbnail_with_libreoffice

    if not has_libreoffice():
        return None

    # Check cache first
    cached = _get_cached_thumbnail(path, slide_index)
    if cached:
        return cached

    try:
        with tempfile.NamedTemporaryFile(suffix='.png', delete=False) as tmp:
            tmp_path = tmp.name

        try:
            # Generate thumbnail using LibreOffice
            success = generate_thumbnail_with_libreoffice(
                ppt_path=path,
                output_path=tmp_path,
                slide_index=slide_index,
                width=320,
                height=180,
            )

            if not success or not os.path.exists(tmp_path):
                return None

            # Read and encode the image
            with open(tmp_path, 'rb') as f:
                image_data = f.read()

            base64_data = _image_data_to_base64(image_data)

            # Cache the result
            _set_cached_thumbnail(path, slide_index, base64_data)

            return base64_data

        finally:
            # Clean up temp file
            try:
                if os.path.exists(tmp_path):
                    os.remove(tmp_path)
            except Exception:
                pass

    except Exception as e:
        print(f"Error generating Linux presentation thumbnail: {e}")
        return None


def get_ppt_thumbnail_from_wps_window(slide_index: int = 1) -> str | None:
    """
    Get thumbnail from the currently active WPS slideshow window.

    This function finds the WPS slideshow window, extracts the filename from
    the window title, locates the actual file via /proc/{pid}/fd, and generates
    a thumbnail using LibreOffice.

    Args:
        slide_index: Which slide to thumbnail (1-based)

    Returns:
        Base64 encoded PNG image data, or None if failed
    """
    if not sys.platform.startswith('linux'):
        return None

    # Import here to avoid issues on non-Linux platforms
    from .system.linux import (
        find_linux_slideshow_window_id,
        get_ppt_path_from_slideshow_window,
    )

    try:
        # Find the slideshow window
        window_id = find_linux_slideshow_window_id()
        if not window_id:
            return None

        # Get the PPT file path from the window
        ppt_path = get_ppt_path_from_slideshow_window(window_id)
        if not ppt_path:
            return None

        # Generate thumbnail
        return get_presentation_thumbnail(ppt_path, slide_index)

    except Exception as e:
        print(f"Error getting PPT thumbnail from WPS window: {e}")
        return None

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
