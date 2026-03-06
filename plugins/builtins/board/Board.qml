import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import QtQuick.Window 2.15

Rectangle {
    id: root
    color: "transparent"
    focus: true
    property string toolbarPosition: typeof boardToolbarPosition !== "undefined" ? boardToolbarPosition : "bottom"
    property string backgroundColor: typeof boardBackgroundColor !== "undefined" ? boardBackgroundColor : "#202020"
    property string popupBackgroundColor: typeof boardPopupBackgroundColor !== "undefined" ? boardPopupBackgroundColor : ""
    property string popupBorderColor: typeof boardPopupBorderColor !== "undefined" ? boardPopupBorderColor : ""
    property int eraserMode: typeof boardEraserMode !== "undefined" ? boardEraserMode : 0
    property bool darkBackground: isDarkColor(backgroundColor)
    Keys.onEscapePressed: backend.closeWindow()

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
    
    // Main Content Area
    Rectangle {
        id: board
        anchors.fill: parent
        color: backgroundColor
        clip: true
        
        Canvas {
            id: canvas
            objectName: "canvas"
            anchors.fill: parent
            renderTarget: Canvas.FramebufferObject
            renderStrategy: Canvas.Threaded
            
            property color drawColor: darkBackground ? "white" : "black"
            property int lineWidth: 3
            property bool isEraser: false
            property int eraserMode: root.eraserMode // Bind to root property
            property int currentStrokeId: 0
            
            property var lastX
            property var lastY
            property var pendingLines: []
            property var allLines: [] // Store all strokes history
            property bool needsFullRepaint: false

            onWidthChanged: {
                needsFullRepaint = true;
                requestPaint();
            }
            onHeightChanged: {
                needsFullRepaint = true;
                requestPaint();
            }
            
            onPaint: {
                var ctx = getContext("2d");
                ctx.lineJoin = "round";
                ctx.lineCap = "round";
                
                var w = width;
                var h = height;

                if (needsFullRepaint) {
                    ctx.clearRect(0, 0, w, h);
                    for (var i = 0; i < allLines.length; i++) {
                        drawLine(ctx, allLines[i], w, h);
                    }
                    needsFullRepaint = false;
                }

                while (pendingLines.length > 0) {
                    var line = pendingLines.shift();
                    drawLine(ctx, line, w, h);
                }
            }

            function drawLine(ctx, line, w, h) {
                ctx.beginPath();
                if (line.isEraser) {
                     ctx.globalCompositeOperation = "destination-out";
                     ctx.lineWidth = 20; 
                } else {
                    ctx.globalCompositeOperation = "source-over";
                    ctx.strokeStyle = line.color;
                    ctx.lineWidth = line.width;
                }
                
                var x1 = line.x1 * w;
                var y1 = line.y1 * h;
                var x2 = line.x2 * w;
                var y2 = line.y2 * h;

                ctx.moveTo(x1, y1);
                ctx.lineTo(x2, y2);
                ctx.stroke();
            }

            function hitTest(x, y) {
                // x, y are normalized (0-1)
                // Threshold: let's say 10 pixels converted to normalized
                var w = width;
                var h = height;
                if (w <= 0 || h <= 0) return -1;
                
                var t = 10 / w; // Approximation using width
                
                for (var i = 0; i < allLines.length; i++) {
                    var line = allLines[i];
                    if (line.isEraser) continue;
                    
                    // Simple bounding box check first
                    var minX = Math.min(line.x1, line.x2) - t;
                    var maxX = Math.max(line.x1, line.x2) + t;
                    var minY = Math.min(line.y1, line.y2) - t;
                    var maxY = Math.max(line.y1, line.y2) + t;
                    
                    if (x >= minX && x <= maxX && y >= minY && y <= maxY) {
                        // Detailed distance check
                        var dist = distToSegment(x, y, line.x1, line.y1, line.x2, line.y2);
                        // Normalize distance check roughly (aspect ratio might skew this but ok for now)
                        if (dist < t) {
                            return line.strokeId;
                        }
                    }
                }
                return -1;
            }
            
            function distToSegment(x, y, x1, y1, x2, y2) {
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
                return Math.sqrt(dx * dx + dy * dy);
            }

            function removeStroke(strokeId) {
                if (strokeId === undefined || strokeId === null || strokeId === -1) return;
                
                var newLines = [];
                var changed = false;
                for (var i = 0; i < allLines.length; i++) {
                    if (allLines[i].strokeId !== strokeId) {
                        newLines.push(allLines[i]);
                    } else {
                        changed = true;
                    }
                }
                
                if (changed) {
                    allLines = newLines;
                    needsFullRepaint = true;
                    requestPaint();
                }
            }
            
            MouseArea {
                anchors.fill: parent
                onPressed: (mouse) => {
                    canvas.lastX = mouse.x / canvas.width
                    canvas.lastY = mouse.y / canvas.height
                    canvas.currentStrokeId++;
                }
                onReleased: (mouse) => {
                    canvas.currentStrokeId++;
                }
                onPositionChanged: (mouse) => {
                    var currentX = mouse.x / canvas.width;
                    var currentY = mouse.y / canvas.height;
                    
                    if (canvas.isEraser && canvas.eraserMode === 1) {
                        // Stroke Eraser
                        var hitId = canvas.hitTest(currentX, currentY);
                        if (hitId !== -1) {
                            canvas.removeStroke(hitId);
                        }
                    } else {
                        // Point Eraser or Pen
                        var line = {
                            x1: canvas.lastX,
                            y1: canvas.lastY,
                            x2: currentX,
                            y2: currentY,
                            color: canvas.drawColor.toString(),
                            width: canvas.lineWidth,
                            isEraser: canvas.isEraser,
                            strokeId: canvas.currentStrokeId
                        };
                        canvas.pendingLines.push(line);
                        canvas.allLines.push(line);
                        canvas.lastX = currentX;
                        canvas.lastY = currentY;
                        canvas.requestPaint();
                    }
                }
            }
            
            function clear() {
                var ctx = getContext("2d");
                ctx.clearRect(0, 0, width, height);
                canvas.allLines = [];
                canvas.pendingLines = [];
                requestPaint();
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
                        isEraser: s.isEraser
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
                canvas.needsFullRepaint = true;
                canvas.requestPaint();
            }
        }
    }
    
    // Watermark
    Text {
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        anchors.rightMargin: 15
        anchors.bottomMargin: 10
        text: typeof watermarkText !== "undefined" ? watermarkText : ""
        color: darkBackground ? Qt.rgba(1, 1, 1, 0.3) : Qt.rgba(0, 0, 0, 0.3)
        font.pixelSize: 12
        horizontalAlignment: Text.AlignRight
        visible: typeof showWatermark !== "undefined" ? showWatermark : false
        z: 100
    }

    // Color Popup
    Popup {
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
        width: 300
        height: 200
        padding: 0
        
        background: Rectangle {
            color: root.popupBackgroundColor !== "" ? root.popupBackgroundColor : (darkBackground ? "#202020" : "#FFFFFF")
            radius: 12
            border.color: root.popupBorderColor !== "" ? root.popupBorderColor : (darkBackground ? Qt.rgba(1, 1, 1, 0.1) : Qt.rgba(0, 0, 0, 0.1))
            border.width: 1
            
            layer.enabled: true
        }
        
        property int activeTab: 0 // 0: Theme, 1: Standard
        property string hoveredColorName: ""
        property string hoveredColorRgb: ""
        property string hoveredColorHex: ""

        contentItem: Item {
            id: contentContainer
            anchors.fill: parent
            
            // Tab Header
            Row {
                id: tabHeader
                height: 48
                anchors.top: parent.top
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.leftMargin: 16
                anchors.rightMargin: 16
                spacing: 24
                
                Item {
                    width: tabThemeText.contentWidth
                    height: parent.height
                    
                    Text {
                        id: tabThemeText
                        text: themeColorsText
                        color: colorPopup.activeTab === 0 
                               ? (darkBackground ? "white" : "black") 
                               : (darkBackground ? "#888" : "#666")
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
                        color: darkBackground ? "white" : "black"
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
                        color: colorPopup.activeTab === 1 
                               ? (darkBackground ? "white" : "black") 
                               : (darkBackground ? "#888" : "#666")
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
                        color: darkBackground ? "white" : "black"
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
                color: darkBackground ? Qt.rgba(1,1,1,0.08) : Qt.rgba(0,0,0,0.08)
            }
            
            // Content Area
            Item {
                anchors.top: tabHeader.bottom
                anchors.bottom: infoBox.top
                anchors.left: parent.left
                anchors.right: parent.right
                
                // Theme Colors
                Grid {
                    visible: colorPopup.activeTab === 0
                    columns: 10
                    spacing: 6
                    anchors.centerIn: parent
                    
                    Repeater {
                        model: themeColors
                        Rectangle {
                            width: 22
                            height: 22
                            color: modelData
                            radius: 4
                            border.width: 1
                            border.color: darkBackground ? Qt.rgba(1,1,1,0.1) : Qt.rgba(0,0,0,0.1)
                            
                            MouseArea {
                                anchors.fill: parent
                                cursorShape: Qt.PointingHandCursor
                                hoverEnabled: true
                                onEntered: {
                                    parent.scale = 1.2
                                    parent.z = 1
                                    parent.border.color = darkBackground ? "white" : "black"
                                    parent.border.width = 2
                                    colorPopup.updateInfo(modelData)
                                }
                                onExited: {
                                    parent.scale = 1.0
                                    parent.z = 0
                                    parent.border.color = darkBackground ? Qt.rgba(1,1,1,0.1) : Qt.rgba(0,0,0,0.1)
                                    parent.border.width = 1
                                    colorPopup.clearInfo()
                                }
                                onClicked: {
                                    canvas.drawColor = modelData
                                    canvas.isEraser = false
                                    colorPopup.close()
                                }
                            }
                        }
                    }
                }
                
                // Standard Colors
                Grid {
                    visible: colorPopup.activeTab === 1
                    columns: 10
                    spacing: 6
                    anchors.centerIn: parent
                    
                    Repeater {
                        model: standardColors
                        Rectangle {
                            width: 22
                            height: 22
                            color: modelData
                            radius: 4
                            border.width: 1
                            border.color: darkBackground ? Qt.rgba(1,1,1,0.1) : Qt.rgba(0,0,0,0.1)
                            
                            MouseArea {
                                anchors.fill: parent
                                cursorShape: Qt.PointingHandCursor
                                hoverEnabled: true
                                onEntered: {
                                    parent.scale = 1.2
                                    parent.z = 1
                                    parent.border.color = darkBackground ? "white" : "black"
                                    parent.border.width = 2
                                    colorPopup.updateInfo(modelData)
                                }
                                onExited: {
                                    parent.scale = 1.0
                                    parent.z = 0
                                    parent.border.color = darkBackground ? Qt.rgba(1,1,1,0.1) : Qt.rgba(0,0,0,0.1)
                                    parent.border.width = 1
                                    colorPopup.clearInfo()
                                }
                                onClicked: {
                                    canvas.drawColor = modelData
                                    canvas.isEraser = false
                                    colorPopup.close()
                                }
                            }
                        }
                    }
                }
            }
            
            // Info Box
            Item {
                id: infoBox
                height: 48
                anchors.bottom: parent.bottom
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.margins: 16
                
                Rectangle {
                    anchors.top: parent.top
                    anchors.left: parent.left
                    anchors.right: parent.right
                    height: 1
                    color: darkBackground ? Qt.rgba(1,1,1,0.08) : Qt.rgba(0,0,0,0.08)
                }
                
                Row {
                    anchors.centerIn: parent
                    spacing: 8
                    
                    Rectangle {
                        width: 16
                        height: 16
                        radius: 4
                        color: colorPopup.hoveredColorHex !== "" ? colorPopup.hoveredColorHex : canvas.drawColor
                        border.width: 1
                        border.color: darkBackground ? Qt.rgba(1,1,1,0.2) : Qt.rgba(0,0,0,0.2)
                        anchors.verticalCenter: parent.verticalCenter
                    }
                    
                    Column {
                        anchors.verticalCenter: parent.verticalCenter
                        spacing: 2
                        
                        Text {
                            text: colorPopup.hoveredColorName !== "" ? colorPopup.hoveredColorName : (colorPopup.hoveredColorHex !== "" ? colorPopup.hoveredColorHex : canvas.drawColor.toString())
                            color: darkBackground ? "white" : "black"
                            font.pixelSize: 12
                            font.bold: true
                        }
                        
                        Text {
                            text: colorPopup.hoveredColorRgb !== "" ? colorPopup.hoveredColorRgb : ""
                            color: darkBackground ? "#AAA" : "#666"
                            font.pixelSize: 10
                            visible: text !== ""
                        }
                    }
                }
            }
        }
        
        function updateInfo(colorVal) {
            hoveredColorHex = colorVal
            hoveredColorRgb = getRgbString(colorVal)
            hoveredColorName = getColorName(colorVal)
        }
        
        function clearInfo() {
            hoveredColorHex = ""
            hoveredColorRgb = ""
            hoveredColorName = ""
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
            // Translations can be handled if needed, but for now English/Generic is fine or we can add a map property
            // If the color matches exactly one of our known ones, return a name.
            return map[upper] || upper;
        }
    }

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
        anchors.bottom: parent.bottom
        anchors.bottomMargin: 20
        anchors.horizontalCenter: parent.horizontalCenter
        
        states: [
            State {
                name: "top"
                when: toolbarPosition === "top"
                AnchorChanges {
                    target: toolbar
                    anchors.top: parent.top
                    anchors.bottom: undefined
                    anchors.left: undefined
                    anchors.right: undefined
                    anchors.horizontalCenter: parent.horizontalCenter
                    anchors.verticalCenter: undefined
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
                    anchors.left: parent.left
                    anchors.right: undefined
                    anchors.top: undefined
                    anchors.bottom: undefined
                    anchors.horizontalCenter: undefined
                    anchors.verticalCenter: parent.verticalCenter
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
                    anchors.right: parent.right
                    anchors.left: undefined
                    anchors.top: undefined
                    anchors.bottom: undefined
                    anchors.horizontalCenter: undefined
                    anchors.verticalCenter: parent.verticalCenter
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
            columns: isVertical ? 1 : 999
            
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
                            colorPopup.open()
                        } else {
                            canvas.isEraser = false
                        }
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
                        canvas.isEraser = true
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
                        text: clearText
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
    }
}
}
