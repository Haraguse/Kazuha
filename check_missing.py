import xml.etree.ElementTree as ET
import os
import sys
from fill_translations import translations

if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

def check_missing(ts_path, lang_code):
    if not os.path.exists(ts_path):
        print(f"File {ts_path} not found.")
        return
    
    tree = ET.parse(ts_path)
    root = tree.getroot()
    
    lang_trans = translations.get(lang_code, {})
    
    missing = []
    for context in root.findall('context'):
        context_name = context.find('name').text
        ctx_trans = lang_trans.get(context_name, {})
        
        for message in context.findall('message'):
            source = message.find('source').text
            translation = message.find('translation')
            
            if translation.text is None or translation.text.strip() == "":
                if source not in ctx_trans:
                    missing.append((context_name, source))
    
    if missing:
        print(f"\nMissing translations for {lang_code} ({ts_path}):")
        for ctx, src in missing:
            print(f"  [{ctx}] {src}")
    else:
        print(f"\nNo missing translations for {lang_code}.")

ts_files = {
    "en": "translations/kazuha_en.ts",
    "ja": "translations/kazuha_ja.ts",
    "zh_TW": "translations/kazuha_zh_TW.ts",
    "bo": "translations/kazuha_bo.ts",
}

for lang, path in ts_files.items():
    check_missing(path, lang)
