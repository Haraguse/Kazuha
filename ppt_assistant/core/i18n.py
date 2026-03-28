import json
import os

from ppt_assistant.core.config import SETTINGS_PATH


_TRANSLATIONS = {
    "zh-CN": {
        "tray.tooltip": "Luminalium 助手",
        "tray.title": "Luminalium",
        "tray.settings": "设置",
        "tray.board": "小黑板",
        "tray.timer": "计时工具",
        "tray.restart": "重新启动程序",
        "tray.exit": "退出程序",
        "tray.toggle": "显示/隐藏工具栏",
        "timer.notify.title": "时间到",
        "timer.notify.body": "倒计时已结束",
        "timer.background.title": "计时器",
        "timer.background.body": "计时器正在后台运行",
        "crash.toast.title": "检测到异常退出",
        "crash.toast.body": "已发送提醒，可稍后重新打开应用",
        "overlay.compatibility": "兼容模式",
    },
    "zh-TW": {
        "tray.tooltip": "Luminalium 助手",
        "tray.title": "Luminalium",
        "tray.settings": "設定",
        "tray.board": "小黑板",
        "tray.timer": "Timer",
        "tray.restart": "重新啟動程式",
        "tray.exit": "退出程式",
        "tray.toggle": "顯示/隱藏工具列",
        "timer.notify.title": "時間到",
        "timer.notify.body": "倒數計時已結束",
        "timer.background.title": "計時器",
        "timer.background.body": "計時器正在背景執行",
        "crash.toast.title": "偵測到異常結束",
        "crash.toast.body": "已送出提醒，可稍後重新開啟",
        "overlay.compatibility": "相容模式",
    },
    "yue-HK": {
        "tray.tooltip": "Luminalium 幫手",
        "tray.title": "Luminalium",
        "tray.settings": "設定",
        "tray.board": "黑板仔",
        "tray.timer": "計時器",
        "tray.restart": "重啟程式",
        "tray.exit": "走人",
        "tray.toggle": "開/閂工具列",
        "timer.notify.title": "時間到喇",
        "timer.notify.body": "倒數完咗，收工啦",
        "timer.background.title": "計時器",
        "timer.background.body": "計時器喺後台行緊",
        "crash.toast.title": "檢測到異常退出",
        "crash.toast.body": "已發通知，之後可以再開返",
        "overlay.compatibility": "兼容模式",
    },
    "ja-JP": {
        "tray.tooltip": "Luminalium アシスタント",
        "tray.title": "Luminalium",
        "tray.settings": "設定",
        "tray.board": "黒板",
        "tray.timer": "Timer",
        "tray.restart": "再起動",
        "tray.exit": "終了",
        "tray.toggle": "ツールバーの表示/非表示",
        "timer.notify.title": "時間になりました",
        "timer.notify.body": "タイマーが終了しました",
        "crash.toast.title": "異常終了を検知しました",
        "crash.toast.body": "通知を送信しました。必要なら再起動してください",
        "overlay.compatibility": "互換モード",
    },
    "en-US": {
        "tray.tooltip": "Luminalium Assistant",
        "tray.title": "Luminalium",
        "tray.settings": "Settings",
        "tray.board": "Board",
        "tray.timer": "Timer",
        "tray.restart": "Restart",
        "tray.exit": "Exit",
        "tray.toggle": "Show/Hide Toolbar",
        "timer.notify.title": "Time's up",
        "timer.notify.body": "Countdown finished",
        "crash.toast.title": "Unexpected exit detected",
        "crash.toast.body": "A notification was sent. You can reopen the app later.",
        "overlay.compatibility": "Compat Mode",
    },
    "ug-CN": {
        "tray.tooltip": "Luminalium ياردەمچىسى",
        "tray.title": "Luminalium",
        "tray.settings": "تەڭشەكلەر",
        "tray.timer": "ۋاقىت بەلگىلەش قىستۇرمىسى",
        "tray.restart": "قايتا قوزغىتىش",
        "tray.exit": "چېكىنىش",
        "timer.notify.title": "ۋاقىت توشتى",
        "timer.notify.body": "قايتۇرما ۋاقىت تاماملاندى",
        "timer.background.title": "ۋاقىت بەلگىلەش",
        "timer.background.body": "ۋاقىت بەلگىلەش ئارقا سۇپىدا ئىشلەۋاتىدۇ",
        "crash.toast.title": "Unexpected exit detected",
        "crash.toast.body": "A notification was sent.",
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
