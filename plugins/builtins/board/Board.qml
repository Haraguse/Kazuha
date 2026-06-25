import QtQuick 2.15
import QtQml 2.15
import QtQuick.Window 2.15
import Qt5Compat.GraphicalEffects
import LuminaliumBoard 1.0

Rectangle {
    id: root
    color: "transparent"
    focus: true

    property string toolbarPosition: typeof boardToolbarPosition !== "undefined" ? boardToolbarPosition : "bottom"
    property string backgroundColor: typeof boardBackgroundColor !== "undefined" ? boardBackgroundColor : "#202020"
    property string popupBackgroundColor: typeof boardPopupBackgroundColor !== "undefined" ? boardPopupBackgroundColor : ""
    property string popupBorderColor: typeof boardPopupBorderColor !== "undefined" ? boardPopupBorderColor : ""
    property int eraserMode: typeof boardEraserMode !== "undefined" ? boardEraserMode : 0
    property bool penStrokeEnabled: typeof boardPenStrokeEnabled !== "undefined" ? boardPenStrokeEnabled : false
    property bool darkBackground: isDarkColor(backgroundColor)
    property real performanceScale: Math.max(1.0, Math.max(width, height) / 1600.0)
    property real toolbarOpacity: (typeof boardToolbarOpacity !== "undefined" && boardToolbarOpacity > 0.0)
                                 ? boardToolbarOpacity : 0.92
    property var boardPages: []
    property int currentBoardPage: 1
    property int tbH: (typeof titleBarHeight !== "undefined") ? titleBarHeight : 32

    // Theme from overlay design
    property color accentColor:        themeVal("accent", "#3275F5")
    property color toolbarBg:          themeVal("toolbar_bg", darkBackground ? "#202020" : "#FFFFFF")
    property color toolbarFg:          themeVal("toolbar_fg", darkBackground ? "#FFFFFF" : "#191919")
    property color toolbarBorder:      themeVal("toolbar_border", darkBackground ? "#28FFFFFF" : "#14000000")
    property color toolbarLine:        themeVal("toolbar_line", darkBackground ? "#26FFFFFF" : "#26000000")
    property color buttonHover:        themeVal("btn_hover_bg", darkBackground ? "#14FFFFFF" : "#0A000000")
    property color buttonActive:       themeVal("btn_active_bg", darkBackground ? "#1EFFFFFF" : "#1E000000")
    property color popupBg:            popupBackgroundColor !== "" ? popupBackgroundColor : themeVal("popup_bg", toolbarBg)
    property color popupBorder:        popupBorderColor !== "" ? popupBorderColor : themeVal("popup_border", toolbarBorder)
    property color popupFg:            themeVal("popup_fg", toolbarFg)
    property color textSecondary:      themeVal("item_hover", darkBackground ? "#90FFFFFF" : "#99000000")
    property color controlBg:          themeVal("card_bg", darkBackground ? "#14FFFFFF" : "#0A000000")
    property string fontFamily:        typeof boardFontFamily !== "undefined" ? boardFontFamily : ""

    function themeVal(key, fallback) {
        return boardTheme ? (boardTheme[key] || fallback) : fallback;
    }

    Keys.onEscapePressed: backend.closeWindow()

    function isDarkColor(value) {
        // Normalize QColor / color object to a string
        var colorString = "";
        if (typeof value === "string") {
            colorString = value;
        } else if (value && typeof value.toString === "function") {
            // QColor and other color objects produce a #RRGGBB(AA) or rgba() string
            colorString = value.toString();
        }
        if (!colorString) return true;

        // Try hex #RRGGBB / #RGB
        var hexMatch = colorString.match(/^#([0-9A-Fa-f]{6})$/);
        if (hexMatch) {
            var hex = hexMatch[1];
            var r = parseInt(hex.slice(0, 2), 16);
            var g = parseInt(hex.slice(2, 4), 16);
            var b = parseInt(hex.slice(4, 6), 16);
            var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            return luminance < 128;
        }

        // Try rgb() / rgba()
        var rgbMatch = colorString.match(/rgba?\s*\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)/i);
        if (rgbMatch) {
            var rr = parseFloat(rgbMatch[1]);
            var gg = parseFloat(rgbMatch[2]);
            var bb = parseFloat(rgbMatch[3]);
            var lum = 0.299 * rr + 0.587 * gg + 0.114 * bb;
            // If rgb values are 0-1, scale them
            if (rr <= 1.0 && gg <= 1.0 && bb <= 1.0) {
                lum = 0.299 * rr * 255 + 0.587 * gg * 255 + 0.114 * bb * 255;
            }
            return lum < 128;
        }

        // Default to dark for unrecognised values
        return true;
    }

    function isArrayLike(value) {
        return value && typeof value !== "string" && typeof value.length === "number";
    }

    function createEmptyBoardPage() {
        return { strokes: [], thumb: "" };
    }

    function cloneStrokeList(strokes) {
        return canvas.cloneLines((strokes && isArrayLike(strokes)) ? strokes : []);
    }

    function normalizeBoardPage(pageData) {
        var strokes = [];
        if (pageData && isArrayLike(pageData.strokes)) {
            strokes = cloneStrokeList(pageData.strokes);
        }
        return { strokes: strokes, thumb: "" };
    }

    function ensureBoardPages() {
        if (!isArrayLike(boardPages) || boardPages.length === 0) {
            boardPages = [createEmptyBoardPage()];
        } else {
            var normalizedPages = [];
            for (var i = 0; i < boardPages.length; i++) {
                normalizedPages.push(normalizeBoardPage(boardPages[i]));
            }
            boardPages = normalizedPages;
        }
        if (currentBoardPage < 1) currentBoardPage = 1;
        if (currentBoardPage > boardPages.length) currentBoardPage = boardPages.length;
    }

    function persistCurrentPageToModel() {
        ensureBoardPages();
        var pageIndex = Math.max(0, Math.min(boardPages.length - 1, currentBoardPage - 1));
        var page = normalizeBoardPage(boardPages[pageIndex] || createEmptyBoardPage());
        page.strokes = cloneStrokeList(canvas.getStrokes());
        boardPages[pageIndex] = page;
        boardPages = boardPages.slice(0);
        return page;
    }

    function applyCurrentBoardPage() {
        ensureBoardPages();
        var pageIndex = Math.max(0, Math.min(boardPages.length - 1, currentBoardPage - 1));
        var page = normalizeBoardPage(boardPages[pageIndex] || createEmptyBoardPage());
        canvas.setStrokes(page.strokes);
    }

    function getBoardDocument() {
        persistCurrentPageToModel();
        var pagesOut = [];
        for (var i = 0; i < boardPages.length; i++) {
            var page = normalizeBoardPage(boardPages[i]);
            pagesOut.push({ strokes: cloneStrokeList(page.strokes), thumb: "" });
        }
        return { currentPage: currentBoardPage, pages: pagesOut };
    }

    function setBoardDocument(documentData) {
        var pages = [];
        var currPage = 1;
        if (isArrayLike(documentData)) {
            pages = [{ strokes: cloneStrokeList(documentData), thumb: "" }];
        } else if (documentData && isArrayLike(documentData.pages) && documentData.pages.length > 0) {
            for (var i = 0; i < documentData.pages.length; i++) {
                pages.push(normalizeBoardPage(documentData.pages[i]));
            }
            currPage = parseInt(documentData.currentPage || 1);
            if (isNaN(currPage)) currPage = 1;
        }
        if (pages.length === 0) {
            pages = [createEmptyBoardPage()];
            currPage = 1;
        }
        boardPages = pages;
        currentBoardPage = Math.max(1, Math.min(boardPages.length, currPage));
        applyCurrentBoardPage();
    }

    Component.onCompleted: {
        ensureBoardPages();
        applyCurrentBoardPage();
    }

    // Helper to apply opacity to a color
    function applyOpacityToColor(color, opacity) {
        return Qt.rgba(color.r, color.g, color.b, color.a * opacity);
    }

    function getIconPath(name) {
        return iconsDir + name + ".svg";
    }

    // ── Main drawing area ────────────────────────────────────────────────────
    Rectangle {
        id: board
        anchors.top:    parent.top
        anchors.left:   parent.left
        anchors.right:  parent.right
        anchors.bottom: parent.bottom
        color: backgroundColor
        clip: true

        NativeBoardItem {
            id: canvas
            objectName: "canvas"
            anchors.fill: parent
            backgroundColor: root.backgroundColor
            minSegmentPx: minSegmentPixels

            property color drawColor: darkBackground ? "white" : "black"
            property int lineWidth: 3
            property int eraserWidth: 20
            property bool isEraser: false
            property int eraserMode: root.eraserMode
            property int currentStrokeId: 0
            property bool penStrokeEnabled: root.penStrokeEnabled
            property real lastWidth: lineWidth
            property real minSegmentPixels: 0.6 * root.performanceScale
            property real minSegmentPixelsSquared: minSegmentPixels * minSegmentPixels
            property real maxSegmentPixels: 16.0 * root.performanceScale
            property var undoStack: []
            property var redoStack: []
            property var gestureAddedLines: []
            property var gestureRemovedStrokes: []
            property var gestureRemovedStrokeIds: ({})
            property var lastX
            property var lastY
            property real lastRawX: 0
            property real lastRawY: 0
            property real lastFilteredX: 0
            property real lastFilteredY: 0
            property bool pointerActive: false
            property var allLines: []
            readonly property bool canUndo: undoStack.length > 0
            readonly property bool canRedo: redoStack.length > 0

            onWidthChanged: requestRepaintAll()
            onHeightChanged: requestRepaintAll()

            function drawLine(ctx, line) {
                addLine(line.x1, line.y1, line.x2, line.y2, line.width,
                        line.color.toString(), !!line.isEraser, line.width || 20, line.strokeId);
            }

            function cloneLines(lines) {
                var result = [];
                for (var i = 0; i < lines.length; i++) {
                    var line = lines[i];
                    result.push({
                        x1: line.x1, y1: line.y1, x2: line.x2, y2: line.y2,
                        color: line.color, width: line.width,
                        isEraser: line.isEraser, strokeId: line.strokeId
                    });
                }
                return result;
            }

            function cloneRemovedStrokes(strokes) {
                var result = [];
                for (var i = 0; i < strokes.length; i++) {
                    result.push({
                        strokeId: strokes[i].strokeId,
                        startIndex: strokes[i].startIndex,
                        lines: cloneLines(strokes[i].lines)
                    });
                }
                return result;
            }

            function applyLines(lines) {
                allLines = cloneLines(lines);
                canvas.setAllLines(allLines);
            }

            function enqueueLine(line) {
                allLines.push(line);
                gestureAddedLines.push(line);
                canvas.addLine(line.x1, line.y1, line.x2, line.y2, line.width,
                               line.color || canvas.drawColor.toString(), !!line.isEraser,
                               line.eraserPx || canvas.eraserWidth, line.strokeId);
            }

            function calcDynamicWidth(currentX, currentY) {
                var width = canvas.isEraser ? canvas.eraserWidth : canvas.lineWidth;
                if (canvas.penStrokeEnabled && !canvas.isEraser) {
                    var dx = currentX - canvas.lastX;
                    var dy = currentY - canvas.lastY;
                    var dist = Math.sqrt(dx * dx + dy * dy);
                    var speed = dist * Math.max(canvas.width, canvas.height);
                    var minW = Math.max(1, canvas.lineWidth * 0.6);
                    var maxW = canvas.lineWidth * 1.8;
                    var t = Math.min(1, speed / 25);
                    width = maxW - (maxW - minW) * t;
                    width = (width + canvas.lastWidth) / 2;
                    canvas.lastWidth = width;
                }
                return width;
            }

            function appendSegmentTo(currentX, currentY) {
                var line = {
                    x1: canvas.lastX, y1: canvas.lastY,
                    x2: currentX, y2: currentY,
                    color: canvas.drawColor.toString(),
                    width: calcDynamicWidth(currentX, currentY),
                    isEraser: canvas.isEraser,
                    strokeId: canvas.currentStrokeId
                };
                canvas.enqueueLine(line);
                canvas.lastX = currentX;
                canvas.lastY = currentY;
            }

            function processInputPoint(currentX, currentY, forceFinal) {
                var w = Math.max(1, canvas.width);
                var h = Math.max(1, canvas.height);
                var rawDxPx = (currentX - canvas.lastRawX) * w;
                var rawDyPx = (currentY - canvas.lastRawY) * h;
                var rawDistSquared = rawDxPx * rawDxPx + rawDyPx * rawDyPx;
                canvas.lastRawX = currentX;
                canvas.lastRawY = currentY;
                if (!forceFinal && rawDistSquared < 0.09) return;
                if (canvas.isEraser && canvas.eraserMode === 1) {
                    if (rawDistSquared < canvas.lowSamplePixelsSquared && !forceFinal) return;
                    var hitId = canvas.hitTest(currentX, currentY);
                    if (hitId !== -1 && !canvas.gestureRemovedStrokeIds[hitId]) {
                        var removedStroke = canvas.removeStroke(hitId);
                        if (removedStroke) {
                            canvas.gestureRemovedStrokes.push(removedStroke);
                            canvas.gestureRemovedStrokeIds[hitId] = true;
                        }
                    }
                    return;
                }
                canvas.lastFilteredX = currentX;
                canvas.lastFilteredY = currentY;
                appendDirectly(currentX, currentY, forceFinal);
            }

            function appendDirectly(targetX, targetY, forceFinal) {
                var w = canvas.width || 1;
                var h = canvas.height || 1;
                var startX = canvas.lastX;
                var startY = canvas.lastY;
                var dxPx = (targetX - startX) * w;
                var dyPx = (targetY - startY) * h;
                var movePxSquared = dxPx * dxPx + dyPx * dyPx;
                if (!forceFinal && movePxSquared < 0.5) return;
                var maxSegSq = canvas.maxSegmentPixels * canvas.maxSegmentPixels;
                if (movePxSquared > maxSegSq && !forceFinal) {
                    var midX = (startX + targetX) * 0.5;
                    var midY = (startY + targetY) * 0.5;
                    appendSegmentTo(midX, midY);
                }
                appendSegmentTo(targetX, targetY);
            }

            function pushHistoryAction(action) {
                undoStack = undoStack.concat([action]);
                redoStack = [];
            }

            function beginGesture() {
                gestureAddedLines = [];
                gestureRemovedStrokes = [];
                gestureRemovedStrokeIds = ({});
            }

            function commitGesture() {
                if (gestureAddedLines.length > 0) {
                    pushHistoryAction({ type: "add", strokeId: currentStrokeId, lines: cloneLines(gestureAddedLines) });
                } else if (gestureRemovedStrokes.length > 0) {
                    pushHistoryAction({ type: "remove", strokes: cloneRemovedStrokes(gestureRemovedStrokes) });
                }
                gestureAddedLines = [];
                gestureRemovedStrokes = [];
                gestureRemovedStrokeIds = ({});
            }

            function undo() {
                if (!canUndo) return;
                var action = undoStack[undoStack.length - 1];
                undoStack = undoStack.slice(0, undoStack.length - 1);
                if (action.type === "add") {
                    removeStroke(action.strokeId);
                } else if (action.type === "remove") {
                    restoreRemovedStrokes(action.strokes);
                } else if (action.type === "clear") {
                    applyLines(action.lines);
                }
                redoStack = redoStack.concat([action]);
            }

            function redo() {
                if (!canRedo) return;
                var action = redoStack[redoStack.length - 1];
                redoStack = redoStack.slice(0, redoStack.length - 1);
                if (action.type === "add") {
                    allLines = allLines.concat(cloneLines(action.lines));
                    canvas.setAllLines(allLines);
                } else if (action.type === "remove") {
                    for (var i = 0; i < action.strokes.length; i++) removeStroke(action.strokes[i].strokeId);
                } else if (action.type === "clear") {
                    applyLines([]);
                }
                undoStack = undoStack.concat([action]);
            }

            function hitTest(x, y) {
                var w = width;
                var h = height;
                if (w <= 0 || h <= 0) return -1;
                var t = 10 / w;
                var tSquared = t * t;
                for (var i = allLines.length - 1; i >= 0; i--) {
                    var line = allLines[i];
                    if (line.isEraser) continue;
                    var minX = Math.min(line.x1, line.x2) - t;
                    var maxX = Math.max(line.x1, line.x2) + t;
                    var minY = Math.min(line.y1, line.y2) - t;
                    var maxY = Math.max(line.y1, line.y2) + t;
                    if (x >= minX && x <= maxX && y >= minY && y <= maxY) {
                        var distSquared = distToSegmentSquared(x, y, line.x1, line.y1, line.x2, line.y2);
                        if (distSquared < tSquared) return line.strokeId;
                    }
                }
                return -1;
            }

            function distToSegmentSquared(x, y, x1, y1, x2, y2) {
                var A = x - x1;
                var B = y - y1;
                var C = x2 - x1;
                var D = y2 - y1;
                var dot = A * C + B * D;
                var len_sq = C * C + D * D;
                var param = -1;
                if (len_sq !== 0) param = dot / len_sq;
                var xx, yy;
                if (param < 0) { xx = x1; yy = y1; }
                else if (param > 1) { xx = x2; yy = y2; }
                else { xx = x1 + param * C; yy = y1 + param * D; }
                var dx = x - xx;
                var dy = y - yy;
                return dx * dx + dy * dy;
            }

            function removeStroke(strokeId) {
                if (strokeId === undefined || strokeId === null || strokeId === -1) return;
                var newLines = [];
                var removedLines = [];
                var changed = false;
                var startIndex = -1;
                for (var i = 0; i < allLines.length; i++) {
                    if (allLines[i].strokeId !== strokeId) {
                        newLines.push(allLines[i]);
                    } else {
                        if (startIndex === -1) startIndex = i;
                        removedLines.push(allLines[i]);
                        changed = true;
                    }
                }
                if (changed) {
                    allLines = newLines;
                    canvas.removeStrokeAndRepaint(strokeId);
                    return { strokeId: strokeId, startIndex: startIndex, lines: cloneLines(removedLines) };
                }
                return null;
            }

            function restoreRemovedStrokes(strokes) {
                if (!strokes || strokes.length === 0) return;
                var sorted = cloneRemovedStrokes(strokes);
                sorted.sort(function(a, b) { return a.startIndex - b.startIndex; });
                var lines = cloneLines(allLines);
                var offset = 0;
                for (var i = 0; i < sorted.length; i++) {
                    var stroke = sorted[i];
                    var insertAt = stroke.startIndex + offset;
                    if (insertAt < 0) insertAt = 0;
                    if (insertAt > lines.length) insertAt = lines.length;
                    lines = lines.slice(0, insertAt).concat(cloneLines(stroke.lines), lines.slice(insertAt));
                    offset += stroke.lines.length;
                }
                applyLines(lines);
            }

            MouseArea {
                anchors.fill: parent
                hoverEnabled: false
                onPressed: (mouse) => {
                    canvas.beginGesture();
                    var x = mouse.x / canvas.width;
                    var y = mouse.y / canvas.height;
                    canvas.lastX = x; canvas.lastY = y;
                    canvas.lastRawX = x; canvas.lastRawY = y;
                    canvas.lastFilteredX = x; canvas.lastFilteredY = y;
                    canvas.pointerActive = true;
                    canvas.currentStrokeId++;
                    canvas.lastWidth = canvas.lineWidth;
                }
                onReleased: (mouse) => {
                    var x = mouse.x / canvas.width;
                    var y = mouse.y / canvas.height;
                    canvas.processInputPoint(x, y, true);
                    canvas.pointerActive = false;
                    canvas.commitGesture();
                    canvas.lastWidth = canvas.lineWidth;
                }
                onCanceled: {
                    canvas.pointerActive = false;
                    canvas.commitGesture();
                    canvas.lastWidth = canvas.lineWidth;
                }
                onPositionChanged: (mouse) => {
                    var currentX = mouse.x / canvas.width;
                    var currentY = mouse.y / canvas.height;
                    canvas.processInputPoint(currentX, currentY, false);
                }
            }

            function clear() {
                if (canvas.allLines.length === 0) return;
                pushHistoryAction({ type: "clear", lines: cloneLines(canvas.allLines) });
                applyLines([]);
            }

            function getStrokes() {
                var result = [];
                for (var i = 0; i < canvas.allLines.length; i++) result.push(canvas.allLines[i]);
                return result;
            }

            function setStrokes(strokes) {
                var newStrokes = [];
                var w = width || 800;
                var h = height || 600;
                if (w === 0) w = 800;
                if (h === 0) h = 600;
                for (var i = 0; i < strokes.length; i++) {
                    var s = strokes[i];
                    var line = {
                        x1: s.x1, y1: s.y1, x2: s.x2, y2: s.y2,
                        color: s.color, width: s.width,
                        isEraser: s.isEraser, strokeId: s.strokeId
                    };
                    if (line.x1 > 1.1 || line.y1 > 1.1 || line.x2 > 1.1 || line.y2 > 1.1) {
                        line.x1 /= w; line.y1 /= h; line.x2 /= w; line.y2 /= h;
                    }
                    if (line.strokeId === undefined) line.strokeId = -1000 - i;
                    newStrokes.push(line);
                }
                canvas.allLines = newStrokes;
                canvas.undoStack = [];
                canvas.redoStack = [];
                canvas.gestureAddedLines = [];
                canvas.gestureRemovedStrokes = [];
                canvas.gestureRemovedStrokeIds = ({});
                var maxStrokeId = 0;
                for (var j = 0; j < newStrokes.length; j++) {
                    if (newStrokes[j].strokeId > maxStrokeId) maxStrokeId = newStrokes[j].strokeId;
                }
                canvas.currentStrokeId = maxStrokeId;
                canvas.setAllLines(newStrokes);
            }
        }
    }

    // Watermark
        Text {
            anchors.right:  board.right
            anchors.bottom: board.bottom
            anchors.rightMargin:  15
            anchors.bottomMargin: 10
            text: typeof watermarkText !== "undefined" ? watermarkText : ""
            color: darkBackground ? Qt.rgba(1, 1, 1, 0.3) : Qt.rgba(0, 0, 0, 0.3)
            font.family: root.fontFamily
            font.pixelSize: 12
            horizontalAlignment: Text.AlignRight
            visible: typeof showWatermark !== "undefined" ? showWatermark : false
            z: 100
        }

    // ── Shared outside-click overlay (closes popups) ─────────────────────────
    MouseArea {
        id: popupOverlay
        anchors.fill: board
        z: 98
        enabled: colorPopup.visible || eraserPopup.visible
        onClicked: {
            colorPopup.close();
            eraserPopup.close();
        }
    }

    // ── Color Popup ──────────────────────────────────────────────────────────
    Rectangle {
        id: colorPopup
        parent: root
        x: popupXPosition()
        y: popupYPosition(height)
        width: 316
        height: 236
        z: 100
        radius: 12
        color: popupBg
        border.color: popupBorder
        border.width: 1
        clip: true

        property real popupOpacity: 0.0
        visible: popupOpacity > 0.0
        opacity: popupOpacity
        Behavior on popupOpacity { NumberAnimation { duration: 200; easing.type: Easing.OutQuad } }

        layer.enabled: true
        layer.effect: DropShadow {
            verticalOffset: 4
            radius: 16
            samples: 33
            color: darkBackground ? "#50000000" : "#30000000"
        }

        function popupXPosition() {
            if (toolbarPosition === "left")  return toolbar.x + toolbar.width + 12;
            if (toolbarPosition === "right") return toolbar.x - width - 12;
            return (root.width - width) / 2;
        }
        function popupYPosition(popupHeight) {
            if (toolbarPosition === "top")    return toolbar.y + toolbar.height + 12;
            if (toolbarPosition === "bottom") return toolbar.y - popupHeight - 12;
            return toolbar.y + (toolbar.height - popupHeight) / 2;
        }

        function open()  { popupOpacity = 1.0; }
        function close() { popupOpacity = 0.0; }
        property bool opened: popupOpacity > 0.0
        property int activeTab: 0
        property string hoveredColorName: ""
        property string hoveredColorRgb: ""
        property string hoveredColorHex: ""

        Item {
            anchors.fill: parent
            anchors.margins: 16

            // Tab Header
            Row {
                id: tabHeader
                height: 40
                anchors.top: parent.top
                anchors.left: parent.left
                anchors.right: parent.right
                spacing: 24

                Item {
                    width: tabThemeText.contentWidth
                    height: parent.height
                    Text {
                        id: tabThemeText
                        text: themeColorsText
                        color: colorPopup.activeTab === 0 ? popupFg : textSecondary
                        font.family: root.fontFamily
                        font.pixelSize: 13
                        font.bold: colorPopup.activeTab === 0
                        anchors.centerIn: parent
                        MouseArea {
                            anchors.fill: parent
                            cursorShape: Qt.PointingHandCursor
                            onClicked: colorPopup.activeTab = 0
                        }
                    }
                    Rectangle {
                        height: 3
                        radius: 1.5
                        color: accentColor
                        anchors.bottom: parent.bottom
                        anchors.left: parent.left
                        anchors.right: parent.right
                        visible: colorPopup.activeTab === 0
                    }
                }

                Item {
                    width: tabStandardText.contentWidth
                    height: parent.height
                    Text {
                        id: tabStandardText
                        text: standardColorsText
                        color: colorPopup.activeTab === 1 ? popupFg : textSecondary
                        font.family: root.fontFamily
                        font.pixelSize: 13
                        font.bold: colorPopup.activeTab === 1
                        anchors.centerIn: parent
                        MouseArea {
                            anchors.fill: parent
                            cursorShape: Qt.PointingHandCursor
                            onClicked: colorPopup.activeTab = 1
                        }
                    }
                    Rectangle {
                        height: 3
                        radius: 1.5
                        color: accentColor
                        anchors.bottom: parent.bottom
                        anchors.left: parent.left
                        anchors.right: parent.right
                        visible: colorPopup.activeTab === 1
                    }
                }
            }

            Rectangle {
                anchors.top: tabHeader.bottom
                anchors.left: parent.left
                anchors.right: parent.right
                height: 1
                color: toolbarLine
            }

            // Content
            Item {
                anchors.top: tabHeader.bottom
                anchors.topMargin: 12
                anchors.bottom: infoBox.top
                anchors.bottomMargin: 12
                anchors.left: parent.left
                anchors.right: parent.right

                Grid {
                    visible: colorPopup.activeTab === 0
                    columns: 10
                    spacing: 8
                    anchors.centerIn: parent
                    Repeater {
                        model: themeColors
                        delegate: Rectangle {
                            width: 22
                            height: 22
                            color: modelData
                            radius: 4
                            border.width: 1
                            border.color: toolbarLine
                            MouseArea {
                                anchors.fill: parent
                                cursorShape: Qt.PointingHandCursor
                                hoverEnabled: true
                                onEntered: {
                                    parent.scale = 1.2; parent.z = 1;
                                    parent.border.color = popupFg;
                                    parent.border.width = 2;
                                    colorPopup.updateInfo(modelData);
                                }
                                onExited: {
                                    parent.scale = 1.0; parent.z = 0;
                                    parent.border.color = toolbarLine;
                                    parent.border.width = 1;
                                    colorPopup.clearInfo();
                                }
                                onClicked: {
                                    if (canvas) { canvas.drawColor = modelData; canvas.isEraser = false; }
                                    colorPopup.close();
                                }
                            }
                        }
                    }
                }

                Grid {
                    visible: colorPopup.activeTab === 1
                    columns: 10
                    spacing: 8
                    anchors.centerIn: parent
                    Repeater {
                        model: standardColors
                        delegate: Rectangle {
                            width: 22
                            height: 22
                            color: modelData
                            radius: 4
                            border.width: 1
                            border.color: toolbarLine
                            MouseArea {
                                anchors.fill: parent
                                cursorShape: Qt.PointingHandCursor
                                hoverEnabled: true
                                onEntered: {
                                    parent.scale = 1.2; parent.z = 1;
                                    parent.border.color = popupFg;
                                    parent.border.width = 2;
                                    colorPopup.updateInfo(modelData);
                                }
                                onExited: {
                                    parent.scale = 1.0; parent.z = 0;
                                    parent.border.color = toolbarLine;
                                    parent.border.width = 1;
                                    colorPopup.clearInfo();
                                }
                                onClicked: {
                                    canvas.drawColor = modelData;
                                    canvas.isEraser = false;
                                    colorPopup.close();
                                }
                            }
                        }
                    }
                }

                Row {
                    id: penSizeRow
                    anchors.horizontalCenter: parent.horizontalCenter
                    anchors.bottom: parent.bottom
                    spacing: 8

                    Text {
                        text: penSizeText
                        color: popupFg
                        font.family: root.fontFamily
                        font.pixelSize: 12
                    }

                    SliderHandle {
                        from: 1; to: 18; stepSize: 1
                        value: canvas ? canvas.lineWidth : 3
                        onMoved: (v) => { if (canvas) canvas.lineWidth = Math.round(v); }
                    }

                    Text {
                        text: canvas ? Math.round(canvas.lineWidth) : 3
                        color: textSecondary
                        font.family: root.fontFamily
                        font.pixelSize: 11
                    }
                }
            }

            // Info Box
            Item {
                id: infoBox
                height: 44
                anchors.bottom: parent.bottom
                anchors.left: parent.left
                anchors.right: parent.right

                Rectangle {
                    anchors.top: parent.top
                    anchors.left: parent.left
                    anchors.right: parent.right
                    height: 1
                    color: toolbarLine
                }

                Row {
                    anchors.centerIn: parent
                    spacing: 8
                    Rectangle {
                        width: 16
                        height: 16
                        radius: 4
                        color: colorPopup.hoveredColorHex !== "" ? colorPopup.hoveredColorHex : (canvas ? canvas.drawColor : "transparent")
                        border.width: 1
                        border.color: toolbarLine
                        anchors.verticalCenter: parent.verticalCenter
                    }
                    Column {
                        anchors.verticalCenter: parent.verticalCenter
                        spacing: 2
                        Text {
                            text: colorPopup.hoveredColorName !== "" ? colorPopup.hoveredColorName : (colorPopup.hoveredColorHex !== "" ? colorPopup.hoveredColorHex : (canvas ? canvas.drawColor.toString() : ""))
                            color: popupFg
                            font.family: root.fontFamily
                            font.pixelSize: 12
                            font.bold: true
                        }
                        Text {
                            text: colorPopup.hoveredColorRgb !== "" ? colorPopup.hoveredColorRgb : ""
                            color: textSecondary
                            font.family: root.fontFamily
                            font.pixelSize: 10
                            visible: text !== ""
                        }
                    }
                }
            }
        }

        function updateInfo(colorVal) {
            hoveredColorHex = colorVal;
            hoveredColorRgb = getRgbString(colorVal);
            hoveredColorName = getColorName(colorVal);
        }
        function clearInfo() {
            hoveredColorHex = "";
            hoveredColorRgb = "";
            hoveredColorName = "";
        }
        function getRgbString(hex) {
            if (!hex || hex.length < 7) return "";
            var r = parseInt(hex.substring(1, 3), 16);
            var g = parseInt(hex.substring(3, 5), 16);
            var b = parseInt(hex.substring(5, 7), 16);
            return "RGB(" + r + ", " + g + ", " + b + ")";
        }
        function getColorName(hex) {
            var upper = hex.toUpperCase();
            var map = {
                "#FFFFFF": "White", "#000000": "Black", "#E7E6E6": "Light Gray", "#44546A": "Blue Gray", "#4472C4": "Blue",
                "#ED7D31": "Orange", "#A5A5A5": "Gray", "#FFC000": "Gold", "#5B9BD5": "Light Blue", "#70AD47": "Green",
                "#C00000": "Dark Red", "#FF0000": "Red", "#FFFF00": "Yellow", "#92D050": "Light Green", "#00B050": "Sea Green",
                "#00B0F0": "Sky Blue", "#0070C0": "Blue", "#002060": "Dark Blue", "#7030A0": "Purple"
            };
            return map[upper] || upper;
        }
    }

    // ── Eraser Popup ─────────────────────────────────────────────────────────
    Rectangle {
        id: eraserPopup
        parent: root
        x: colorPopup.popupXPosition()
        y: colorPopup.popupYPosition(height)
        width: 272
        height: 196
        z: 100
        radius: 12
        color: popupBg
        border.color: popupBorder
        border.width: 1
        clip: true

        property real popupOpacity: 0.0
        visible: popupOpacity > 0.0
        opacity: popupOpacity
        Behavior on popupOpacity { NumberAnimation { duration: 200; easing.type: Easing.OutQuad } }

        layer.enabled: true
        layer.effect: DropShadow {
            verticalOffset: 4
            radius: 16
            samples: 33
            color: darkBackground ? "#50000000" : "#30000000"
        }

        function open()  { popupOpacity = 1.0; }
        function close() { popupOpacity = 0.0; }
        property bool opened: popupOpacity > 0.0

        Column {
            anchors.fill: parent
            anchors.margins: 16
            spacing: 12

            Text {
                text: eraserSizeText
                color: popupFg
                font.family: root.fontFamily
                font.pixelSize: 12
            }

            SliderHandle {
                from: 8; to: 40; stepSize: 1
                value: canvas ? canvas.eraserWidth : 20
                enabled: canvas ? canvas.eraserMode === 0 : true
                onMoved: (v) => { if (canvas) canvas.eraserWidth = Math.round(v); }
            }

            Text {
                text: canvas ? Math.round(canvas.eraserWidth) : 20
                color: textSecondary
                font.family: root.fontFamily
                font.pixelSize: 11
            }

            Rectangle {
                width: parent.width
                height: 1
                color: toolbarLine
            }

            Text {
                text: slideClearText
                color: popupFg
                font.family: root.fontFamily
                font.pixelSize: 12
            }

            Rectangle {
                id: clearSliderContainer
                width: parent.width
                height: 44
                radius: 22
                color: controlBg

                Text {
                    anchors.centerIn: parent
                    text: slideClearHintText
                    color: textSecondary
                    font.family: root.fontFamily
                    font.pixelSize: 13
                    opacity: 1.0 - (clearThumb.x / (parent.width - clearThumb.width - 4))
                }

                Rectangle {
                    id: clearThumb
                    width: 40
                    height: 40
                    radius: 20
                    color: popupFg
                    y: 2
                    x: 2

                    Image {
                        id: clearThumbIcon
                        source: getIconPath("Clear")
                        width: 20
                        height: 20
                        anchors.centerIn: parent
                        sourceSize: Qt.size(20, 20)
                        visible: false
                    }
                    ColorOverlay {
                        anchors.fill: clearThumbIcon
                        source: clearThumbIcon
                        color: popupBg
                    }

                    MouseArea {
                        anchors.fill: parent
                        drag.target: parent
                        drag.axis: Drag.XAxis
                        drag.minimumX: 2
                        drag.maximumX: clearSliderContainer.width - clearThumb.width - 2
                        onReleased: {
                            if (clearThumb.x > (clearSliderContainer.width - clearThumb.width - 2) * 0.8) {
                                if (canvas) canvas.clear();
                                eraserPopup.close();
                            }
                            clearThumb.x = 2;
                        }
                    }
                }
            }
        }
    }

    // ── Floating Toolbar (overlay style) ─────────────────────────────────────
    Rectangle {
        id: toolbar
        property bool isVertical: toolbarPosition === "left" || toolbarPosition === "right"
        property real itemSize: 38
        property real spacing: 4
        property real pad: 8
        property int toolCount: 5
        width: isVertical ? itemSize + pad * 2
                          : itemSize * toolCount + spacing * (toolCount - 1) + pad * 2
        height: isVertical ? itemSize * toolCount + spacing * (toolCount - 1) + pad * 2
                           : itemSize + pad * 2
        radius: isVertical ? width / 2 : height / 2
        color: applyOpacityToColor(toolbarBg, toolbarOpacity)
        border.color: toolbarBorder
        border.width: 0.5
        z: 90

        layer.enabled: true
        layer.effect: DropShadow {
            verticalOffset: 2
            radius: 12
            samples: 25
            color: darkBackground ? "#40000000" : "#26000000"
        }

        anchors.bottom: board.bottom
        anchors.bottomMargin: 20
        anchors.horizontalCenter: board.horizontalCenter

        states: [
            State {
                name: "top"
                when: toolbarPosition === "top"
                AnchorChanges {
                    target: toolbar
                    anchors.top: board.top
                    anchors.bottom: undefined
                    anchors.horizontalCenter: board.horizontalCenter
                }
                PropertyChanges {
                    target: toolbar
                    anchors.topMargin: 20
                    anchors.bottomMargin: 0
                }
            },
            State {
                name: "left"
                when: toolbarPosition === "left"
                AnchorChanges {
                    target: toolbar
                    anchors.left: board.left
                    anchors.right: undefined
                    anchors.bottom: undefined
                    anchors.verticalCenter: board.verticalCenter
                }
                PropertyChanges {
                    target: toolbar
                    anchors.leftMargin: 20
                    anchors.bottomMargin: 0
                }
            },
            State {
                name: "right"
                when: toolbarPosition === "right"
                AnchorChanges {
                    target: toolbar
                    anchors.right: board.right
                    anchors.left: undefined
                    anchors.bottom: undefined
                    anchors.verticalCenter: board.verticalCenter
                }
                PropertyChanges {
                    target: toolbar
                    anchors.rightMargin: 20
                    anchors.bottomMargin: 0
                }
            }
        ]

        Grid {
            id: grid
            anchors.centerIn: parent
            spacing: toolbar.spacing
            columns: toolbar.isVertical ? 1 : 999

            // Pen
            ToolButton {
                iconSource: getIconPath("Pen")
                isActive: canvas ? !canvas.isEraser : true
                isVertical: toolbar.isVertical
                showText: showToolText
                label: penText
                onClicked: {
                    if (canvas && !canvas.isEraser) {
                        if (colorPopup.opened) colorPopup.close(); else colorPopup.open();
                    } else if (canvas) {
                        canvas.isEraser = false;
                    }
                    eraserPopup.close();
                }
            }

            // Eraser
            ToolButton {
                iconSource: getIconPath("Eraser")
                isActive: canvas ? canvas.isEraser : false
                isVertical: toolbar.isVertical
                showText: showToolText
                label: eraserText
                onClicked: {
                    if (canvas && canvas.isEraser) {
                        if (eraserPopup.opened) eraserPopup.close(); else eraserPopup.open();
                    } else if (canvas) {
                        canvas.isEraser = true;
                        colorPopup.close();
                    }
                }
            }

            // Clear
            ToolButton {
                iconSource: getIconPath("Clear")
                isVertical: toolbar.isVertical
                showText: showToolText
                label: clearText
                onClicked: canvas.clear()
            }

            // Undo
            ToolButton {
                iconSource: getIconPath("undo")
                enabled: canvas && canvas.canUndo
                isVertical: toolbar.isVertical
                showText: showToolText
                label: undoText
                onClicked: {
                    colorPopup.close(); eraserPopup.close();
                    if (canvas) canvas.undo();
                }
            }

            // Redo
            ToolButton {
                iconSource: getIconPath("redo")
                enabled: canvas && canvas.canRedo
                isVertical: toolbar.isVertical
                showText: showToolText
                label: redoText
                onClicked: {
                    colorPopup.close(); eraserPopup.close();
                    if (canvas) canvas.redo();
                }
            }
        }
    }

    // Fullscreen button
    FloatingButton {
        id: fullscreenToggle
        anchors.left: board.left
        anchors.verticalCenter: toolbar.verticalCenter
        anchors.leftMargin: 20
        iconSource: getIconPath((backend && backend.isFullscreen) ? "exitfullscr" : "fullscr")
        z: 91
        onClicked: if (backend) backend.toggleFullscreen()
    }

    // Save PNG button
    FloatingButton {
        id: savePageButton
        anchors.right: board.right
        anchors.verticalCenter: toolbar.verticalCenter
        anchors.rightMargin: 20
        z: 91
        onClicked: if (backend) backend.saveCurrentPageAsPng()
        Text {
            anchors.centerIn: parent
            text: "PNG"
            color: toolbarFg
            font.family: root.fontFamily
            font.pixelSize: 12
            font.bold: true
        }
    }

    // Position floating buttons away from the toolbar when it is vertical
    states: [
        State {
            name: "toolbarLeft"
            when: toolbarPosition === "left"
            AnchorChanges {
                target: fullscreenToggle
                anchors.left: undefined
                anchors.right: board.right
            }
            AnchorChanges {
                target: savePageButton
                anchors.right: board.right
                anchors.left: undefined
            }
            PropertyChanges {
                target: fullscreenToggle
                anchors.rightMargin: 20
                anchors.verticalCenterOffset: -40
            }
            PropertyChanges {
                target: savePageButton
                anchors.rightMargin: 20
                anchors.verticalCenterOffset: 40
            }
        },
        State {
            name: "toolbarRight"
            when: toolbarPosition === "right"
            AnchorChanges {
                target: fullscreenToggle
                anchors.left: board.left
                anchors.right: undefined
            }
            AnchorChanges {
                target: savePageButton
                anchors.left: board.left
                anchors.right: undefined
            }
            PropertyChanges {
                target: fullscreenToggle
                anchors.leftMargin: 20
                anchors.verticalCenterOffset: -40
            }
            PropertyChanges {
                target: savePageButton
                anchors.leftMargin: 20
                anchors.verticalCenterOffset: 40
            }
        }
    ]

    // ── Reusable components ──────────────────────────────────────────────────
    component SliderHandle: Item {
        id: sliderRoot
        property real from: 0
        property real to: 100
        property real stepSize: 1
        property real value: 50
        property real handleSize: 16
        property color trackColor: controlBg
        property color fillColor: textSecondary
        property color handleColor: popupFg
        property bool enabled: true
        signal moved(real newValue)

        implicitWidth: 140
        implicitHeight: 20

        opacity: enabled ? 1.0 : 0.4

        Rectangle {
            id: track
            anchors.verticalCenter: parent.verticalCenter
            width: parent.width; height: 4; radius: 2
            color: sliderRoot.trackColor
            Rectangle {
                width: Math.max(0, (sliderRoot.value - sliderRoot.from) /
                            Math.max(0.001, sliderRoot.to - sliderRoot.from) * parent.width)
                height: parent.height; radius: parent.radius
                color: sliderRoot.fillColor
            }
        }
        Rectangle {
            id: hHandle
            width: sliderRoot.handleSize; height: sliderRoot.handleSize
            radius: sliderRoot.handleSize / 2
            color: sliderRoot.handleColor
            anchors.verticalCenter: parent.verticalCenter
            x: Math.max(0, Math.min(sliderRoot.width - width,
                (sliderRoot.value - sliderRoot.from) /
                Math.max(0.001, sliderRoot.to - sliderRoot.from) * (sliderRoot.width - width)))
        }
        MouseArea {
            anchors.fill: parent
            enabled: sliderRoot.enabled
            function _calc(mx) {
                var ratio = Math.max(0, Math.min(1, mx / Math.max(1, sliderRoot.width - sliderRoot.handleSize)));
                var raw = sliderRoot.from + ratio * (sliderRoot.to - sliderRoot.from);
                return Math.max(sliderRoot.from, Math.min(sliderRoot.to, sliderRoot.stepSize > 0 ? Math.round(raw / sliderRoot.stepSize) * sliderRoot.stepSize : raw));
            }
            onPressed:  (m) => { sliderRoot.value = _calc(m.x - sliderRoot.handleSize/2); sliderRoot.moved(sliderRoot.value); }
            onPositionChanged: (m) => { sliderRoot.value = _calc(m.x - sliderRoot.handleSize/2); sliderRoot.moved(sliderRoot.value); }
        }
    }

    component ToolButton: Rectangle {
        id: toolBtn
        property alias iconSource: iconImg.source
        property bool isActive: false
        property bool isVertical: false
        property bool showText: false
        property string label: ""
        property bool enabled: true
        signal clicked()

        width: isVertical ? (showText ? Math.max(36, labelText.contentWidth) : 36) : (showText ? Math.max(36, labelText.contentWidth) : 36)
        height: isVertical ? (showText ? 56 : 36) : (showText ? 56 : 36)
        color: !enabled ? "transparent" : (isActive ? buttonActive : (toolBtnMouse.pressed ? buttonActive : (toolBtnMouse.containsMouse ? buttonHover : "transparent")))
        opacity: enabled ? 1.0 : 0.5
        scale: toolBtnMouse.pressed && enabled ? 0.95 : 1.0
        radius: width / 2

        Behavior on color { ColorAnimation { duration: 150 } }
        Behavior on opacity { NumberAnimation { duration: 150 } }
        Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutQuad } }

        Column {
            anchors.centerIn: parent
            spacing: 4
            Image {
                id: iconImg
                width: 20
                height: 20
                sourceSize: Qt.size(20, 20)
                visible: false
            }
            ColorOverlay {
                id: iconOverlay
                anchors.horizontalCenter: parent.horizontalCenter
                width: 20
                height: 20
                source: iconImg
                color: toolBtn.enabled ? toolbarFg : textSecondary
                visible: true
            }
            Text {
                id: labelText
                text: toolBtn.label
                color: toolbarFg
                font.family: root.fontFamily
                font.pixelSize: 11
                anchors.horizontalCenter: parent.horizontalCenter
                visible: toolBtn.showText
            }
        }

        MouseArea {
            id: toolBtnMouse
            anchors.fill: parent
            enabled: toolBtn.enabled
            hoverEnabled: true
            cursorShape: enabled ? Qt.PointingHandCursor : Qt.ArrowCursor
            onClicked: toolBtn.clicked()
        }
    }

    component FloatingButton: Rectangle {
        id: floatBtn
        property alias iconSource: iconImg2.source
        property bool hovered: false
        property bool pressed: false
        signal clicked()
        width: 50
        height: 50
        radius: 25
        color: pressed ? buttonActive : (hovered ? buttonHover : applyOpacityToColor(toolbarBg, toolbarOpacity))
        border.color: toolbarBorder
        border.width: 0.5
        scale: pressed ? 0.95 : 1.0

        Behavior on color { ColorAnimation { duration: 150 } }
        Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutQuad } }

        Image {
            id: iconImg2
            width: 20
            height: 20
            sourceSize: Qt.size(20, 20)
            anchors.centerIn: parent
            visible: source.toString() !== ""
        }
        ColorOverlay {
            anchors.fill: iconImg2
            source: iconImg2
            color: toolbarFg
            visible: iconImg2.source.toString() !== ""
        }

        MouseArea {
            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onEntered: floatBtn.hovered = true
            onExited:  floatBtn.hovered = false
            onPressed: floatBtn.pressed = true
            onReleased: floatBtn.pressed = false
            onClicked: floatBtn.clicked()
        }
    }
}
