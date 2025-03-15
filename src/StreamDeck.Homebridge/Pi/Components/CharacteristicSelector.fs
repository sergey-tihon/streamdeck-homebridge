module StreamDeck.Homebridge.PiCharacteristicSelector

open System.Text.RegularExpressions
open Feliz
open StreamDeck.SDK.Components
open StreamDeck.Homebridge.PiModel

let view (model: PiModel) (dispatch: PiMsg -> unit) (accessory: Client.AccessoryDetails) =
    Pi.select "Characteristic" [
        prop.value(
            model.ActionSetting.CharacteristicType
            |> Option.defaultValue "DEFAULT"
        )
        prop.children [
            Html.option [ prop.value "DEFAULT" ]
            let characteristics =
                accessory.serviceCharacteristics |> Array.sortBy(fun x -> x.``type``)

            for x in characteristics do
                Html.option [ prop.value x.``type``; prop.text x.description ]
        ]
        prop.onChange(fun (value: string) ->
            let msg = if value = "DEFAULT" then None else Some value
            dispatch <| PiMsg.SelectCharacteristic msg)
    ]

let successConfirmation = Pi.message "" "green" "Button successfully configured"
