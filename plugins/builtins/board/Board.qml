import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import QtQuick.Window 2.15

Rectangle {
    id: root
    color: "transparent"
    focus: true
    Keys.onEscapePressed: backend.closeWindow()
    
    // Main Content Area
    Rectangle {
        id: board
        anchors.fill: parent
        color: "#202020"
        clip: true
        
        Canvas {
            id: canvas
            objectName: "canvas"
            anchors.fill: parent
            renderTarget: Canvas.FramebufferObject
            renderStrategy: Canvas.Threaded
            
            property color drawColor: "white"
            property int lineWidth: 3
            property bool isEraser: false
            
            property var lastX
            property var lastY
            property var pendingLines: []
            property var allLines: [] // Store all strokes history
            
            onPaint: {
                var ctx = getContext("2d");
                ctx.lineJoin = "round";
                ctx.lineCap = "round";
                
                while (pendingLines.length > 0) {
                    var line = pendingLines.shift();
                    ctx.beginPath();
                    if (line.isEraser) {
                         ctx.globalCompositeOperation = "destination-out";
                         ctx.lineWidth = 20;
                    } else {
                        ctx.globalCompositeOperation = "source-over";
                        ctx.strokeStyle = line.color;
                        ctx.lineWidth = line.width;
                    }
                    ctx.moveTo(line.x1, line.y1);
                    ctx.lineTo(line.x2, line.y2);
                    ctx.stroke();
                }
            }
            
            MouseArea {
                anchors.fill: parent
                onPressed: (mouse) => {
                    canvas.lastX = mouse.x
                    canvas.lastY = mouse.y
                }
                onPositionChanged: (mouse) => {
                    var line = {
                        x1: canvas.lastX,
                        y1: canvas.lastY,
                        x2: mouse.x,
                        y2: mouse.y,
                        color: canvas.drawColor.toString(),
                        width: canvas.lineWidth,
                        isEraser: canvas.isEraser
                    };
                    canvas.pendingLines.push(line);
                    canvas.allLines.push(line);
                    canvas.lastX = mouse.x;
                    canvas.lastY = mouse.y;
                    canvas.requestPaint();
                }
            }
            
            function clear() {
                var ctx = getContext("2d");
                ctx.clearRect(0, 0, width, height);
                canvas.allLines = [];
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
                var ctx = getContext("2d");
                ctx.clearRect(0, 0, width, height);
                canvas.allLines = strokes;
                for (var i = 0; i < strokes.length; i++) {
                    canvas.pendingLines.push(strokes[i]);
                }
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
        color: Qt.rgba(1, 1, 1, 0.3)
        font.pixelSize: 12
        horizontalAlignment: Text.AlignRight
        visible: typeof showWatermark !== "undefined" ? showWatermark : false
        z: 100
    }

    // Color Popup
    Popup {
        id: colorPopup
        parent: root
        x: (root.width - width) / 2
        y: root.height - toolbar.height - height - 30
        width: 260
        height: 160
        padding: 12
        
        background: Rectangle {
            color: "#202020"
            radius: 12
            border.color: Qt.rgba(1, 1, 1, 0.1)
            border.width: 1
        }
        
        contentItem: Column {
            spacing: 12
            
            Text {
                text: themeColorsText
                color: "white"
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
                color: "white"
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
        property real itemHeight: showToolText ? 56 : 36
        width: row.width + (height - itemHeight)
        height: showToolText ? 76 : 50
        radius: height / 2
        color: Qt.rgba(0.2, 0.2, 0.2, 0.9)
        border.color: Qt.rgba(1, 1, 1, 0.2)
        border.width: 1
        anchors.bottom: parent.bottom
        anchors.bottomMargin: 20
        anchors.horizontalCenter: parent.horizontalCenter
        
        Row {
            id: row
            anchors.centerIn: parent
            spacing: 4
            
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
                    onClicked: canvas.clear()
            }
        }
    }
}
}
