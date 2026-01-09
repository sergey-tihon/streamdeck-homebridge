[<RequireQualifiedAccessAttribute>]
module StreamDeck.SDK.Components.Pi

open Feliz
open StreamDeck.SDK.Css

let input (label: string) props =
    Html.div [
        prop.className SdPi.Item
        prop.type' "field"
        prop.children [
            Html.div [ prop.className SdPi.ItemLabel; prop.text label ]
            Html.input(prop.className SdPi.ItemValue :: props)
        ]
    ]

let selectElement props =
    Html.select(prop.classes [ SdPi.ItemValue; "select" ] :: props)

let select (label: string) props =
    Html.div [
        prop.className SdPi.Item
        prop.children [
            Html.div [ prop.className SdPi.ItemLabel; prop.text label ]
            Html.select(prop.classes [ SdPi.ItemValue; "select" ] :: props)
        ]
    ]

let row (label: string) (children: ReactElement list) =
    Html.div [
        prop.className SdPi.Item
        prop.children(
            Html.div [ prop.className SdPi.ItemLabel; prop.text label ]
            :: children
        )
    ]

let iconButton (icon: string) (tooltip: string) (onClick: unit -> unit) =
    Html.button [
        prop.style [
            style.backgroundColor "#2d2d2d"
            style.border(1, borderStyle.solid, "#969696")
            style.borderRadius 3
            style.padding 6
            style.cursor.pointer
            style.display.flex
            style.alignItems.center
            style.justifyContent.center
            style.flexShrink 0
            style.marginRight 13
        ]
        prop.title tooltip
        prop.dangerouslySetInnerHTML icon
        prop.onClick(fun _ -> onClick())
    ]

let range (label: string) (children: ReactElement seq) =
    Html.div [
        prop.className SdPi.Item
        prop.type' "range"
        prop.children [
            Html.div [ prop.className SdPi.ItemLabel; prop.text label ]
            Html.div [ prop.className SdPi.ItemValue; prop.children children ]
        ]
    ]

let button (text: string) onClick =
    Html.div [
        prop.className SdPi.Item
        prop.type' "button"
        prop.children [
            Html.button [ prop.className SdPi.ItemValue; prop.text text; prop.onClick onClick ]
        ]
    ]

let message icon color (message: string) =
    Html.details [
        prop.classes [ "message"; icon ]
        prop.children [ Html.summary [ prop.style [ style.color color ]; prop.text message ] ]
    ]
