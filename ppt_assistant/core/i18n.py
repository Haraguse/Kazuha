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
        "tray.action.confirm.cancel": "取消",
        "tray.restart.confirm.title": "确认重新启动",
        "tray.restart.confirm.body": "重新启动将关闭并立即重新打开 Luminalium。确定现在重新启动吗？",
        "tray.restart.confirm.confirm": "重新启动",
        "tray.exit.confirm.title": "\u786e\u8ba4\u9000\u51fa",
        "tray.exit.confirm.body": "\u9000\u51fa\u540e\u5c06\u5173\u95ed Luminalium\u3002\u786e\u5b9a\u73b0\u5728\u9000\u51fa\u5417\uff1f",
        "tray.exit.confirm.confirm": "\u9000\u51fa",
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
        "tray.action.confirm.cancel": "取消",
        "tray.restart.confirm.title": "確認重新啟動",
        "tray.restart.confirm.body": "重新啟動會關閉並立即重新開啟 Luminalium。確定現在重新啟動嗎？",
        "tray.restart.confirm.confirm": "重新啟動",
        "tray.exit.confirm.title": "\u78ba\u8a8d\u9000\u51fa",
        "tray.exit.confirm.body": "\u9000\u51fa\u5f8c\u5c07\u95dc\u9589 Luminalium\u3002\u78ba\u5b9a\u73fe\u5728\u9000\u51fa\u55ce\uff1f",
        "tray.exit.confirm.confirm": "\u9000\u51fa",
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
        "tray.action.confirm.cancel": "取消",
        "tray.restart.confirm.title": "確認重新啟動",
        "tray.restart.confirm.body": "重新啟動會閂咗再即刻打開 Luminalium。係咪而家重啟？",
        "tray.restart.confirm.confirm": "重新啟動",
        "tray.exit.confirm.title": "\u78ba\u8a8d\u9000\u51fa",
        "tray.exit.confirm.body": "\u9000\u51fa\u5f8c\u6703\u95dc\u9589 Luminalium\u3002\u4fc2\u54aa\u800c\u5bb6\u9000\u51fa\uff1f",
        "tray.exit.confirm.confirm": "\u9000\u51fa",
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
        "tray.action.confirm.cancel": "キャンセル",
        "tray.restart.confirm.title": "再起動の確認",
        "tray.restart.confirm.body": "再起動すると Luminalium を閉じてすぐに再度起動します。今すぐ再起動しますか？",
        "tray.restart.confirm.confirm": "再起動",
        "tray.exit.confirm.title": "\u7d42\u4e86\u306e\u78ba\u8a8d",
        "tray.exit.confirm.body": "Luminalium \u3092\u9589\u3058\u3066\u7d42\u4e86\u3057\u307e\u3059\u3002\u3053\u306e\u307e\u307e\u7d42\u4e86\u3057\u307e\u3059\u304b\uff1f",
        "tray.exit.confirm.confirm": "\u7d42\u4e86",
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
        "tray.action.confirm.cancel": "Cancel",
        "tray.restart.confirm.title": "Confirm restart",
        "tray.restart.confirm.body": "Restarting will close and reopen Luminalium right away. Do you want to restart now?",
        "tray.restart.confirm.confirm": "Restart",
        "tray.exit.confirm.title": "Confirm exit",
        "tray.exit.confirm.body": "Exiting will close Luminalium. Do you want to exit now?",
        "tray.exit.confirm.confirm": "Exit",
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
    table = (
        _TRANSLATIONS.get(lang)
        or _TRANSLATIONS.get(fallback_lang)
        or _TRANSLATIONS["zh-CN"]
    )
    if key in table:
        return table[key]
    default = _TRANSLATIONS.get(fallback_lang) or _TRANSLATIONS["zh-CN"]
    return default.get(key, key)
