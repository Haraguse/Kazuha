import QtQuick 2.15
import QtQml 2.15
import QtQuick.Window 2.15
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
    property var boardPages: []
    property int currentBoardPage: 1
    // titleBarHeight is injected from Python; fallback to 32
    property int tbH: (typeof titleBarHeight !== "undefined") ? titleBarHeight : 32
    Keys.onEscapePressed: backend.closeWindow()

    // ── Self-drawn title bar ──────────────────────────────────────────────────
    Rectangle {
        id: titleBar
        visible: false  // 使用系统Windows窗口边框时隐藏自绘标题栏
        height: root.tbH
        anchors.top:   parent.top
        anchors.left:  parent.left
        anchors.right: parent.right
        // Match board background colour exactly for a seamless look
        color: darkBackground ? Qt.rgba(0.098, 0.098, 0.098, 1.0)
                              : Qt.rgba(0.937, 0.937, 0.937, 1.0)
        z: 200

        // ---- Bottom separator ----
        Rectangle {
            anchors.bottom: parent.bottom
            anchors.left:   parent.left
            anchors.right:  parent.right
            height: 1
            color: darkBackground ? Qt.rgba(1, 1, 1, 0.08) : Qt.rgba(0, 0, 0, 0.08)
        }

        // ---- Window title ----
        Text {
            id: titleText
            // wndTitle injected from Python; fallback
            text: (typeof windowTitle !== "undefined") ? windowTitle : "\u5c0f\u9ed1\u677f"
            color: darkBackground ? "#E5E5E5" : "#1A1A1A"
            font.pixelSize: 12
            elide: Text.ElideRight
            verticalAlignment: Text.AlignVCenter
            anchors.left:         parent.left
            anchors.leftMargin:   12
            anchors.verticalCenter: parent.verticalCenter
            anchors.right:        captionButtons.left
            anchors.rightMargin:  8
        }

        // ---- Caption buttons row ----
        Row {
            id: captionButtons
            anchors.right:  parent.right
            anchors.top:    parent.top
            anchors.bottom: parent.bottom

            // ─ Minimize ─
            Item {
                id: minBtn
                width: 46
                height: parent.height
                Rectangle {
                    anchors.fill: parent
                    color: minBtnArea.containsMouse
                           ? (darkBackground ? Qt.rgba(1,1,1,0.12) : Qt.rgba(0,0,0,0.08))
                           : "transparent"
                    Behavior on color { ColorAnimation { duration: 80 } }
                }
                Text {
                    text: "\uE921"   // Segoe MDL2: ChromeMinimize
                    font.family: "Segoe MDL2 Assets"
                    font.pixelSize: 10
                    color: darkBackground ? "#E5E5E5" : "#1A1A1A"
                    anchors.centerIn: parent
                }
                MouseArea {
                    id: minBtnArea
                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.ArrowCursor
                    onClicked: if (backend) backend.minimizeWindow()
                }
            }

            // ─ Maximize / Restore ─
            Item {
                id: maxBtn
                width: 46
                height: parent.height
                // hover state driven by native filter (HTMAXBUTTON / WM_NCMOUSEMOVE)
                property bool nativeHovered: backend ? backend.maximizeBtnHovered : false
                Rectangle {
                    anchors.fill: parent
                    color: maxBtn.nativeHovered
                           ? (darkBackground ? Qt.rgba(1,1,1,0.12) : Qt.rgba(0,0,0,0.08))
                           : "transparent"
                    Behavior on color { ColorAnimation { duration: 80 } }
                }
                Text {
                    // ChromeMaximize \uE922 / ChromeRestore \uE923
                    text: (backend && backend.isMaximized) ? "\uE923" : "\uE922"
                    font.family: "Segoe MDL2 Assets"
                    font.pixelSize: 10
                    color: darkBackground ? "#E5E5E5" : "#1A1A1A"
                    anchors.centerIn: parent
                }
                // No click handler: HTMAXBUTTON lets Windows handle maximize natively
                // (also triggers Win11 Snap Layouts popup on hover)
            }

            // ─ Close ─
            Item {
                id: closeBtn
                width: 46
                height: parent.height
                Rectangle {
                    id: closeBtnBg
                    anchors.fill: parent
                    color: closeBtnArea.containsMouse ? "#C42B1C" : "transparent"
                    Behavior on color { ColorAnimation { duration: 80 } }
                }
                Text {
                    text: "\uE8BB"   // Segoe MDL2: ChromeClose
                    font.family: "Segoe MDL2 Assets"
                    font.pixelSize: 10
                    color: closeBtnArea.containsMouse ? "#FFFFFF"
                                                     : (darkBackground ? "#E5E5E5" : "#1A1A1A")
                    anchors.centerIn: parent
                    Behavior on color { ColorAnimation { duration: 80 } }
                }
                MouseArea {
                    id: closeBtnArea
                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.ArrowCursor
                    onClicked: if (backend) backend.closeWindow()
                }
            }
        }
    } // end titleBar

    function isDarkColor(value) {
        if (!value || value.length < 6) return true;
        var hex = value.charAt(0) === "#" ? value.slice(1) : value;
        if (hex.length !== 6) return true;
        var r = parseInt(hex.slice(0, 2), 16);
        var g = parseInt(hex.slice(2, 4), 16);
        var b = parseInt(hex.slice(4, 6), 16);
        if (isNaN(r) || isNaN(g) || isNaN(b)) return true;
        var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
        return luminance < 128;
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
        return {
            strokes: strokes,
            thumb: ""
        };
    }

    function ensureBoardPages() {
        if (!isArrayLike(boardPages) || boardPages.length === 0) {
            boardPages = [createEmptyBoardPage()];
        } else if (boardPages.length > 1) {
            boardPages = [normalizeBoardPage(boardPages[0])];
        }
        currentBoardPage = 1;
    }

    function persistCurrentPageToModel() {
        ensureBoardPages();
        var page = normalizeBoardPage(boardPages[0] || createEmptyBoardPage());
        page.strokes = cloneStrokeList(canvas.getStrokes());
        boardPages = [page];
        return page;
    }

    function applyCurrentBoardPage() {
        ensureBoardPages();
        var page = normalizeBoardPage(boardPages[0] || createEmptyBoardPage());
        canvas.setStrokes(page.strokes);
    }

    function getBoardDocument() {
        var page = persistCurrentPageToModel();
        return {
            currentPage: 1,
            pages: [{ strokes: cloneStrokeList(page.strokes), thumb: "" }]
        };
    }

    function setBoardDocument(documentData) {
        var strokes = [];
        if (isArrayLike(documentData)) {
            strokes = cloneStrokeList(documentData);
        } else if (documentData && isArrayLike(documentData.pages)) {
            strokes = cloneStrokeList((documentData.pages[0] || {}).strokes || []);
        }
        boardPages = [{ strokes: strokes, thumb: "" }];
        currentBoardPage = 1;
        applyCurrentBoardPage();
    }

    Component.onCompleted: {
        ensureBoardPages();
        applyCurrentBoardPage();
    }

    // ── Main drawing area (below title bar) ─────────────────────────────────
    // Main Content Area
    Rectangle {
        id: board
        anchors.top:    titleBar.visible ? titleBar.bottom : parent.top
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
            property int eraserMode: 1
            property int currentStrokeId: 0
            property bool penStrokeEnabled: root.penStrokeEnabled
            property real lastWidth: lineWidth
            property real minSegmentPixels: 0.6 * root.performanceScale
            property real minSegmentPixelsSquared: minSegmentPixels * minSegmentPixels
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
            property var allLines: [] // Store all strokes history
            property bool needsFullRepaint: false
            property bool paintScheduled: false
            // 极致优化：完全禁用平滑处理，直接绘制以获得最低延迟
            property real smoothingFactorPen: 0.0
            property real smoothingFactorEraser: 0.0
            property real lowSamplePixels: 2.0 * root.performanceScale
            property real lowSamplePixelsSquared: lowSamplePixels * lowSamplePixels
            // 极致优化：大幅增加最大分段像素，最小化插值计算
            property real maxSegmentPixels: 16.0 * root.performanceScale
            readonly property bool canUndo: undoStack.length > 0
            readonly property bool canRedo: redoStack.length > 0

            onWidthChanged: {
                requestRepaintAll();
            }
            onHeightChanged: {
                requestRepaintAll();
            }
            
            function drawLine(ctx, line, w, h) {
                // Compatibility for scripts that call drawLine on canvas.
                // Now we just add it to the native item's queue.
                addLine(line.x1, line.y1, line.x2, line.y2, line.width, 
                        line.color.toString(), !!line.isEraser, line.width || 20, line.strokeId);
            }

            function cloneLines(lines) {
                var result = [];
                for (var i = 0; i < lines.length; i++) {
                    var line = lines[i];
                    result.push({
                        x1: line.x1,
                        y1: line.y1,
                        x2: line.x2,
                        y2: line.y2,
                        color: line.color,
                        width: line.width,
                        isEraser: line.isEraser,
                        strokeId: line.strokeId
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
                    x1: canvas.lastX,
                    y1: canvas.lastY,
                    x2: currentX,
                    y2: currentY,
                    color: canvas.drawColor.toString(),
                    width: calcDynamicWidth(currentX, currentY),
                    isEraser: canvas.isEraser,
                    strokeId: canvas.currentStrokeId
                };
                canvas.enqueueLine(line);
                canvas.lastX = currentX;
                canvas.lastY = currentY;
            }

            function appendEraserSegment(currentX, currentY) {
                var line = {
                    x1: canvas.lastX,
                    y1: canvas.lastY,
                    x2: currentX,
                    y2: currentY,
                    color: "#000000",
                    width: canvas.eraserWidth,
                    isEraser: true,
                    eraserPx: canvas.eraserWidth,
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
                // 极致优化：使用固定小阈值，几乎不过滤点
                if (!forceFinal && rawDistSquared < 0.09) {
                    return;
                }
                if (canvas.isEraser) {
                    if (rawDistSquared < canvas.lowSamplePixelsSquared && !forceFinal) {
                        return;
                    }
                    canvas.appendEraserSegment(currentX, currentY);
                    return;
                }
                // 极致优化：直接绘制，完全跳过所有平滑处理
                canvas.lastFilteredX = currentX;
                canvas.lastFilteredY = currentY;
                appendDirectly(currentX, currentY, forceFinal);
            }

            // 极致优化：最简单的直接绘制，最小化计算
            function appendDirectly(targetX, targetY, forceFinal) {
                var w = canvas.width || 1;
                var h = canvas.height || 1;
                var startX = canvas.lastX;
                var startY = canvas.lastY;
                var dxPx = (targetX - startX) * w;
                var dyPx = (targetY - startY) * h;
                // 极致优化：使用平方距离，避免开方
                var movePxSquared = dxPx * dxPx + dyPx * dyPx;
                // 极致优化：极宽松的阈值，几乎不过滤
                if (!forceFinal && movePxSquared < 0.5) {
                    return;
                }
                // 极致优化：只有当距离很大时才插入一个中间点
                var maxSegSq = canvas.maxSegmentPixels * canvas.maxSegmentPixels;
                if (movePxSquared > maxSegSq && !forceFinal) {
                    var midX = (startX + targetX) * 0.5;
                    var midY = (startY + targetY) * 0.5;
                    appendSegmentTo(midX, midY);
                }
                appendSegmentTo(targetX, targetY);
            }

            function appendToTarget(targetX, targetY, forceFinal) {
                var w = Math.max(1, canvas.width);
                var h = Math.max(1, canvas.height);
                var startX = canvas.lastX;
                var startY = canvas.lastY;
                var dxPx = (targetX - startX) * w;
                var dyPx = (targetY - startY) * h;
                var movePxSquared = dxPx * dxPx + dyPx * dyPx;
                if (!forceFinal && movePxSquared < canvas.lowSamplePixelsSquared) {
                    return;
                }
                var steps = Math.ceil(Math.sqrt(movePxSquared) / canvas.maxSegmentPixels);
                if (steps < 1) steps = 1;
                // 优化：限制最大分段数为2，减少计算量
                if (steps > 2) steps = 2;
                for (var i = 1; i <= steps; i++) {
                    var t = i / steps;
                    var x = startX + (targetX - startX) * t;
                    var y = startY + (targetY - startY) * t;
                    var segDxPx = (x - canvas.lastX) * w;
                    var segDyPx = (y - canvas.lastY) * h;
                    // 优化：提高最小分段阈值，减少小分段绘制
                    if (!forceFinal && (segDxPx * segDxPx + segDyPx * segDyPx) < 1.0) {
                        continue;
                    }
                    appendSegmentTo(x, y);
                }
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
                    pushHistoryAction({
                        type: "add",
                        strokeId: currentStrokeId,
                        lines: cloneLines(gestureAddedLines)
                    });
                } else if (gestureRemovedStrokes.length > 0) {
                    pushHistoryAction({
                        type: "remove",
                        strokes: cloneRemovedStrokes(gestureRemovedStrokes)
                    });
                }
                gestureAddedLines = [];
                gestureRemovedStrokes = [];
                gestureRemovedStrokeIds = ({});
            }

            function undo() {
                if (!canUndo) return;
                var action = undoStack[undoStack.length - 1];
                var nextUndo = undoStack.slice(0, undoStack.length - 1);
                undoStack = nextUndo;
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
                var nextRedo = redoStack.slice(0, redoStack.length - 1);
                redoStack = nextRedo;
                if (action.type === "add") {
                    allLines = allLines.concat(cloneLines(action.lines));
                    canvas.setAllLines(allLines);
                } else if (action.type === "remove") {
                    for (var i = 0; i < action.strokes.length; i++) {
                        removeStroke(action.strokes[i].strokeId);
                    }
                } else if (action.type === "clear") {
                    applyLines([]);
                }
                undoStack = undoStack.concat([action]);
            }

            function hitTest(x, y) {
                // x, y are normalized (0-1)
                // Threshold: let's say 10 pixels converted to normalized
                var w = width;
                var h = height;
                if (w <= 0 || h <= 0) return -1;
                
                var t = 10 / w; // Approximation using width
                var tSquared = t * t;
                
                for (var i = 0; i < allLines.length; i++) {
                    var line = allLines[i];
                    if (line.isEraser) continue;
                    
                    // Simple bounding box check first
                    var minX = Math.min(line.x1, line.x2) - t;
                    var maxX = Math.max(line.x1, line.x2) + t;
                    var minY = Math.min(line.y1, line.y2) - t;
                    var maxY = Math.max(line.y1, line.y2) + t;
                    
                    if (x >= minX && x <= maxX && y >= minY && y <= maxY) {
                        var distSquared = distToSegmentSquared(x, y, line.x1, line.y1, line.x2, line.y2);
                        if (distSquared < tSquared) {
                            return line.strokeId;
                        }
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
                if (len_sq !== 0) // in case of 0 length line
                    param = dot / len_sq;
                
                var xx, yy;
                
                if (param < 0) {
                    xx = x1;
                    yy = y1;
                }
                else if (param > 1) {
                    xx = x2;
                    yy = y2;
                }
                else {
                    xx = x1 + param * C;
                    yy = y1 + param * D;
                }
                
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
                    return {
                        strokeId: strokeId,
                        startIndex: startIndex,
                        lines: cloneLines(removedLines)
                    };
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
                // 极致优化：禁用鼠标跟踪以减少事件开销
                hoverEnabled: false
                onPressed: (mouse) => {
                    canvas.beginGesture()
                    var x = mouse.x / canvas.width
                    var y = mouse.y / canvas.height
                    canvas.lastX = x
                    canvas.lastY = y
                    canvas.lastRawX = x
                    canvas.lastRawY = y
                    canvas.lastFilteredX = x
                    canvas.lastFilteredY = y
                    canvas.pointerActive = true
                    canvas.currentStrokeId++;
                    canvas.lastWidth = canvas.lineWidth;
                }
                onReleased: (mouse) => {
                    // 极致优化：直接处理最后一个点
                    var x = mouse.x / canvas.width
                    var y = mouse.y / canvas.height
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
                    // 极致优化：直接处理移动事件，无延迟
                    var currentX = mouse.x / canvas.width;
                    var currentY = mouse.y / canvas.height;
                    canvas.processInputPoint(currentX, currentY, false);
                }
            }
            
            function clear() {
                if (canvas.allLines.length === 0) return;
                pushHistoryAction({
                    type: "clear",
                    lines: cloneLines(canvas.allLines)
                });
                applyLines([]);
            }

            function getStrokes() {
                // Return simple object list, QML will convert to QJSValue/QVariant
                var result = [];
                for (var i = 0; i < canvas.allLines.length; i++) {
                    result.push(canvas.allLines[i]);
                }
                return result;
            }

            function setStrokes(strokes) {
                // Convert legacy absolute coordinates to relative if needed
                var newStrokes = [];
                var w = width || 800; // Fallback to avoid div by zero if 0
                var h = height || 600;
                
                if (w === 0) w = 800;
                if (h === 0) h = 600;

                for (var i = 0; i < strokes.length; i++) {
                    var s = strokes[i];
                    // Deep copy to avoid modifying original reference if passed by ref
                    var line = {
                        x1: s.x1,
                        y1: s.y1,
                        x2: s.x2,
                        y2: s.y2,
                        color: s.color,
                        width: s.width,
                        isEraser: s.isEraser,
                        strokeId: s.strokeId
                    };

                    // Heuristic: if values are > 1.1, assume absolute pixels
                    if (line.x1 > 1.1 || line.y1 > 1.1 || line.x2 > 1.1 || line.y2 > 1.1) {
                        line.x1 /= w;
                        line.y1 /= h;
                        line.x2 /= w;
                        line.y2 /= h;
                    }
                    
                    // Assign strokeId if missing (for legacy strokes)
                    if (line.strokeId === undefined) {
                         // Generate a unique ID for this batch load if we can, or just random
                         // But we want grouped strokes.
                         // For now, let's just use a counter that increments per line to avoid deleting everything at once
                         // Or better: try to guess strokes by continuity? Too complex.
                         // Just give each line a unique ID so stroke eraser acts like point eraser on old strokes (deletes segment only)
                         line.strokeId = -1000 - i; 
                    }
                    
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
                    if (newStrokes[j].strokeId > maxStrokeId) {
                        maxStrokeId = newStrokes[j].strokeId;
                    }
                }
                canvas.currentStrokeId = maxStrokeId;
                canvas.setAllLines(newStrokes);
            }
        }
    }
    
    // Watermark
    Text {
        anchors.right:        board.right
        anchors.bottom:       board.bottom
        anchors.rightMargin:  15
        anchors.bottomMargin: 10
        text: typeof watermarkText !== "undefined" ? watermarkText : ""
        color: darkBackground ? Qt.rgba(1, 1, 1, 0.3) : Qt.rgba(0, 0, 0, 0.3)
        font.pixelSize: 12
        horizontalAlignment: Text.AlignRight
        visible: typeof showWatermark !== "undefined" ? showWatermark : false
        z: 100
    }

    // ── Shared outside-click overlay (closes any open popup) ─────────────────
    MouseArea {
        id: popupOverlay
        anchors.fill: board
        z: 98
        enabled: colorPopup.visible || eraserPopup.visible
        onClicked: { colorPopup.close(); eraserPopup.close(); }
    }

    Rectangle {
        id: colorPopup
        parent: root
        x: toolbarPosition === "left"
            ? toolbar.x + toolbar.width + 12
            : toolbarPosition === "right"
                ? toolbar.x - width - 12
                : (root.width - width) / 2
        y: toolbarPosition === "top"
            ? toolbar.y + toolbar.height + 12
            : toolbarPosition === "bottom"
                ? toolbar.y - height - 12
                : toolbar.y + (toolbar.height - height) / 2
        width: 312
        visible: false
        opacity: 0
        scale: 0.92
        z: 100
        radius: 12
        color: root.popupBackgroundColor !== "" ? root.popupBackgroundColor : (darkBackground ? "#202020" : "#FFFFFF")
        border.color: root.popupBorderColor !== "" ? root.popupBorderColor : (darkBackground ? Qt.rgba(1, 1, 1, 0.1) : Qt.rgba(0, 0, 0, 0.1))
        border.width: 1
        clip: true

        Behavior on opacity { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }
        Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }

        property color textPrimary: darkBackground ? "#E5E5E5" : "#191919"
        property color textSecondary: darkBackground ? Qt.rgba(0.8, 0.8, 0.8, 0.6) : Qt.rgba(0.1, 0.1, 0.1, 0.6)
        property color popupBorder: darkBackground ? Qt.rgba(1, 1, 1, 0.1) : Qt.rgba(0, 0, 0, 0.1)
        property string hoveredColorName: ""
        property string hoveredColorRgb: ""
        property string hoveredColorHex: ""

        function open() {
            closeTimer.stop();
            visible = true;
            openTimer.start();
        }
        function close() {
            openTimer.stop();
            opacity = 0;
            scale = 0.92;
            closeTimer.start();
        }

        Timer { id: openTimer; interval: 10; onTriggered: { if (colorPopup.visible) { colorPopup.opacity = 1; colorPopup.scale = 1.0; } } }
        Timer { id: closeTimer; interval: 200; onTriggered: { colorPopup.visible = false; } }

        Column {
            id: popupContent
            anchors.fill: parent
            anchors.margins: 16
            spacing: 12

            Column {
                id: colorGridContainer
                width: parent.width
                spacing: 8

                Row {
                    id: colorRow1
                    width: parent.width
                    spacing: 8

                    Repeater {
                        model: penColorsRow1
                        delegate: Item {
                            width: (colorRow1.width - (9 - 1) * colorRow1.spacing) / 9
                            height: 28

                            property bool isActive: (canvas && !canvas.isEraser && Qt.colorEqual(canvas.drawColor, modelData)) || false

                            Rectangle {
                                id: colorSwatch
                                width: 24
                                height: 24
                                radius: 4
                                color: modelData
                                anchors.centerIn: parent
                                border.width: 2
                                border.color: parent.isActive ? colorPopup.textPrimary : "transparent"

                                Rectangle {
                                    anchors.centerIn: parent
                                    width: 28
                                    height: 28
                                    radius: 6
                                    color: "transparent"
                                    border.width: 2
                                    border.color: parent.isActive ? Qt.rgba(0, 0, 0, 0.3) : "transparent"
                                    visible: parent.isActive
                                }

                                Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }
                                Behavior on border.color { ColorAnimation { duration: 150 } }

                                MouseArea {
                                    anchors.fill: parent
                                    cursorShape: Qt.PointingHandCursor
                                    hoverEnabled: true
                                    onEntered: {
                                        colorSwatch.scale = 1.15;
                                        colorSwatch.z = 1;
                                        colorPopup.updateInfo(modelData);
                                    }
                                    onExited: {
                                        colorSwatch.scale = 1.0;
                                        colorSwatch.z = 0;
                                        colorPopup.clearInfo();
                                    }
                                    onClicked: {
                                        if (canvas) {
                                            canvas.drawColor = modelData;
                                            canvas.isEraser = false;
                                        }
                                        colorPopup.close();
                                    }
                                }
                            }
                        }
                    }
                }

                Row {
                    id: colorRow2
                    width: parent.width
                    spacing: 8

                    Repeater {
                        model: penColorsRow2
                        delegate: Item {
                            width: (colorRow2.width - (9 - 1) * colorRow2.spacing) / 9
                            height: 28

                            property bool isActive: (canvas && !canvas.isEraser && Qt.colorEqual(canvas.drawColor, modelData)) || false

                            Rectangle {
                                id: colorSwatch
                                width: 24
                                height: 24
                                radius: 4
                                color: modelData
                                anchors.centerIn: parent
                                border.width: 2
                                border.color: parent.isActive ? colorPopup.textPrimary : "transparent"

                                Rectangle {
                                    anchors.centerIn: parent
                                    width: 28
                                    height: 28
                                    radius: 6
                                    color: "transparent"
                                    border.width: 2
                                    border.color: parent.isActive ? Qt.rgba(0, 0, 0, 0.3) : "transparent"
                                    visible: parent.isActive
                                }

                                Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }
                                Behavior on border.color { ColorAnimation { duration: 150 } }

                                MouseArea {
                                    anchors.fill: parent
                                    cursorShape: Qt.PointingHandCursor
                                    hoverEnabled: true
                                    onEntered: {
                                        colorSwatch.scale = 1.15;
                                        colorSwatch.z = 1;
                                        colorPopup.updateInfo(modelData);
                                    }
                                    onExited: {
                                        colorSwatch.scale = 1.0;
                                        colorSwatch.z = 0;
                                        colorPopup.clearInfo();
                                    }
                                    onClicked: {
                                        if (canvas) {
                                            canvas.drawColor = modelData;
                                            canvas.isEraser = false;
                                        }
                                        colorPopup.close();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Column {
                id: colorInfoBox
                width: parent.width
                spacing: 2

                Rectangle {
                    width: parent.width
                    height: 1
                    color: colorPopup.popupBorder
                }

                Text {
                    id: infoName
                    text: colorPopup.hoveredColorName !== "" ? colorPopup.hoveredColorName : (canvas ? colorPopup.getColorName(colorPopup.toHex(canvas.drawColor)) : "")
                    color: colorPopup.textSecondary
                    font.pixelSize: 11
                    topPadding: 8
                }

                Text {
                    id: infoRgb
                    text: colorPopup.hoveredColorRgb !== "" ? colorPopup.hoveredColorRgb : colorPopup.getCurrentRgb()
                    color: colorPopup.textSecondary
                    font.pixelSize: 11
                }

                Text {
                    id: infoHex
                    text: colorPopup.hoveredColorHex !== "" ? colorPopup.hoveredColorHex : (canvas ? colorPopup.toHex(canvas.drawColor) : "")
                    color: colorPopup.textSecondary
                    font.pixelSize: 11
                }
            }
        }

        property string sizeDisplayText: canvas ? Math.round(canvas.lineWidth) : "3"

        height: popupContent.implicitHeight + 32

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
        function getCurrentRgb() {
            if (!canvas) return "";
            var c = canvas.drawColor;
            return "RGB(" + Math.round(c.r * 255) + ", " + Math.round(c.g * 255) + ", " + Math.round(c.b * 255) + ")";
        }
        function toHex(c) {
            if (!c) return "";
            var r = Math.round(c.r * 255).toString(16);
            var g = Math.round(c.g * 255).toString(16);
            var b = Math.round(c.b * 255).toString(16);
            if (r.length < 2) r = "0" + r;
            if (g.length < 2) g = "0" + g;
            if (b.length < 2) b = "0" + b;
            return ("#" + r + g + b).toUpperCase();
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
                "#FFFFFF": "白色", "#000000": "黑色", "#E7E6E6": "浅灰色", "#44546A": "蓝灰色",
                "#4472C4": "蓝色", "#ED7D31": "橙色", "#FF0000": "红色", "#A5A5A5": "灰色",
                "#FFC000": "金色", "#5B9BD5": "浅蓝色", "#70AD47": "绿色", "#C00000": "深红色",
                "#FFFF00": "黄色", "#92D050": "浅绿色", "#FF69B4": "骚粉色", "#00BCD4": "亮青色",
                "#002060": "深蓝色", "#7030A0": "紫色"
            };
            return map[upper] || upper;
        }
    }

    Rectangle {
        id: sizePopup
        parent: root
        x: {
            if (toolbarPosition === "left") return colorPopup.x + colorPopup.width + 8;
            if (toolbarPosition === "right") return colorPopup.x - width - 8;
            return colorPopup.x + colorPopup.width + 8;
        }
        y: colorPopup.y
        width: 36
        height: colorPopup.height
        visible: colorPopup.visible
        opacity: colorPopup.opacity
        scale: colorPopup.scale
        z: colorPopup.z
        radius: 12
        color: root.popupBackgroundColor !== "" ? root.popupBackgroundColor : (darkBackground ? "#202020" : "#FFFFFF")
        border.color: root.popupBorderColor !== "" ? root.popupBorderColor : (darkBackground ? Qt.rgba(1, 1, 1, 0.1) : Qt.rgba(0, 0, 0, 0.1))
        border.width: 1
        clip: true

        Behavior on opacity { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }
        Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }

        Item {
            id: sizeSliderContent
            anchors.fill: parent
            anchors.margins: 8

            Item {
                id: vertSlider
                width: parent.width
                height: parent.height
                property real from: 1
                property real to: 18
                property real stepSize: 1
                property real value: canvas ? canvas.lineWidth : 3

                function _calc(my) {
                    var usable = Math.max(1, height - vsHandle.height);
                    var ratio = 1 - Math.max(0, Math.min(1, my / usable));
                    var raw = from + ratio * (to - from);
                    return Math.max(from, Math.min(to,
                        stepSize > 0 ? Math.round(raw / stepSize) * stepSize : raw));
                }

                Rectangle {
                    id: vsTrack
                    anchors.horizontalCenter: parent.horizontalCenter
                    width: 4; height: parent.height; radius: 2
                    y: 0
                    color: darkBackground ? Qt.rgba(1,1,1,0.18) : Qt.rgba(0,0,0,0.12)
                }

                Rectangle {
                    id: vsTrackFill
                    anchors.horizontalCenter: parent.horizontalCenter
                    width: 4; radius: 2
                    y: vsHandle.y + vsHandle.height / 2
                    height: vertSlider.height - y
                    color: darkBackground ? "#C0C0C0" : "#555555"
                }

                Rectangle {
                    id: vsHandle
                    width: 20; height: 20; radius: 10
                    color: darkBackground ? "#FFFFFF" : "#333333"
                    anchors.horizontalCenter: parent.horizontalCenter
                    y: Math.max(0, Math.min(vertSlider.height - height,
                        (1 - (vertSlider.value - vertSlider.from) /
                         Math.max(0.001, vertSlider.to - vertSlider.from))
                        * (vertSlider.height - height)))
                }

                MouseArea {
                    anchors.fill: parent
                    hoverEnabled: true
                    onEntered: colorPopup.closeTimer.stop()
                    onExited: colorPopup.closeTimer.start()
                    onPressed: (m) => {
                        var v = vertSlider._calc(m.y - vsHandle.height/2);
                        vertSlider.value = v;
                        if (canvas) canvas.lineWidth = Math.round(v);
                    }
                    onPositionChanged: (m) => {
                        var v = vertSlider._calc(m.y - vsHandle.height/2);
                        vertSlider.value = v;
                        if (canvas) canvas.lineWidth = Math.round(v);
                    }
                }
            }
        }
    }

    Rectangle {
        id: eraserPopup
        parent: root
        x: toolbarPosition === "left"
            ? toolbar.x + toolbar.width + 12
            : toolbarPosition === "right"
                ? toolbar.x - width - 12
                : (root.width - width) / 2
        y: toolbarPosition === "top"
            ? toolbar.y + toolbar.height + 12
            : toolbarPosition === "bottom"
                ? toolbar.y - height - 12
                : toolbar.y + (toolbar.height - height) / 2
        width: 236
        height: 60
        visible: false
        opacity: 0
        scale: 0.92
        z: 100
        radius: 12
        color: root.popupBackgroundColor !== "" ? root.popupBackgroundColor : (darkBackground ? "#202020" : "#FFFFFF")
        border.color: root.popupBorderColor !== "" ? root.popupBorderColor : (darkBackground ? Qt.rgba(1, 1, 1, 0.1) : Qt.rgba(0, 0, 0, 0.1))
        border.width: 1
        clip: true

        Behavior on opacity { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }
        Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }

        property bool opened: visible
        function open()  {
            erCloseTimer.stop();
            visible = true;
            erOpenTimer.start();
        }
        function close() {
            erOpenTimer.stop();
            opacity = 0;
            scale = 0.92;
            erCloseTimer.start();
        }

        Timer { id: erOpenTimer; interval: 10; onTriggered: { if (eraserPopup.visible) { eraserPopup.opacity = 1; eraserPopup.scale = 1.0; } } }
        Timer { id: erCloseTimer; interval: 200; onTriggered: { eraserPopup.visible = false; } }

        Rectangle {
            id: clearSliderContainer
            anchors.fill: parent
            anchors.margins: 8
            radius: 22
            color: darkBackground ? Qt.rgba(1,1,1,0.08) : Qt.rgba(0,0,0,0.06)
            clip: true

            property real dragX: 2
            property bool dragging: false
            property real maxX: width - clearThumb.width - 2

            Text {
                id: clearText
                text: slideClearText
                anchors.centerIn: parent
                color: darkBackground ? Qt.rgba(1,1,1,0.5) : Qt.rgba(0,0,0,0.4)
                font.pixelSize: 14
                opacity: 1 - Math.max(0, Math.min(1, clearSliderContainer.dragX / clearSliderContainer.maxX * 1.5))
            }

            Rectangle {
                id: clearThumb
                width: 40
                height: 40
                radius: 20
                anchors.verticalCenter: parent.verticalCenter
                x: clearSliderContainer.dragX
                color: darkBackground ? "#484848" : "#444444"

                Image {
                    anchors.fill: parent
                    anchors.margins: 10
                    source: iconsDir + "Clear.svg"
                    sourceSize.width: 20
                    sourceSize.height: 20
                    fillMode: Image.PreserveAspectFit
                }

                Behavior on x {
                    enabled: !clearSliderContainer.dragging
                    NumberAnimation { duration: 200; easing.type: Easing.OutCubic }
                }

                scale: clearSliderContainer.dragging ? 0.95 : 1.0
                Behavior on scale { NumberAnimation { duration: 100 } }
            }

            MouseArea {
                anchors.fill: parent
                preventStealing: true

                onPressed: (m) => {
                    clearSliderContainer.dragging = true
                    var xInThumb = m.x - clearThumb.x
                    if (xInThumb < 0 || xInThumb > clearThumb.width) {
                        var newX = Math.max(2, Math.min(clearSliderContainer.maxX, m.x - clearThumb.width/2))
                        clearSliderContainer.dragX = newX
                    }
                }
                onPositionChanged: (m) => {
                    if (clearSliderContainer.dragging) {
                        var newX = Math.max(2, Math.min(clearSliderContainer.maxX, m.x - clearThumb.width/2))
                        clearSliderContainer.dragX = newX
                    }
                }
                onReleased: {
                    clearSliderContainer.dragging = false
                    if (clearSliderContainer.dragX >= clearSliderContainer.maxX - 4) {
                        if (canvas) canvas.clear()
                        eraserPopup.close()
                    }
                    clearSliderContainer.dragX = 2
                }
                onCanceled: {
                    clearSliderContainer.dragging = false
                    clearSliderContainer.dragX = 2
                }
            }
        }
    } // end eraserPopup

    // Floating Toolbar
    Rectangle {
        id: toolbar
        property bool isVertical: toolbarPosition === "left" || toolbarPosition === "right"
        property real itemHeight: showToolText ? 56 : 36
        property real capThickness: showToolText ? 76 : 50
        width: isVertical ? capThickness : grid.implicitWidth + (capThickness - itemHeight)
        height: isVertical ? grid.implicitHeight + (capThickness - itemHeight) : capThickness
        radius: isVertical ? width / 2 : height / 2
        color: Qt.rgba(0.2, 0.2, 0.2, 0.9)
        border.color: Qt.rgba(1, 1, 1, 0.2)
        border.width: 1
        anchors.bottom: board.bottom
        anchors.bottomMargin: 20
        anchors.horizontalCenter: board.horizontalCenter
        
        states: [
            State {
                name: "top"
                when: toolbarPosition === "top"
                AnchorChanges {
                    target: toolbar
                    anchors.top:              board.top
                    anchors.bottom:           undefined
                    anchors.left:             undefined
                    anchors.right:            undefined
                    anchors.horizontalCenter: board.horizontalCenter
                    anchors.verticalCenter:   undefined
                }
                PropertyChanges {
                    target: toolbar
                    anchors.topMargin: 20
                    anchors.bottomMargin: 0
                    anchors.leftMargin: 0
                    anchors.rightMargin: 0
                }
            },
            State {
                name: "left"
                when: toolbarPosition === "left"
                AnchorChanges {
                    target: toolbar
                    anchors.left:             board.left
                    anchors.right:            undefined
                    anchors.top:              undefined
                    anchors.bottom:           undefined
                    anchors.horizontalCenter: undefined
                    anchors.verticalCenter:   board.verticalCenter
                }
                PropertyChanges {
                    target: toolbar
                    anchors.leftMargin: 20
                    anchors.rightMargin: 0
                    anchors.topMargin: 0
                    anchors.bottomMargin: 0
                }
            },
            State {
                name: "right"
                when: toolbarPosition === "right"
                AnchorChanges {
                    target: toolbar
                    anchors.right:            board.right
                    anchors.left:             undefined
                    anchors.top:              undefined
                    anchors.bottom:           undefined
                    anchors.horizontalCenter: undefined
                    anchors.verticalCenter:   board.verticalCenter
                }
                PropertyChanges {
                    target: toolbar
                    anchors.rightMargin: 20
                    anchors.leftMargin: 0
                    anchors.topMargin: 0
                    anchors.bottomMargin: 0
                }
            }
        ]

        Grid {
            id: grid
            anchors.centerIn: parent
            spacing: 4
            columns: toolbar.isVertical ? 1 : 999

            // Pen
            Item {
                width: showToolText ? Math.max(36, textPen.contentWidth) : 36
                height: showToolText ? 56 : 36
                
                Column {
                    anchors.centerIn: parent
                    spacing: 4
                    
                    Item {
                        width: 36
                        height: 36
                        anchors.horizontalCenter: parent.horizontalCenter
                        
                        Rectangle {
                            anchors.fill: parent
                            radius: 18
                            color: !canvas.isEraser ? Qt.rgba(1, 1, 1, 0.2) : "transparent"
                            visible: !canvas.isEraser
                        }
                        
                        Image {
                            source: iconsDir + "Pen.svg"
                            width: 20
                            height: 20
                            anchors.centerIn: parent
                            sourceSize: Qt.size(20, 20)
                            opacity: 1.0
                        }
                    }
                    
                    Text {
                        id: textPen
                        text: penText
                        color: "white"
                        font.pixelSize: 11
                        anchors.horizontalCenter: parent.horizontalCenter
                        visible: showToolText
                    }
                }
                
                MouseArea {
                    anchors.fill: parent
                    cursorShape: Qt.PointingHandCursor
                    onClicked: {
                        if (!canvas.isEraser) {
                            if (colorPopup.opened) colorPopup.close();
                            else colorPopup.open();
                        } else {
                            canvas.isEraser = false
                        }
                        if (eraserPopup.opened) eraserPopup.close();
                    }
                }
            }
            
            // Eraser
            Item {
                width: showToolText ? Math.max(36, textEraser.contentWidth) : 36
                height: showToolText ? 56 : 36
                
                Column {
                    anchors.centerIn: parent
                    spacing: 4
                    
                    Item {
                        width: 36
                        height: 36
                        anchors.horizontalCenter: parent.horizontalCenter
                        
                        Rectangle {
                            anchors.fill: parent
                            radius: 18
                            color: canvas.isEraser ? Qt.rgba(1, 1, 1, 0.2) : "transparent"
                            visible: canvas.isEraser
                        }
                        
                        Image {
                            source: iconsDir + "Eraser.svg"
                            width: 20
                            height: 20
                            anchors.centerIn: parent
                            sourceSize: Qt.size(20, 20)
                            opacity: 1.0
                        }
                    }
                    
                    Text {
                        id: textEraser
                        text: eraserText
                        color: "white"
                        font.pixelSize: 11
                        anchors.horizontalCenter: parent.horizontalCenter
                        visible: showToolText
                    }
                }
                
                MouseArea {
                    anchors.fill: parent
                    cursorShape: Qt.PointingHandCursor
                    enabled: true
                    onClicked: {
                        if (canvas.isEraser) {
                            if (eraserPopup.opened) eraserPopup.close();
                            else eraserPopup.open();
                        } else {
                            canvas.isEraser = true
                            if (colorPopup.opened) colorPopup.close();
                        }
                    }
                }
            }
            
            // Clear
            Item {
                width: showToolText ? Math.max(36, textClear.contentWidth) : 36
                height: showToolText ? 56 : 36
                
                Column {
                    anchors.centerIn: parent
                    spacing: 4
                    
                    Item {
                        width: 36
                        height: 36
                        anchors.horizontalCenter: parent.horizontalCenter
                        
                        Image {
                            source: iconsDir + "Clear.svg"
                            width: 20
                            height: 20
                            anchors.centerIn: parent
                            sourceSize: Qt.size(20, 20)
                            opacity: 1.0
                        }
                    }
                    
                    Text {
                        id: textClear
                        text: slideClearText
                        color: "white"
                        font.pixelSize: 11
                        anchors.horizontalCenter: parent.horizontalCenter
                        visible: showToolText
                    }
                }
                
                MouseArea {
                    anchors.fill: parent
                    cursorShape: Qt.PointingHandCursor
                    enabled: true
                    onClicked: canvas.clear()
                }
            }

            // Undo
            Item {
                width: showToolText ? Math.max(36, textUndo.contentWidth) : 36
                height: showToolText ? 56 : 36
                opacity: (canvas && canvas.canUndo) ? 1.0 : 0.38

                Column {
                    anchors.centerIn: parent
                    spacing: 4

                    Item {
                        width: 36
                        height: 36
                        anchors.horizontalCenter: parent.horizontalCenter

                        Image {
                            source: iconsDir + "undo.svg"
                            width: 20
                            height: 20
                            anchors.centerIn: parent
                            sourceSize: Qt.size(20, 20)
                            opacity: 1.0
                        }
                    }

                    Text {
                        id: textUndo
                        text: undoText
                        color: "white"
                        font.pixelSize: 11
                        anchors.horizontalCenter: parent.horizontalCenter
                        visible: showToolText
                    }
                }

                MouseArea {
                    anchors.fill: parent
                    cursorShape: (canvas && canvas.canUndo) ? Qt.PointingHandCursor : Qt.ArrowCursor
                    enabled: canvas ? canvas.canUndo : false
                    onClicked: {
                        if (colorPopup.opened) colorPopup.close();
                        if (eraserPopup.opened) eraserPopup.close();
                        if (canvas) canvas.undo();
                    }
                }
            }

            // Redo
            Item {
                width: showToolText ? Math.max(36, textRedo.contentWidth) : 36
                height: showToolText ? 56 : 36
                opacity: (canvas && canvas.canRedo) ? 1.0 : 0.38

                Column {
                    anchors.centerIn: parent
                    spacing: 4

                    Item {
                        width: 36
                        height: 36
                        anchors.horizontalCenter: parent.horizontalCenter

                        Image {
                            source: iconsDir + "redo.svg"
                            width: 20
                            height: 20
                            anchors.centerIn: parent
                            sourceSize: Qt.size(20, 20)
                            opacity: 1.0
                        }
                    }

                    Text {
                        id: textRedo
                        text: redoText
                        color: "white"
                        font.pixelSize: 11
                        anchors.horizontalCenter: parent.horizontalCenter
                        visible: showToolText
                    }
                }

                MouseArea {
                    anchors.fill: parent
                    cursorShape: (canvas && canvas.canRedo) ? Qt.PointingHandCursor : Qt.ArrowCursor
                    enabled: canvas ? canvas.canRedo : false
                    onClicked: {
                        if (colorPopup.opened) colorPopup.close();
                        if (eraserPopup.opened) eraserPopup.close();
                        if (canvas) canvas.redo();
                    }
                }
            }
        }

        z: 90
    }

    Rectangle {
        id: fullscreenToggle
        width: toolbar.height
        height: toolbar.height
        radius: toolbar.radius
        color: toolbar.color
        border.color: toolbar.border.color
        border.width: toolbar.border.width
        anchors.left:       board.left
        anchors.bottom:     toolbar.bottom
        anchors.leftMargin: 20
        z: 91

        Image {
            source: iconsDir + ((backend && backend.isFullscreen) ? "exitfullscr.svg" : "fullscr.svg")
            width: 20
            height: 20
            anchors.centerIn: parent
            sourceSize: Qt.size(20, 20)
            opacity: 1.0
        }

        MouseArea {
            anchors.fill: parent
            cursorShape: Qt.PointingHandCursor
            onClicked: if (backend) backend.toggleFullscreen()
        }
    }

}
