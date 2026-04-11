import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import Qt5Compat.GraphicalEffects

Item {
    id: root
    width: 1920
    height: 1080

    property var currentConfig: ({
            showStatusBar: true,
            disableAnimations: false,
            statusBarShowTime: true,
            statusBarShowSeconds: false,
            statusBarShowBattery: true,
            statusBarShowVolume: true,
            statusBarShowNetwork: true,
            statusBarShowMusic: true,
            statusBarShowMusicProgress: true,
            toolbarOrder: ["select", "pen", "eraser", "clear", "spotlight", "board_in_board", "timer", "apps"],
            toolbarPosition: "bottom",
            compatibilityMode: false,
            flipperPosition: "center",
            showClear: true,
            clearMode: "slider",
            showSpotlight: true,
            showBoardInBoard: true,
            showTimer: true,
            scale: 1.0,
            safeArea: 0,
            popWindowScale: 1.0,
            toolbarOpacity: 1.0,
            sidePageOpacity: 1.0,
            strictEdgeAlignment: false,
            texts: {},
            apps: [],
            disabledTools: []
        })
    property var currentTheme: ({
            themeId: "default",
            themeMode: "light",
            darkMode: false,
            accentColor: "#3275F5",
            toolbarBg: "#FFFFFF",
            toolbarBorder: "rgba(0,0,0,0.08)",
            toolbarFg: "#333333",
            toolbarLine: "rgba(0,0,0,0.08)",
            statusBg: "rgba(0,0,0,0.25)",
            statusFg: "#FFFFFF",
            pageBg: "#FFFFFF",
            pageBorder: "rgba(0,0,0,0.08)",
            pageFg: "#333333",
            pageHint: "rgba(0,0,0,0.5)",
            pageHover: "rgba(0,0,0,0.05)",
            popupBg: "#FFFFFF",
            popupBorder: "rgba(0,0,0,0.12)",
            popupFg: "#333333",
            buttonHover: "rgba(0,0,0,0.06)",
            buttonActive: "rgba(0,0,0,0.12)",
            controlBg: "rgba(0,0,0,0.06)",
            controlHover: "rgba(0,0,0,0.1)",
            controlActive: "rgba(0,0,0,0.15)",
            thumbBg: "#FFFFFF",
            thumbIcon: "#333333",
            dialogMask: "rgba(0,0,0,0.2)",
            dialogBg: "rgba(255,255,255,0.98)",
            dialogBorder: "rgba(0,0,0,0.08)",
            dialogTitle: "#1a1c1e",
            dialogText: "rgba(0,0,0,0.65)",
            dialogPrimary: "#3275F5",
            dialogButtonText: "#666666"
        })
    property var systemStatus: ({
            is_desktop: false,
            battery_percent: 100,
            battery_charging: false,
            network_online: false,
            volume: -1,
            smtc_status: "",
            smtc_title: "",
            smtc_position_ms: 0,
            smtc_duration_ms: 0
        })
    property int currentPage: 1
    property int totalPage: 1
    property string currentTool: "select"
    property string currentColorTab: "highlight"
    property string currentPenColorHex: "#FF0000"
    property var colorInfo: ({
            name: "红色",
            rgb: "255 0 0",
            hex: "#FF0000"
        })
    property var thumbnailSources: ({})
    property var thumbnailRequested: ({})
    property bool colorPickerVisible: false
    property bool eraserPopupVisible: false
    property bool pageSelectorVisible: false
    property bool pageSelectorLeftSide: false
    property bool inkPromptVisible: false
    property bool toolbarCollapsed: false
    property real clearSliderProgress: 0.0
    property real gestureStartX: 0
    property real gestureStartY: 0
    property var toolbarItems: []

    readonly property bool animationsEnabled: currentConfig.disableAnimations !== true
    readonly property real edgeInset: currentConfig.strictEdgeAlignment ? 0 : 20 + Number(currentConfig.safeArea || 0)
    readonly property string toolbarPos: String(currentConfig.toolbarPosition || "bottom")
    readonly property string flipperPos: String(currentConfig.flipperPosition || "center")
    readonly property real popScale: Number(currentConfig.popWindowScale || 1.0)

    property var penColors: [
        {
            tab: "pen",
            name: "白色",
            rgb: "255 255 255",
            hex: "#FFFFFF"
        },
        {
            tab: "pen",
            name: "黑色",
            rgb: "0 0 0",
            hex: "#000000"
        },
        {
            tab: "pen",
            name: "浅灰色",
            rgb: "231 230 230",
            hex: "#E7E6E6"
        },
        {
            tab: "pen",
            name: "蓝灰色",
            rgb: "68 84 106",
            hex: "#44546A"
        },
        {
            tab: "pen",
            name: "蓝色",
            rgb: "68 114 196",
            hex: "#4472C4"
        },
        {
            tab: "pen",
            name: "橙色",
            rgb: "237 125 49",
            hex: "#ED7D31"
        },
        {
            tab: "pen",
            name: "灰色",
            rgb: "165 165 165",
            hex: "#A5A5A5"
        },
        {
            tab: "pen",
            name: "金色",
            rgb: "255 192 0",
            hex: "#FFC000"
        },
        {
            tab: "pen",
            name: "浅蓝色",
            rgb: "91 155 213",
            hex: "#5B9BD5"
        },
        {
            tab: "pen",
            name: "绿色",
            rgb: "112 173 71",
            hex: "#70AD47"
        }
    ]
    property var highlightColors: [
        {
            tab: "highlight",
            name: "深红色",
            rgb: "192 0 0",
            hex: "#C00000"
        },
        {
            tab: "highlight",
            name: "红色",
            rgb: "255 0 0",
            hex: "#FF0000"
        },
        {
            tab: "highlight",
            name: "金色",
            rgb: "255 192 0",
            hex: "#FFC000"
        },
        {
            tab: "highlight",
            name: "黄色",
            rgb: "255 255 0",
            hex: "#FFFF00"
        },
        {
            tab: "highlight",
            name: "浅绿色",
            rgb: "146 208 80",
            hex: "#92D050"
        },
        {
            tab: "highlight",
            name: "绿色",
            rgb: "0 176 80",
            hex: "#00B050"
        },
        {
            tab: "highlight",
            name: "浅蓝色",
            rgb: "0 176 240",
            hex: "#00B0F0"
        },
        {
            tab: "highlight",
            name: "蓝色",
            rgb: "0 112 192",
            hex: "#0070C0"
        },
        {
            tab: "highlight",
            name: "深蓝色",
            rgb: "0 32 96",
            hex: "#002060"
        },
        {
            tab: "highlight",
            name: "紫色",
            rgb: "112 48 160",
            hex: "#7030A0"
        }
    ]

    Timer {
        id: maskTimer
        interval: 16
        repeat: false
        onTriggered: root.sendMaskUpdate()
    }
    Timer {
        id: fallbackMaskTimer
        interval: root.animationsEnabled ? 360 : 0
        repeat: false
        onTriggered: root.sendMaskUpdate()
    }
    Timer {
        id: clockTimer
        interval: 1000
        running: true
        repeat: true
        onTriggered: timeDisplay.text = root.buildTimeText()
    }
    Timer {
        id: clearResetTimer
        interval: root.animationsEnabled ? 300 : 0
        repeat: false
        onTriggered: {
            clearSliderProgress = 0.0;
            root.closePopup();
        }
    }

    function translatedText(key) {
        return (currentConfig.texts && currentConfig.texts[key]) || key;
    }
    function iconSource(key) {
        const mapping = {
            select: "../../icons/Mouse.svg",
            pen: "../../icons/Pen.svg",
            eraser: "../../icons/Eraser.svg",
            clear: "../../icons/Clear.svg",
            spotlight: "../../icons/spotlight.svg",
            board_in_board: "../../icons/board-in-board.svg",
            timer: "../../icons/timer.svg",
            apps: "../../icons/More.svg",
            end: "../../icons/Minimize.svg",
            previous: "../../icons/Previous.svg",
            next: "../../icons/Next.svg"
        };
        return Qt.resolvedUrl(mapping[key] || "../../icons/More.svg");
    }
    function panelRect(item, role, fillHeight) {
        if (!item || !item.visible || item.opacity <= 0.01 || item.width <= 0 || item.height <= 0)
            return null;
        const p = item.mapToItem(root, 0, 0);
        return {
            x: p.x,
            y: fillHeight ? 0 : p.y,
            width: item.width,
            height: fillHeight ? root.height : item.height,
            role: role || ""
        };
    }
    function scheduleMaskUpdate() {
        maskTimer.restart();
        if (animationsEnabled)
            fallbackMaskTimer.restart();
    }
    function sendMaskUpdate() {
        if (!overlayBridge || typeof overlayBridge.updateMask !== "function")
            return;
        const rects = [];
        const items = [[statusBar, "status-bar", false], [leftFlipper, "left-flipper", false], [rightFlipper, "right-flipper", false], [toolbarCollapsed ? null : toolbarShadowRect, "toolbar", false], [toolbarCollapsed ? toolbarHandle : null, "toolbar-handle", false], [colorPicker, "color-picker", false], [eraserPopup, "eraser-popup", false], [pageSelector, "page-selector", true], [inkPromptMask, "ink-prompt", false]];
        for (let i = 0; i < items.length; i += 1) {
            const rect = panelRect(items[i][0], items[i][1], items[i][2]);
            if (rect)
                rects.push(rect);
        }
        overlayBridge.updateMask(rects);
    }
    function closePopup() {
        colorPickerVisible = false;
        eraserPopupVisible = false;
        pageSelectorVisible = false;
        clearSliderProgress = 0.0;
        if (overlayBridge && typeof overlayBridge.releaseFocus === "function")
            overlayBridge.releaseFocus();
        scheduleMaskUpdate();
    }
    function openPageSelector(leftSide) {
        const same = pageSelectorVisible && pageSelectorLeftSide === leftSide;
        closePopup();
        if (same)
            return;
        pageSelectorLeftSide = leftSide;
        pageSelectorVisible = true;
        requestThumbnailBurst();
        if (overlayBridge && typeof overlayBridge.startBackgroundThumbnailCaching === "function")
            overlayBridge.startBackgroundThumbnailCaching(totalPage);
        if (overlayBridge && typeof overlayBridge.resizeNudge === "function")
            overlayBridge.resizeNudge();
        scheduleMaskUpdate();
    }
    function buildTimeText() {
        if (currentConfig.statusBarShowTime === false)
            return "";
        const now = new Date();
        const hh = String(now.getHours()).padStart(2, "0");
        const mm = String(now.getMinutes()).padStart(2, "0");
        if (currentConfig.statusBarShowSeconds === true)
            return hh + ":" + mm + ":" + String(now.getSeconds()).padStart(2, "0");
        return hh + ":" + mm;
    }
    function buildMusicText() {
        if (currentConfig.statusBarShowMusic === false)
            return "";
        const st = String(systemStatus.smtc_status || "");
        if (!st || st === "Closed" || st === "Stopped")
            return "";
        const title = String(systemStatus.smtc_title || st || "");
        if (currentConfig.statusBarShowMusicProgress === false)
            return title;
        function fmt(ms) {
            const sec = Math.max(0, Math.floor(Number(ms || 0) / 1000));
            return String(Math.floor(sec / 60)).padStart(2, "0") + ":" + String(sec % 60).padStart(2, "0");
        }
        const duration = Number(systemStatus.smtc_duration_ms || 0);
        if (duration <= 0)
            return title;
        return title + " " + fmt(systemStatus.smtc_position_ms) + "/" + fmt(duration);
    }
    function replaceThumbnail(index, url) {
        const updated = Object.assign({}, thumbnailSources);
        updated[index] = url;
        thumbnailSources = updated;
    }
    function markThumbnailRequested(index) {
        const updated = Object.assign({}, thumbnailRequested);
        updated[index] = true;
        thumbnailRequested = updated;
    }
    function requestThumbnailBurst() {
        if (!overlayBridge || typeof overlayBridge.requestThumbnail !== "function")
            return;
        const start = Math.max(1, currentPage - 5);
        const end = Math.min(totalPage, currentPage + 5);
        for (let i = start; i <= end; i += 1)
            if (!thumbnailRequested[i]) {
                markThumbnailRequested(i);
                overlayBridge.requestThumbnail(i);
            }
    }
    function resetToolState(tool) {
        closePopup();
        currentTool = tool || "select";
    }
    function resetPenColorState() {
        currentColorTab = "highlight";
        currentPenColorHex = "#FF0000";
        colorInfo = {
            name: "红色",
            rgb: "255 0 0",
            hex: "#FF0000"
        };
    }
    function selectColor(entry) {
        currentColorTab = entry.tab;
        currentPenColorHex = entry.hex;
        colorInfo = {
            name: entry.name,
            rgb: entry.rgb,
            hex: entry.hex
        };
        const parts = String(entry.rgb).split(" ");
        if (overlayBridge && typeof overlayBridge.setPenColor === "function" && parts.length >= 3)
            overlayBridge.setPenColor(Number(parts[0]), Number(parts[1]), Number(parts[2]));
        closePopup();
    }
    function selectTool(tool) {
        if (tool === "pen" && currentTool === "pen") {
            if (colorPickerVisible)
                closePopup();
            else {
                closePopup();
                colorPickerVisible = true;
                if (overlayBridge && typeof overlayBridge.resizeNudge === "function")
                    overlayBridge.resizeNudge();
                scheduleMaskUpdate();
            }
            return;
        }
        if (tool === "eraser" && currentTool === "eraser") {
            if (eraserPopupVisible)
                closePopup();
            else {
                closePopup();
                eraserPopupVisible = true;
                if (overlayBridge && typeof overlayBridge.resizeNudge === "function")
                    overlayBridge.resizeNudge();
                scheduleMaskUpdate();
            }
            return;
        }
        closePopup();
        currentTool = tool;
        if (!overlayBridge || typeof overlayBridge.setTool !== "function")
            return;
        if (tool === "select")
            overlayBridge.setTool("arrow");
        else if (tool === "pen")
            overlayBridge.setTool("pen");
        else if (tool === "eraser")
            overlayBridge.setTool("eraser");
    }
    function handleToolAction(item) {
        if (!item || item.disabled)
            return;
        if (item.type === "app" && overlayBridge && typeof overlayBridge.launchApp === "function") {
            overlayBridge.launchApp(item.path || "");
            return;
        }
        if (item.key === "spotlight" && overlayBridge && typeof overlayBridge.toggleSpotlight === "function") {
            overlayBridge.toggleSpotlight();
            return;
        }
        if (item.key === "board_in_board" && overlayBridge && typeof overlayBridge.toggleBoard === "function") {
            overlayBridge.toggleBoard();
            return;
        }
        if (item.key === "timer" && overlayBridge && typeof overlayBridge.toggleTimer === "function") {
            overlayBridge.toggleTimer();
            return;
        }
        if (item.key === "end" && overlayBridge && typeof overlayBridge.endShow === "function") {
            overlayBridge.endShow();
            return;
        }
        selectTool(item.key);
    }
    function rebuildToolbarItems() {
        const items = [];
        const disabled = currentConfig.disabledTools || [];
        const order = currentConfig.toolbarOrder || ["select", "pen", "eraser", "clear", "spotlight", "board_in_board", "timer", "apps"];
        if (currentConfig.compatibilityMode)
            items.push({
                type: "compat",
                key: "compatibility"
            });
        for (let i = 0; i < order.length; i += 1) {
            const key = order[i];
            if (key === "clear")
                continue;
            if (currentConfig.compatibilityMode && (key === "pen" || key === "eraser"))
                continue;
            if (key === "spotlight" && !currentConfig.showSpotlight)
                continue;
            if (key === "board_in_board" && !currentConfig.showBoardInBoard)
                continue;
            if (key === "timer" && !currentConfig.showTimer)
                continue;
            if (key === "apps") {
                const apps = currentConfig.apps || [];
                for (let j = 0; j < apps.length; j += 1)
                    items.push({
                        type: "app",
                        key: "apps-" + j,
                        text: apps[j].name || translatedText("apps"),
                        path: apps[j].path || "",
                        icon: apps[j].icon || "",
                        disabled: false
                    });
                continue;
            }
            items.push({
                type: "tool",
                key: key,
                text: translatedText(key),
                disabled: disabled.indexOf(key) !== -1
            });
        }
        items.push({
            type: "separator",
            key: "separator"
        });
        items.push({
            type: "tool",
            key: "end",
            text: translatedText("end"),
            danger: true,
            disabled: disabled.indexOf("endShow") !== -1 || disabled.indexOf("minimize") !== -1
        });
        toolbarItems = items;
    }

    Connections {
        target: overlayBridge
        function onConfigChanged(config) {
            currentConfig = config || currentConfig;
            rebuildToolbarItems();
            scheduleMaskUpdate();
        }
        function onThemeChanged(theme) {
            currentTheme = theme || currentTheme;
            scheduleMaskUpdate();
        }
        function onPageInfoChanged(current, total) {
            currentPage = current;
            totalPage = total;
            if (pageSelectorVisible)
                requestThumbnailBurst();
            scheduleMaskUpdate();
        }
        function onSystemStatusChanged(data) {
            systemStatus = data || systemStatus;
        }
        function onThumbnailReady(index, url) {
            replaceThumbnail(index, url);
        }
        function onToolStateReset(tool) {
            resetToolState(tool);
        }
        function onPenColorReset() {
            resetPenColorState();
        }
        function onInkPromptVisibilityChanged(visible) {
            if (visible)
                closePopup();
            inkPromptVisible = !!visible;
            scheduleMaskUpdate();
        }
        function onRestrictionsChanged(protectedView, presentationReadonly) {
        }
    }

    Component.onCompleted: {
        timeDisplay.text = root.buildTimeText();
        root.rebuildToolbarItems();
        if (overlayBridge && typeof overlayBridge.requestInitState === "function")
            overlayBridge.requestInitState();
        scheduleMaskUpdate();
    }

    // Status Bar - 样式1:1复刻overlay.html
    Rectangle {
        id: statusBar
        visible: currentConfig.showStatusBar !== false
        z: 20
        radius: 16
        color: currentTheme.statusBg
        border.color: currentTheme.toolbarBorder
        border.width: 1
        anchors.top: parent.top
        anchors.topMargin: 24
        anchors.right: parent.right
        anchors.rightMargin: 24
        width: statusRow.implicitWidth + 28
        height: 44
        opacity: 0.98

        Row {
            id: statusRow
            anchors.centerIn: parent
            spacing: 10
            Label {
                id: timeDisplay
                visible: currentConfig.statusBarShowTime !== false
                text: root.buildTimeText()
                color: currentTheme.statusFg
                font.pixelSize: 22
                font.weight: Font.DemiBold
            }
            Label {
                visible: root.buildMusicText().length > 0
                text: root.buildMusicText()
                color: currentTheme.statusFg
                opacity: 0.86
                font.pixelSize: 13
            }
            Label {
                visible: currentConfig.statusBarShowVolume !== false && Number(systemStatus.volume) >= 0
                text: Number(systemStatus.volume) >= 0 ? systemStatus.volume + "%" : ""
                color: currentTheme.statusFg
                opacity: 0.86
                font.pixelSize: 13
            }
            Label {
                visible: currentConfig.statusBarShowNetwork !== false
                text: systemStatus.network_online ? "在线" : "离线"
                color: currentTheme.statusFg
                opacity: 0.86
                font.pixelSize: 13
            }
            Label {
                visible: currentConfig.statusBarShowBattery !== false
                text: systemStatus.is_desktop ? "台式机" : ((systemStatus.battery_percent || 0) + "%")
                color: currentTheme.statusFg
                opacity: 0.86
                font.pixelSize: 13
            }
        }
    }

    // Left Flipper - 样式1:1复刻overlay.html (.flipper)
    Rectangle {
        id: leftFlipper
        visible: !pageSelectorVisible || !pageSelectorLeftSide
        z: 10
        radius: 27
        color: currentTheme.pageBg
        border.color: currentTheme.pageBorder
        border.width: 1
        opacity: Number(currentConfig.sidePageOpacity || 1.0)
        width: flipperPos === "bottom" ? 160 : 54
        height: flipperPos === "bottom" ? 54 : 160
        x: flipperPos === "bottom" ? (currentConfig.strictEdgeAlignment ? 0 : edgeInset) : 24
        y: flipperPos === "bottom" ? root.height - height - (currentConfig.strictEdgeAlignment ? 0 : edgeInset) : (root.height - height) / 2 + (toolbarPos === "left" ? -0.15 * root.height : 0)

        Row {
            anchors.fill: parent
            anchors.margins: 8
            spacing: 4
            visible: flipperPos === "bottom"

            Repeater {
                model: ["previous", "center", "next"]
                delegate: Rectangle {
                    width: modelData === "center" ? parent.width - 116 : 38
                    height: 38
                    radius: 19
                    color: mouseArea.pressed ? currentTheme.buttonActive : (mouseArea.containsMouse ? currentTheme.pageHover : "transparent")

                    MouseArea {
                        id: mouseArea
                        anchors.fill: parent
                        hoverEnabled: true
                        onClicked: {
                            if (modelData === "previous" && overlayBridge && typeof overlayBridge.prevPage === "function")
                                overlayBridge.prevPage();
                            else if (modelData === "next" && overlayBridge && typeof overlayBridge.nextPage === "function")
                                overlayBridge.nextPage();
                            else
                                root.openPageSelector(true);
                        }
                    }

                    Item {
                        anchors.centerIn: parent
                        visible: modelData !== "center"
                        width: 20
                        height: 20
                        Image {
                            id: leftRaw
                            anchors.fill: parent
                            source: iconSource(modelData)
                            visible: status === Image.Ready
                            fillMode: Image.PreserveAspectFit
                            sourceSize.width: 40
                            sourceSize.height: 40
                            cache: true
                            asynchronous: false
                        }
                        ColorOverlay {
                            anchors.fill: leftRaw
                            source: leftRaw
                            color: "#333333"
                            visible: leftRaw.visible
                        }
                    }

                    Column {
                        anchors.centerIn: parent
                        visible: modelData === "center"
                        spacing: 2
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "-" : String(currentPage)
                            color: "#333333"
                            font.pixelSize: 16
                            font.weight: Font.Bold
                        }
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "" : "/" + totalPage
                            color: currentTheme.pageHint
                            font.pixelSize: 10
                        }
                    }
                }
            }
        }

        Column {
            anchors.fill: parent
            anchors.margins: 8
            spacing: 4
            visible: flipperPos !== "bottom"

            Repeater {
                model: ["previous", "center", "next"]
                delegate: Rectangle {
                    width: 38
                    height: modelData === "center" ? parent.height - 116 : 38
                    radius: 19
                    color: mouseAreaV.pressed ? currentTheme.buttonActive : (mouseAreaV.containsMouse ? currentTheme.pageHover : "transparent")

                    MouseArea {
                        id: mouseAreaV
                        anchors.fill: parent
                        hoverEnabled: true
                        onClicked: {
                            if (modelData === "previous" && overlayBridge && typeof overlayBridge.prevPage === "function")
                                overlayBridge.prevPage();
                            else if (modelData === "next" && overlayBridge && typeof overlayBridge.nextPage === "function")
                                overlayBridge.nextPage();
                            else
                                root.openPageSelector(true);
                        }
                    }

                    Item {
                        anchors.centerIn: parent
                        visible: modelData !== "center"
                        width: 20
                        height: 20
                        rotation: 90
                        Image {
                            id: leftRawV
                            anchors.fill: parent
                            source: iconSource(modelData)
                            visible: status === Image.Ready
                            fillMode: Image.PreserveAspectFit
                            sourceSize.width: 40
                            sourceSize.height: 40
                            cache: true
                            asynchronous: false
                        }
                        ColorOverlay {
                            anchors.fill: leftRawV
                            source: leftRawV
                            color: "#333333"
                            visible: leftRawV.visible
                        }
                    }

                    Column {
                        anchors.centerIn: parent
                        visible: modelData === "center"
                        spacing: 2
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "-" : String(currentPage)
                            color: "#333333"
                            font.pixelSize: 16
                            font.weight: Font.Bold
                        }
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "" : "/" + totalPage
                            color: currentTheme.pageHint
                            font.pixelSize: 10
                        }
                    }
                }
            }
        }
    }

    // Right Flipper - 样式1:1复刻overlay.html (.flipper)
    Rectangle {
        id: rightFlipper
        visible: !pageSelectorVisible || pageSelectorLeftSide
        z: 10
        radius: 27
        color: currentTheme.pageBg
        border.color: currentTheme.pageBorder
        border.width: 1
        opacity: Number(currentConfig.sidePageOpacity || 1.0)
        width: flipperPos === "bottom" ? 160 : 54
        height: flipperPos === "bottom" ? 54 : 160
        x: flipperPos === "bottom" ? root.width - width - (currentConfig.strictEdgeAlignment ? 0 : edgeInset) : root.width - width - 24
        y: flipperPos === "bottom" ? root.height - height - (currentConfig.strictEdgeAlignment ? 0 : edgeInset) : (root.height - height) / 2 + (toolbarPos === "right" ? -0.15 * root.height : 0)

        Row {
            anchors.fill: parent
            anchors.margins: 8
            spacing: 4
            visible: flipperPos === "bottom"
            layoutDirection: Qt.RightToLeft

            Repeater {
                model: ["next", "center", "previous"]
                delegate: Rectangle {
                    width: modelData === "center" ? parent.width - 116 : 38
                    height: 38
                    radius: 19
                    color: mouseAreaR.pressed ? currentTheme.buttonActive : (mouseAreaR.containsMouse ? currentTheme.pageHover : "transparent")

                    MouseArea {
                        id: mouseAreaR
                        anchors.fill: parent
                        hoverEnabled: true
                        onClicked: {
                            if (modelData === "previous" && overlayBridge && typeof overlayBridge.prevPage === "function")
                                overlayBridge.prevPage();
                            else if (modelData === "next" && overlayBridge && typeof overlayBridge.nextPage === "function")
                                overlayBridge.nextPage();
                            else
                                root.openPageSelector(false);
                        }
                    }

                    Item {
                        anchors.centerIn: parent
                        visible: modelData !== "center"
                        width: 20
                        height: 20
                        Image {
                            id: rightRaw
                            anchors.fill: parent
                            source: iconSource(modelData)
                            visible: status === Image.Ready
                            fillMode: Image.PreserveAspectFit
                            sourceSize.width: 40
                            sourceSize.height: 40
                            cache: true
                            asynchronous: false
                        }
                        ColorOverlay {
                            anchors.fill: rightRaw
                            source: rightRaw
                            color: "#333333"
                            visible: rightRaw.visible
                        }
                    }

                    Column {
                        anchors.centerIn: parent
                        visible: modelData === "center"
                        spacing: 2
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "-" : String(currentPage)
                            color: "#333333"
                            font.pixelSize: 16
                            font.weight: Font.Bold
                        }
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "" : "/" + totalPage
                            color: currentTheme.pageHint
                            font.pixelSize: 10
                        }
                    }
                }
            }
        }

        Column {
            anchors.fill: parent
            anchors.margins: 8
            spacing: 4
            visible: flipperPos !== "bottom"

            Repeater {
                model: ["previous", "center", "next"]
                delegate: Rectangle {
                    width: 38
                    height: modelData === "center" ? parent.height - 116 : 38
                    radius: 19
                    color: mouseAreaRV.pressed ? currentTheme.buttonActive : (mouseAreaRV.containsMouse ? currentTheme.pageHover : "transparent")

                    MouseArea {
                        id: mouseAreaRV
                        anchors.fill: parent
                        hoverEnabled: true
                        onClicked: {
                            if (modelData === "previous" && overlayBridge && typeof overlayBridge.prevPage === "function")
                                overlayBridge.prevPage();
                            else if (modelData === "next" && overlayBridge && typeof overlayBridge.nextPage === "function")
                                overlayBridge.nextPage();
                            else
                                root.openPageSelector(false);
                        }
                    }

                    Item {
                        anchors.centerIn: parent
                        visible: modelData !== "center"
                        width: 20
                        height: 20
                        rotation: 90
                        Image {
                            id: rightRawV
                            anchors.fill: parent
                            source: iconSource(modelData)
                            visible: status === Image.Ready
                            fillMode: Image.PreserveAspectFit
                            sourceSize.width: 40
                            sourceSize.height: 40
                            cache: true
                            asynchronous: false
                        }
                        ColorOverlay {
                            anchors.fill: rightRawV
                            source: rightRawV
                            color: "#333333"
                            visible: rightRawV.visible
                        }
                    }

                    Column {
                        anchors.centerIn: parent
                        visible: modelData === "center"
                        spacing: 2
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "-" : String(currentPage)
                            color: "#333333"
                            font.pixelSize: 16
                            font.weight: Font.Bold
                        }
                        Label {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: currentConfig.compatibilityMode ? "" : "/" + totalPage
                            color: currentTheme.pageHint
                            font.pixelSize: 10
                        }
                    }
                }
            }
        }
    }

    // Toolbar Container - 样式1:1复刻overlay.html (#toolbar-container)
    Item {
        id: toolbarContainer
        z: 100
        anchors.bottom: toolbarPos === "bottom" ? parent.bottom : undefined
        anchors.top: toolbarPos === "top" ? parent.top : undefined
        anchors.left: toolbarPos === "left" ? parent.left : undefined
        anchors.right: toolbarPos === "right" ? parent.right : undefined
        anchors.bottomMargin: toolbarPos === "bottom" ? edgeInset : 0
        anchors.topMargin: toolbarPos === "top" ? edgeInset : 0
        anchors.leftMargin: toolbarPos === "left" ? edgeInset : 0
        anchors.rightMargin: toolbarPos === "right" ? edgeInset : 0
        width: toolbarPos === "left" || toolbarPos === "right" ? 80 : parent.width
        height: toolbarPos === "left" || toolbarPos === "right" ? parent.height : 80
        visible: !toolbarCollapsed

        // Toolbar Surface - 样式1:1复刻overlay.html (#toolbar)
        Item {
            anchors.centerIn: parent
            width: toolbarShadowRect.width
            height: toolbarShadowRect.height + 12

            Rectangle {
                id: toolbarShadowRect
                anchors.horizontalCenter: parent.horizontalCenter
                anchors.top: parent.top
                radius: 999
                color: currentTheme.toolbarBg
                border.color: currentTheme.toolbarBorder
                border.width: 1
                width: toolbarContent.implicitWidth + 20
                height: 54
                opacity: Number(currentConfig.toolbarOpacity || 1.0)

                // layer.enabled disabled on Linux: FBO rendering fails on some Mesa/GPU configs
                // layer.enabled: true

                Row {
                    id: toolbarContent
                    anchors.centerIn: parent
                    spacing: 4

                    // Compatibility Label
                    Rectangle {
                        visible: currentConfig.compatibilityMode
                        width: 28
                        height: 28
                        radius: 14
                        color: Qt.rgba(0.2, 0.46, 0.96, 0.1)
                        anchors.verticalCenter: parent.verticalCenter

                        Rectangle {
                            anchors.fill: parent
                            anchors.margins: -3
                            radius: 17
                            color: "transparent"
                            border.color: Qt.rgba(0.2, 0.46, 0.96, 0.35)
                            border.width: 1
                        }

                        Label {
                            anchors.centerIn: parent
                            text: "C"
                            color: currentTheme.accentColor
                            font.pixelSize: 12
                            font.weight: Font.Medium
                        }
                    }

                    // Toolbar Items
                    Repeater {
                        model: toolbarItems
                        delegate: Rectangle {
                            width: modelData.type === "separator" ? 1 : 38
                            height: modelData.type === "separator" ? 24 : 38
                            radius: modelData.type === "separator" ? 0 : 19
                            color: {
                                if (modelData.type === "separator")
                                    return currentTheme.toolbarLine;
                                if (modelData.disabled)
                                    return "transparent";
                                if (modelData.key === currentTool)
                                    return currentTheme.buttonActive;
                                if (toolMouseArea.pressed)
                                    return currentTheme.buttonActive;
                                if (toolMouseArea.containsMouse)
                                    return currentTheme.buttonHover;
                                return "transparent";
                            }
                            anchors.verticalCenter: parent.verticalCenter
                            opacity: modelData.disabled ? 0.4 : 1

                            MouseArea {
                                id: toolMouseArea
                                anchors.fill: parent
                                hoverEnabled: true
                                enabled: !modelData.disabled
                                onClicked: root.handleToolAction(modelData)
                            }

                            Item {
                                anchors.centerIn: parent
                                visible: modelData.type !== "separator"
                                width: 20
                                height: 20

                                Image {
                                    id: toolIcon
                                    anchors.fill: parent
                                    source: modelData.icon ? Qt.resolvedUrl(modelData.icon) : iconSource(modelData.key)
                                    visible: status === Image.Ready
                                    fillMode: Image.PreserveAspectFit
                                    sourceSize.width: 40
                                    sourceSize.height: 40
                                    cache: true
                                    asynchronous: false
                                }

                                ColorOverlay {
                                    anchors.fill: toolIcon
                                    source: toolIcon
                                    color: modelData.danger ? "#D9485A" : "#333333"
                                    visible: toolIcon.visible
                                }
                            }
                        }
                    }
                }
            }
        }   // <-- toolbarContainer close

        // Toolbar Handle (when collapsed) - 样式1:1复刻overlay.html (#toolbar-handle)
        Rectangle {
            id: toolbarHandle
            visible: toolbarCollapsed
            z: 100
            width: 32
            height: 12
            radius: 6
            color: currentTheme.toolbarBg
            border.color: currentTheme.toolbarBorder
            border.width: 1
            anchors.bottom: parent.bottom
            anchors.bottomMargin: 4
            anchors.horizontalCenter: parent.horizontalCenter
            opacity: 0.6

            MouseArea {
                anchors.fill: parent
                hoverEnabled: true
                onEntered: parent.opacity = 1
                onExited: parent.opacity = 0.6
                onClicked: toolbarCollapsed = false
            }

            Row {
                anchors.centerIn: parent
                spacing: 2

                Repeater {
                    model: 3
                    delegate: Rectangle {
                        width: 4
                        height: 4
                        radius: 2
                        color: "#333333"
                        opacity: 0.5
                    }
                }
            }
        }

        // Color Picker - 样式1:1复刻overlay.html (#color-picker)
        Rectangle {
            id: colorPicker
            visible: colorPickerVisible
            z: 200
            radius: 12
            color: currentTheme.popupBg
            border.color: currentTheme.popupBorder
            border.width: 1
            width: 280
            height: colorContent.implicitHeight + 32
            anchors.bottom: toolbarPos === "bottom" ? parent.bottom : undefined
            anchors.top: toolbarPos === "top" ? parent.top : undefined
            anchors.left: toolbarPos === "left" ? parent.left : undefined
            anchors.right: toolbarPos === "right" ? parent.right : undefined
            anchors.bottomMargin: toolbarPos === "bottom" ? 80 : 0
            anchors.topMargin: toolbarPos === "top" ? 80 : 0
            anchors.leftMargin: toolbarPos === "left" ? 80 : 0
            anchors.rightMargin: toolbarPos === "right" ? 80 : 0
            anchors.horizontalCenter: toolbarPos === "bottom" || toolbarPos === "top" ? parent.horizontalCenter : undefined
            anchors.verticalCenter: toolbarPos === "left" || toolbarPos === "right" ? parent.verticalCenter : undefined

            Column {
                id: colorContent
                anchors.fill: parent
                anchors.margins: 16
                spacing: 12

                // Pivot Tabs
                Row {
                    spacing: 0

                    Rectangle {
                        width: 60
                        height: 28
                        radius: 6
                        color: currentColorTab === "pen" ? "#333333" : "transparent"

                        MouseArea {
                            anchors.fill: parent
                            onClicked: currentColorTab = "pen"
                        }

                        Label {
                            anchors.centerIn: parent
                            text: "普通笔"
                            color: "#333333"
                            font.pixelSize: 13
                            font.weight: currentColorTab === "pen" ? Font.Medium : Font.Normal
                        }
                    }

                    Rectangle {
                        width: 60
                        height: 28
                        radius: 6
                        color: currentColorTab === "highlight" ? currentTheme.buttonActive : "transparent"

                        MouseArea {
                            anchors.fill: parent
                            onClicked: currentColorTab = "highlight"
                        }

                        Label {
                            anchors.centerIn: parent
                            text: "荧光笔"
                            color: currentTheme.popupFg
                            font.pixelSize: 13
                            font.weight: currentColorTab === "highlight" ? Font.Medium : Font.Normal
                        }
                    }
                }

                // Color Grid
                Grid {
                    columns: 5
                    spacing: 8
                    visible: currentColorTab === "pen"

                    Repeater {
                        model: penColors
                        delegate: Rectangle {
                            width: 36
                            height: 36
                            radius: 6
                            color: modelData.hex
                            border.color: currentPenColorHex === modelData.hex ? currentTheme.accentColor : "transparent"
                            border.width: 2

                            MouseArea {
                                anchors.fill: parent
                                onClicked: root.selectColor(modelData)
                                onEntered: {
                                    colorInfo = {
                                        name: modelData.name,
                                        rgb: modelData.rgb,
                                        hex: modelData.hex
                                    };
                                }
                            }
                        }
                    }
                }

                Grid {
                    columns: 5
                    spacing: 8
                    visible: currentColorTab === "highlight"

                    Repeater {
                        model: highlightColors
                        delegate: Rectangle {
                            width: 36
                            height: 36
                            radius: 6
                            color: modelData.hex
                            border.color: currentPenColorHex === modelData.hex ? currentTheme.accentColor : "transparent"
                            border.width: 2

                            MouseArea {
                                anchors.fill: parent
                                onClicked: root.selectColor(modelData)
                                onEntered: {
                                    colorInfo = {
                                        name: modelData.name,
                                        rgb: modelData.rgb,
                                        hex: modelData.hex
                                    };
                                }
                            }
                        }
                    }
                }

                // Color Info
                Column {
                    spacing: 2

                    Label {
                        text: colorInfo.name
                        color: currentTheme.popupFg
                        font.pixelSize: 14
                        font.weight: Font.Medium
                    }

                    Label {
                        text: "RGB (" + colorInfo.rgb + ")"
                        color: currentTheme.pageHint
                        font.pixelSize: 11
                    }

                    Label {
                        text: colorInfo.hex
                        color: currentTheme.pageHint
                        font.pixelSize: 11
                    }
                }
            }
        }

        // Eraser Popup - 样式1:1复刻overlay.html (#eraser-settings)
        Rectangle {
            id: eraserPopup
            visible: eraserPopupVisible
            z: 200
            radius: 12
            color: currentTheme.popupBg
            border.color: currentTheme.popupBorder
            border.width: 1
            width: 232
            height: eraserContent.implicitHeight + 32
            anchors.bottom: toolbarPos === "bottom" ? parent.bottom : undefined
            anchors.top: toolbarPos === "top" ? parent.top : undefined
            anchors.left: toolbarPos === "left" ? parent.left : undefined
            anchors.right: toolbarPos === "right" ? parent.right : undefined
            anchors.bottomMargin: toolbarPos === "bottom" ? 80 : 0
            anchors.topMargin: toolbarPos === "top" ? 80 : 0
            anchors.leftMargin: toolbarPos === "left" ? 80 : 0
            anchors.rightMargin: toolbarPos === "right" ? 80 : 0
            anchors.horizontalCenter: toolbarPos === "bottom" || toolbarPos === "top" ? parent.horizontalCenter : undefined
            anchors.verticalCenter: toolbarPos === "left" || toolbarPos === "right" ? parent.verticalCenter : undefined

            Column {
                id: eraserContent
                anchors.fill: parent
                anchors.margins: 16
                spacing: 12

                // Slider Container - 样式1:1复刻overlay.html (.slider-container)
                Rectangle {
                    width: 200
                    height: 44
                    radius: 22
                    color: currentTheme.controlBg
                    visible: currentConfig.clearMode !== "button"

                    Label {
                        anchors.centerIn: parent
                        text: "滑动清屏"
                        color: currentTheme.pageHint
                        font.pixelSize: 14
                        opacity: clearSliderProgress > 0.1 ? 0 : 1
                    }

                    Rectangle {
                        id: sliderThumb
                        width: 40
                        height: 40
                        radius: 20
                        color: currentTheme.thumbBg
                        x: 2 + clearSliderProgress * (parent.width - 44)
                        anchors.verticalCenter: parent.verticalCenter

                        Behavior on x {
                            NumberAnimation {
                                duration: root.animationsEnabled ? 100 : 0
                            }
                        }

                        Rectangle {
                            anchors.fill: parent
                            radius: 20
                            color: "transparent"
                            border.color: "#0000001A"
                            border.width: 1
                        }

                        Item {
                            anchors.centerIn: parent
                            width: 20
                            height: 20

                            Image {
                                id: clearIcon
                                anchors.fill: parent
                                source: iconSource("clear")
                                visible: status === Image.Ready
                                fillMode: Image.PreserveAspectFit
                                sourceSize.width: 40
                                sourceSize.height: 40
                                cache: true
                                asynchronous: false
                            }

                            ColorOverlay {
                                anchors.fill: clearIcon
                                source: clearIcon
                                color: "#333333"
                                visible: clearIcon.visible
                            }
                        }

                        MouseArea {
                            anchors.fill: parent
                            drag.target: parent
                            drag.axis: Drag.XAxis
                            drag.minimumX: 2
                            drag.maximumX: parent.parent.width - 44

                            onPositionChanged: {
                                clearSliderProgress = (sliderThumb.x - 2) / (parent.parent.width - 44);
                                if (clearSliderProgress >= 0.9) {
                                    if (overlayBridge && typeof overlayBridge.clearScreen === "function")
                                        overlayBridge.clearScreen();
                                    clearResetTimer.restart();
                                }
                            }

                            onReleased: {
                                if (clearSliderProgress < 0.9) {
                                    clearSliderProgress = 0;
                                }
                            }
                        }
                    }
                }

                // Clear Button - 样式1:1复刻overlay.html (.clear-btn-container)
                Rectangle {
                    width: 200
                    height: 44
                    radius: 22
                    color: clearBtnMouseArea.pressed ? currentTheme.controlActive : (clearBtnMouseArea.containsMouse ? currentTheme.controlHover : currentTheme.controlBg)
                    visible: currentConfig.clearMode === "button" || currentConfig.showClear === false

                    MouseArea {
                        id: clearBtnMouseArea
                        anchors.fill: parent
                        hoverEnabled: true
                        onClicked: {
                            if (overlayBridge && typeof overlayBridge.clearScreen === "function")
                                overlayBridge.clearScreen();
                            root.closePopup();
                        }
                    }

                    Row {
                        anchors.centerIn: parent
                        spacing: 8

                        Item {
                            width: 24
                            height: 24

                            Image {
                                id: clearBtnIcon
                                anchors.fill: parent
                                source: iconSource("clear")
                                visible: status === Image.Ready
                                fillMode: Image.PreserveAspectFit
                                sourceSize.width: 48
                                sourceSize.height: 48
                                cache: true
                                asynchronous: false
                            }

                            ColorOverlay {
                                anchors.fill: clearBtnIcon
                                source: clearBtnIcon
                                color: "#333333"
                                visible: clearBtnIcon.visible
                            }
                        }

                        Label {
                            text: "清空屏幕"
                            color: currentTheme.popupFg
                            font.pixelSize: 14
                            font.weight: Font.Medium
                        }
                    }
                }
            }
        }

        // Page Selector - 样式1:1复刻overlay.html (#page-selector)
        Rectangle {
            id: pageSelector
            visible: pageSelectorVisible
            z: 200
            radius: 12
            color: currentTheme.popupBg
            border.color: currentTheme.popupBorder
            border.width: 1
            width: 200
            height: root.height - 48
            anchors.top: parent.top
            anchors.topMargin: 24
            anchors.left: pageSelectorLeftSide ? parent.left : undefined
            anchors.leftMargin: pageSelectorLeftSide ? 16 : 0
            anchors.right: pageSelectorLeftSide ? undefined : parent.right
            anchors.rightMargin: pageSelectorLeftSide ? 0 : 16

            Flickable {
                anchors.fill: parent
                anchors.margins: 12
                contentHeight: pageGrid.height
                clip: true

                Column {
                    id: pageGrid
                    spacing: 8
                    width: parent.width

                    Repeater {
                        model: totalPage
                        delegate: Rectangle {
                            width: parent.width
                            height: 60
                            radius: 8
                            color: (index + 1) === currentPage ? currentTheme.buttonActive : (pageItemMouseArea.containsMouse ? currentTheme.buttonHover : "transparent")
                            border.color: (index + 1) === currentPage ? currentTheme.accentColor : "transparent"
                            border.width: 1

                            MouseArea {
                                id: pageItemMouseArea
                                anchors.fill: parent
                                hoverEnabled: true
                                onClicked: {
                                    if (overlayBridge && typeof overlayBridge.gotoSlide === "function")
                                        overlayBridge.gotoSlide(index + 1);
                                    root.closePopup();
                                }
                            }

                            Row {
                                anchors.fill: parent
                                anchors.margins: 8
                                spacing: 8

                                Rectangle {
                                    width: 80
                                    height: 44
                                    radius: 4
                                    color: currentTheme.controlBg

                                    Image {
                                        anchors.fill: parent
                                        anchors.margins: 2
                                        source: thumbnailSources[index + 1] || ""
                                        fillMode: Image.PreserveAspectFit
                                        visible: !!thumbnailSources[index + 1]
                                    }

                                    Label {
                                        anchors.centerIn: parent
                                        text: String(index + 1)
                                        color: currentTheme.pageHint
                                        font.pixelSize: 14
                                        visible: !thumbnailSources[index + 1]
                                    }
                                }

                                Label {
                                    text: "第" + (index + 1) + "页"
                                    color: currentTheme.popupFg
                                    font.pixelSize: 12
                                    anchors.verticalCenter: parent.verticalCenter
                                }
                            }
                        }
                    }
                }
            }
        }

        // Ink Prompt - 样式1:1复刻overlay.html (#ink-prompt)
        Rectangle {
            id: inkPromptMask
            visible: inkPromptVisible
            z: 9999
            anchors.fill: parent
            color: currentTheme.dialogMask

            MouseArea {
                anchors.fill: parent
                onClicked: {} // Block clicks
            }

            // Ink Card - 样式1:1复刻overlay.html (.ink-card)
            Rectangle {
                anchors.centerIn: parent
                width: Math.min(520, parent.width - 48)
                height: inkCardContent.implicitHeight + 44
                radius: 16
                color: currentTheme.dialogBg
                border.color: currentTheme.dialogBorder
                border.width: 1

                Column {
                    id: inkCardContent
                    anchors.fill: parent
                    anchors.margins: 22
                    spacing: 12

                    Label {
                        text: "是否保留墨迹注释？"
                        color: currentTheme.dialogTitle
                        font.pixelSize: 18
                        font.weight: Font.Black
                    }

                    Label {
                        text: "检测到放映期间添加了墨迹注释，是否保留到幻灯片中？"
                        color: currentTheme.dialogText
                        font.pixelSize: 14
                        font.weight: Font.Bold
                        wrapMode: Text.Wrap
                        width: parent.width
                    }

                    Row {
                        anchors.right: parent.right
                        spacing: 12
                        topPadding: 4

                        // Cancel Button
                        Rectangle {
                            width: cancelLabel.implicitWidth + 36
                            height: 36
                            radius: 10
                            color: cancelMouseArea.pressed ? currentTheme.buttonActive : (cancelMouseArea.containsMouse ? currentTheme.buttonHover : Qt.rgba(0, 0, 0, 0.08))

                            MouseArea {
                                id: cancelMouseArea
                                anchors.fill: parent
                                hoverEnabled: true
                                onClicked: {
                                    if (overlayBridge && typeof overlayBridge.inkPromptResult === "function")
                                        overlayBridge.inkPromptResult(false);
                                }
                            }

                            Label {
                                id: cancelLabel
                                anchors.centerIn: parent
                                text: "不保留"
                                color: currentTheme.dialogTitle
                                font.pixelSize: 14
                                font.weight: Font.Black
                            }
                        }

                        // Confirm Button
                        Rectangle {
                            width: confirmLabel.implicitWidth + 36
                            height: 36
                            radius: 10
                            color: confirmMouseArea.pressed ? Qt.darker(currentTheme.accentColor, 1.1) : (confirmMouseArea.containsMouse ? Qt.lighter(currentTheme.accentColor, 1.1) : currentTheme.accentColor)

                            MouseArea {
                                id: confirmMouseArea
                                anchors.fill: parent
                                hoverEnabled: true
                                onClicked: {
                                    if (overlayBridge && typeof overlayBridge.inkPromptResult === "function")
                                        overlayBridge.inkPromptResult(true);
                                }
                            }

                            Label {
                                id: confirmLabel
                                anchors.centerIn: parent
                                text: "保留"
                                color: "#FFFFFF"
                                font.pixelSize: 14
                                font.weight: Font.Black
                            }
                        }
                    }
                }
            }
        }
    }
}
