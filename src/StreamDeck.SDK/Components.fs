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
            Html.input((prop.className SdPi.ItemValue) :: props)
        ]
    ]

let select (label: string) props =
    Html.div [
        prop.className SdPi.Item
        prop.children [
            Html.div [ prop.className SdPi.ItemLabel; prop.text label ]
            Html.select((prop.classes [ SdPi.ItemValue; "select" ]) :: props)
        ]
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
