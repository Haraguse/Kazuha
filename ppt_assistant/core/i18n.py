import json
import os

from ppt_assistant.core.config import SETTINGS_PATH


_TRANSLATIONS = {
    "zh-CN": {
        "tray.tooltip": "荧素万演",
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
        "resource.monitor.title": "检查你设备的性能资源占用",
        "resource.monitor.body": "相较于启动时，你设备的性能占用可能无法保证 Luminalium 正常运行。如要保持更好的体验，请释放一些性能占用。",
        "debug.resourceAlert.label": "触发资源监测通知",
        "debug.resourceAlert.desc": "手动触发系统资源占用告警通知进行测试",
    },
    "zh-TW": {
        "tray.tooltip": "Luminalium",
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
        "resource.monitor.title": "檢查你裝置的效能資源佔用",
        "resource.monitor.body": "相較於啟動時，你裝置的效能佔用可能無法保證 Luminalium 正常執行。如要保持更好的體驗，請釋放一些效能佔用。",
        "debug.resourceAlert.label": "觸發資源監測通知",
        "debug.resourceAlert.desc": "手動觸發系統資源佔用告警通知進行測試",
    },
    "yue-HK": {
        "tray.tooltip": "Luminalium",
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
        "resource.monitor.title": "屌你老母！你部機就嚟爆喇，傻閪",
        "resource.monitor.body": "你老味，部機咁撚樣佔用資源，Luminalium 點行呀？一陣 Hand 機唔好過嚟喊。識唔識用電腦㗎？快撚啲清咗啲 Background Task 佢啦，廢柴。",
        "crash.toast.body": "已發通知，之後可以再開返",
        "overlay.compatibility": "兼容模式",
        "debug.resourceAlert.label": "觸發資源監測通知",
        "debug.resourceAlert.desc": "手動觸發系統資源佔用告警通知進行測試",
    },
    "ja-JP": {
        "tray.tooltip": "ルマイナリウム",
        "tray.title": "ルマイナリウム",
        "tray.settings": "設定",
        "tray.board": "黒板",
        "tray.timer": "Timer",
        "tray.restart": "再起動",
        "tray.exit": "終了",
        "tray.toggle": "ツールバーの表示/非表示",
        "tray.action.confirm.cancel": "キャンセル",
        "tray.restart.confirm.title": "再起動の確認",
        "tray.restart.confirm.body": "再起動すると ルマイナリウム を閉じてすぐに再度起動します。今すぐ再起動しますか？",
        "tray.restart.confirm.confirm": "再起動",
        "tray.exit.confirm.title": "\u7d42\u4e86\u306e\u78ba\u8a8d",
        "tray.exit.confirm.body": "ルマイナリウム \u3092\u9589\u3058\u3066\u7d42\u4e86\u3057\u307e\u3059\u3002\u3053\u306e\u307e\u307e\u7d42\u4e86\u3057\u307e\u3059\u304b\uff1f",
        "tray.exit.confirm.confirm": "\u7d42\u4e86",
        "timer.notify.title": "時間になりました",
        "timer.notify.body": "タイマーが終了しました",
        "crash.toast.title": "異常終了を検知しました",
        "resource.monitor.title": "デバイスのパフォーマンスリソース使用状況を確認してください",
        "resource.monitor.body": "起動時と比較して、デバイスのパフォーマンス使用率が ルマイナリウム の正常な動作を保証できない可能性があります。より良い体験を維持するために、パフォーマンス使用率を解放してください。",
        "crash.toast.body": "通知を送信しました。必要なら再起動してください",
        "overlay.compatibility": "互換モード",
        "debug.resourceAlert.label": "リソース監視通知をトリガー",
        "debug.resourceAlert.desc": "テスト用にシステムリソース使用通知を手動でトリガー",
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
        "resource.monitor.title": "Check Your Device's Performance Resource Usage",
        "resource.monitor.body": "Compared to startup, your device's performance usage may not guarantee Luminalium's normal operation. To maintain a better experience, please release some performance resources.",
        "crash.toast.body": "A notification was sent. You can reopen the app later.",
        "overlay.compatibility": "Compat Mode",
        "debug.resourceAlert.label": "Trigger Resource Alert",
        "debug.resourceAlert.desc": "Manually trigger system resource usage alert notification for testing",
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
        "resource.monitor.title": "Check device performance",
        "resource.monitor.body": "Device performance usage may be high. Please release some performance resources.",
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
