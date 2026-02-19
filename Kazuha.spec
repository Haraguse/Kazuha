# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['main.py'],
    pathex=[],
    binaries=[],
    datas=[('version.json', '.'), ('plugins', 'plugins'), ('icons', 'icons'), ('ppt_assistant', 'ppt_assistant'), ('fonts', 'fonts')],
    hiddenimports=['PySide6.QtXml', 'plugins.webview_runner'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=['PyQt5', 'setuptools', 'pkg_resources'],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name='Kazuha',
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
    icon=['h:\\Dev\\Kazuha\\icons\\logo.ico'],
)
coll = COLLECT(
    exe,
    a.binaries,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name='Kazuha',
)
