# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['main.py'],
    pathex=[],
    binaries=[],
    datas=[('version.json', '.'), ('config', 'config'), ('plugins', 'plugins'), ('icons', 'icons'), ('fonts', 'fonts'), ('user', 'user'), ('ppt_assistant/assets', 'ppt_assistant/assets'), ('ppt_assistant\\ui\\crash_dialog.html', 'ppt_assistant/ui'), ('ppt_assistant\\ui\\dialog.html', 'ppt_assistant/ui'), ('ppt_assistant\\ui\\LinuxOverlay.qml', 'ppt_assistant/ui'), ('ppt_assistant\\ui\\overlay.html', 'ppt_assistant/ui'), ('ppt_assistant\\ui\\splash.html', 'ppt_assistant/ui'), ('ppt_assistant\\ui\\titlebar.html', 'ppt_assistant/ui'), ('ppt_assistant\\ui\\titlebar_demo.html', 'ppt_assistant/ui')],
    hiddenimports=['PySide6.QtXml', 'winrt.windows.data.xml.dom', 'winrt.windows.ui.notifications'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=['PyQt5', 'PyQt5-Frameless-Window', 'PyQt6', 'setuptools', 'pkg_resources', 'numpy', 'scipy', 'ppt_assistant.rendering'],
    noarchive=False,
    optimize=1,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    [('O', None, 'OPTION')],
    exclude_binaries=True,
    name='Luminalium',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=['H:\\Dev\\Kazuha\\icons\\logo.ico'],
)
coll = COLLECT(
    exe,
    a.binaries,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name='Luminalium',
)
