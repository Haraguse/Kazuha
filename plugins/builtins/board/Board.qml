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
            
            MouseArea {
                anchors.fill: parent
                onPressed: (mouse) => {
                    canvas.lastX = mouse.x / canvas.width
                    canvas.lastY = mouse.y / canvas.height
                }
                onPositionChanged: (mouse) => {
                    var currentX = mouse.x / canvas.width;
                    var currentY = mouse.y / canvas.height;
                    
                    var line = {
                        x1: canvas.lastX,
                        y1: canvas.lastY,
                        x2: currentX,
                        y2: currentY,
                        color: canvas.drawColor.toString(),
                        width: canvas.lineWidth,
                        isEraser: canvas.isEraser
                    };
                    canvas.pendingLines.push(line);
                    canvas.allLines.push(line);
                    canvas.lastX = currentX;
                    canvas.lastY = currentY;
                    canvas.requestPaint();
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
            ? toolbar.x + toolbar.width + 20
            : toolbarPosition === "right"
                ? toolbar.x - width - 20
                : (root.width - width) / 2
        y: toolbarPosition === "top"
            ? toolbar.y + toolbar.height + 20
            : toolbarPosition === "bottom"
                ? toolbar.y - height - 30
                : toolbar.y + (toolbar.height - height) / 2
        width: 260
        height: 160
        padding: 12
        
        background: Rectangle {
            color: root.popupBackgroundColor !== "" ? root.popupBackgroundColor : (darkBackground ? "#202020" : "#FFFFFF")
            radius: 12
            border.color: root.popupBorderColor !== "" ? root.popupBorderColor : (darkBackground ? Qt.rgba(1, 1, 1, 0.1) : Qt.rgba(0, 0, 0, 0.1))
            border.width: 1
        }
        
        contentItem: Column {
            spacing: 12
            
            Text {
                text: themeColorsText
                color: darkBackground ? "white" : "black"
                font.pixelSize: 12
                opacity: 0.8
            }
            
            Grid {
                columns: 10
                spacing: 4
                Repeater {
                    model: themeColors
                    Rectangle {
                        width: 20
                        height: 20
                        color: modelData
                        radius: 4
                        border.width: 1
                        border.color: Qt.rgba(0,0,0,0.1)
                        
                        MouseArea {
                            anchors.fill: parent
                            cursorShape: Qt.PointingHandCursor
                            hoverEnabled: true
                            onEntered: {
                                parent.border.color = "white"
                                parent.border.width = 2
                            }
                            onExited: {
                                parent.border.color = Qt.rgba(0,0,0,0.1)
                                parent.border.width = 1
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
            
            Text {
                text: standardColorsText
                color: darkBackground ? "white" : "black"
                font.pixelSize: 12
                opacity: 0.8
            }
            
            Grid {
                columns: 10
                spacing: 4
                Repeater {
                    model: standardColors
                    Rectangle {
                        width: 20
                        height: 20
                        color: modelData
                        radius: 4
                        border.width: 1
                        border.color: Qt.rgba(0,0,0,0.1)
                        
                        MouseArea {
                            anchors.fill: parent
                            cursorShape: Qt.PointingHandCursor
                            hoverEnabled: true
                            onEntered: {
                                parent.border.color = "white"
                                parent.border.width = 2
                            }
                            onExited: {
                                parent.border.color = Qt.rgba(0,0,0,0.1)
                                parent.border.width = 1
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
                        
                        ToolButton {
                            anchors.fill: parent
                            enabled: true
                            icon.source: iconsDir + "Pen.svg"
                            icon.color: canvas.drawColor
                            icon.width: 20
                            icon.height: 20
                            
                            background: Rectangle {
                                anchors.fill: parent
                                radius: 18
                                color: !canvas.isEraser ? Qt.rgba(1, 1, 1, 0.2) : "transparent"
                                visible: !canvas.isEraser
                            }
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
