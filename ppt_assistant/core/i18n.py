import json
import os

from ppt_assistant.core.config import SETTINGS_PATH


_TRANSLATIONS = {
    "zh-CN": {
        "tray.tooltip": "Kazuha 助手",
        "tray.title": "Kazuha",
        "tray.settings": "设置",
        "tray.board": "小黑板",
        "tray.timer": "计时工具",
        "tray.restart": "重新启动程序",
        "tray.exit": "退出程序",
        "timer.notify.title": "时间到",
        "timer.notify.body": "倒计时已结束",
    },
    "zh-TW": {
        "tray.tooltip": "Kazuha 助手",
        "tray.title": "Kazuha",
        "tray.settings": "設定",
        "tray.board": "小黑板",
        "tray.timer": "Timer",
        "tray.restart": "重新啟動程式",
        "tray.exit": "退出程式",
        "timer.notify.title": "時間到",
        "timer.notify.body": "倒數計時已結束",
    },
    "yue-HK": {
        "tray.tooltip": "Kazuha 幫手",
        "tray.title": "Kazuha",
        "tray.settings": "設定",
        "tray.board": "黑板仔",
        "tray.timer": "計時器",
        "tray.restart": "重啟程式",
        "tray.exit": "走人",
        "timer.notify.title": "時間到喇",
        "timer.notify.body": "倒數完咗，收工啦",
    },
    "ja-JP": {
        "tray.tooltip": "Kazuha アシスタント",
        "tray.title": "Kazuha",
        "tray.settings": "設定",
        "tray.board": "黒板",
        "tray.timer": "Timer",
        "tray.restart": "再起動",
        "tray.exit": "終了",
        "timer.notify.title": "時間になりました",
        "timer.notify.body": "タイマーが終了しました",
    },
    "en-US": {
        "tray.tooltip": "Kazuha Assistant",
        "tray.title": "Kazuha",
        "tray.settings": "Settings",
        "tray.board": "Board",
        "tray.timer": "Timer",
        "tray.restart": "Restart",
        "tray.exit": "Exit",
        "timer.notify.title": "Time's up",
        "timer.notify.body": "Countdown finished",
    },
    "ug-CN": {
        "tray.tooltip": "Kazuha ياردەمچىسى",
        "tray.title": "Kazuha",
        "tray.settings": "تەڭشەكلەر",
        "tray.timer": "ۋاقىت بەلگىلەش قىستۇرمىسى",
        "tray.restart": "قايتا قوزغىتىش",
        "tray.exit": "چېكىنىش",
        "timer.notify.title": "ۋاقىت توشتى",
        "timer.notify.body": "قايتۇرما ۋاقىت تاماملاندى",
    },
}


def get_language() -> str:
    try:
        if os.path.exists(SETTINGS_PATH):
            with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                data = json.load(f)
            lang = (data.get("General") or {}).get("Language")
            if isinstance(lang, str) and lang.strip():
                return lang.strip()
    except Exception:
        pass
    return "zh-CN"


def t(key: str) -> str:
    lang = get_language()
    fallback_lang = "zh-TW" if lang == "yue-HK" else "zh-CN"
    table = _TRANSLATIONS.get(lang) or _TRANSLATIONS.get(fallback_lang) or _TRANSLATIONS["zh-CN"]
    if key in table:
        return table[key]
    default = _TRANSLATIONS.get(fallback_lang) or _TRANSLATIONS["zh-CN"]
    return default.get(key, key)
