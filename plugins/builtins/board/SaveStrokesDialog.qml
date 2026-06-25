import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    id: root
    color: typeof dialogWindowBg !== "undefined" ? dialogWindowBg : "#F5F5F5"
    focus: true
    implicitWidth: 452
    implicitHeight: 214

    property string titleText: typeof dialogTitle !== "undefined" ? dialogTitle : ""
    property string messageText: typeof dialogMessage !== "undefined" ? dialogMessage : ""
    property string confirmText: typeof dialogConfirmText !== "undefined" ? dialogConfirmText : ""
    property string discardText: typeof dialogDiscardText !== "undefined" ? dialogDiscardText : ""
    property string cancelText: typeof dialogCancelText !== "undefined" ? dialogCancelText : ""
    property color bgApp:        typeof dialogBgApp !== "undefined" ? dialogBgApp : "#FAFFFFFF"
    property color textPrimary:  typeof dialogTextPrimary !== "undefined" ? dialogTextPrimary : "#191919"
    property color textSecondary:typeof dialogTextSecondary !== "undefined" ? dialogTextSecondary : "#666666"
    property color accentBlue:   typeof dialogAccent !== "undefined" ? dialogAccent : "#3275F5"
    property color divider:      typeof dialogDivider !== "undefined" ? dialogDivider : "#14000000"
    property color itemHover:    typeof dialogItemHover !== "undefined" ? dialogItemHover : "#0A000000"
    property color buttonActive: typeof dialogButtonActive !== "undefined" ? dialogButtonActive : "#1E000000"
    property string fontFamily:  typeof dialogFontFamily !== "undefined" ? dialogFontFamily : ""

    Keys.onEscapePressed: {
        if (typeof dialogBridge !== "undefined" && dialogBridge) {
            dialogBridge.chooseCancel()
        }
    }

    component DialogButton: Rectangle {
        id: buttonRoot
        property string label: ""
        property bool accent: false
        property bool hovered: false
        property bool pressed: false
        signal clicked()

        implicitWidth: Math.max(88, buttonLabel.implicitWidth + 28)
        implicitHeight: 34
        radius: 17
        color: {
            if (buttonRoot.accent) {
                return buttonRoot.hovered ? accentBlue : "transparent"
            }
            if (buttonRoot.pressed) return buttonActive
            if (buttonRoot.hovered) return itemHover
            return "transparent"
        }
        border.width: 1
        border.color: buttonRoot.accent ? accentBlue : divider

        Behavior on color {
            ColorAnimation { duration: 150 }
        }

        Text {
            id: buttonLabel
            anchors.centerIn: parent
            text: buttonRoot.label
            color: buttonRoot.accent
               ? (buttonRoot.hovered ? "#FFFFFF" : accentBlue)
               : (buttonRoot.hovered || buttonRoot.pressed ? textPrimary : textSecondary)
            font.family: root.fontFamily
            font.pixelSize: 13
            font.weight: Font.Medium

            Behavior on color {
                ColorAnimation { duration: 150 }
            }
        }

        MouseArea {
            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onEntered: buttonRoot.hovered = true
            onExited: {
                buttonRoot.hovered = false
                buttonRoot.pressed = false
            }
            onPressed: buttonRoot.pressed = true
            onReleased: buttonRoot.pressed = false
            onClicked: buttonRoot.clicked()
        }
    }

    Item {
        id: dialogWindow
        anchors.fill: parent

        Item {
            id: container
            anchors.fill: parent
            anchors.leftMargin: 24
            anchors.rightMargin: 24
            anchors.topMargin: 16
            anchors.bottomMargin: 20

            Text {
                id: titleLabel
                anchors.top: parent.top
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.leftMargin: 4
                anchors.rightMargin: 4
                text: root.titleText
                color: root.textPrimary
                font.family: root.fontFamily
                font.pixelSize: 17
                font.weight: Font.DemiBold
                wrapMode: Text.Wrap
            }

            Item {
                id: cardContainer
                anchors.top: titleLabel.bottom
                anchors.topMargin: 10
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.bottom: footer.top
                anchors.bottomMargin: 16

                Rectangle {
                    id: contentCard
                    anchors.fill: parent
                    radius: 12
                    color: root.bgApp
                    border.width: 1
                    border.color: root.divider

                    Text {
                        id: textContent
                        anchors.fill: parent
                        anchors.margins: 24
                        text: root.messageText
                        color: root.textPrimary
                        font.family: root.fontFamily
                        font.pixelSize: 14
                        lineHeight: 1.6
                        lineHeightMode: Text.ProportionalHeight
                        wrapMode: Text.Wrap
                        verticalAlignment: Text.AlignVCenter
                    }
                }
            }

            Item {
                id: footer
                anchors.left: parent.left
                anchors.right: parent.right
                anchors.bottom: parent.bottom
                height: 34

                Row {
                    anchors.right: parent.right
                    anchors.rightMargin: 4
                    anchors.verticalCenter: parent.verticalCenter
                    spacing: 10

                    DialogButton {
                        label: root.cancelText
                        onClicked: {
                            if (typeof dialogBridge !== "undefined" && dialogBridge) {
                                dialogBridge.chooseCancel()
                            }
                        }
                    }

                    DialogButton {
                        label: root.discardText
                        onClicked: {
                            if (typeof dialogBridge !== "undefined" && dialogBridge) {
                                dialogBridge.chooseDiscard()
                            }
                        }
                    }

                    DialogButton {
                        label: root.confirmText
                        accent: true
                        onClicked: {
                            if (typeof dialogBridge !== "undefined" && dialogBridge) {
                                dialogBridge.chooseSave()
                            }
                        }
                    }
                }
            }
        }
    }
}
