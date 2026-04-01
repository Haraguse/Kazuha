import QtQuick

Item {
    id: root
    property string screenTitle: ""
    property string brandText: "Luminalium"
    property url logoSource: ""
    property url fontSource: ""
    property bool darkMode: true
    property bool animationsEnabled: true
    property bool loading: true

    visible: loading || opacity > 0.001
    enabled: loading
    opacity: loading ? 1 : 0

    FontLoader {
        id: miSansFont
        source: root.fontSource
    }

    readonly property string displayFontFamily: (
        miSansFont.status === FontLoader.Ready
        ? miSansFont.name
        : "Sans Serif"
    )

    Behavior on opacity {
        enabled: root.animationsEnabled
        NumberAnimation {
            duration: root.loading ? 120 : 420
            easing.type: Easing.InOutQuad
        }
    }

    Rectangle {
        anchors.fill: parent
        color: root.darkMode ? "#141414" : "#FFFFFF"
    }

    Item {
        id: centerBlock
        width: Math.min(root.width * 0.74, 620)
        height: titleText.paintedHeight
        anchors.centerIn: parent

        Text {
            id: titleText
            width: parent.width
            anchors.top: parent.top
            anchors.horizontalCenter: parent.horizontalCenter
            text: root.screenTitle
            horizontalAlignment: Text.AlignHCenter
            wrapMode: Text.WordWrap
            color: root.darkMode ? "#F2F2F2" : "#111111"
            font.family: root.displayFontFamily
            font.pixelSize: Math.max(38, Math.min(76, root.height * 0.115))
            font.weight: Font.DemiBold
            renderType: Text.NativeRendering
        }
    }

    Row {
        id: brandRow
        anchors.horizontalCenter: parent.horizontalCenter
        anchors.bottom: parent.bottom
        anchors.bottomMargin: Math.max(22, root.height * 0.045)
        spacing: 12
        height: Math.max(logoImage.height, brandLabel.paintedHeight)

        Image {
            id: logoImage
            anchors.verticalCenter: parent.verticalCenter
            source: root.logoSource
            width: 24
            height: 24
            fillMode: Image.PreserveAspectFit
            smooth: true
            mipmap: true
            asynchronous: true
        }

        Text {
            id: brandLabel
            anchors.verticalCenter: parent.verticalCenter
            text: root.brandText
            color: root.darkMode ? "#B8B8B8" : "#444444"
            font.family: root.displayFontFamily
            font.pixelSize: Math.max(12, Math.min(17, root.height * 0.023))
            font.weight: Font.Medium
            font.letterSpacing: 0.35
            renderType: Text.NativeRendering
        }
    }

    MouseArea {
        anchors.fill: parent
        enabled: root.loading
        hoverEnabled: true
    }
}
