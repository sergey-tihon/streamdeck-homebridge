module StreamDeck.Homebridge.PiActionAdjust

open Feliz
open StreamDeck.SDK.Components
open StreamDeck.Homebridge.PiModel

let view (model: PiModel) (dispatch: PiMsg -> unit) = [
    let characteristicSelector = PiCharacteristicSelector.view model dispatch

    yield! PiAccessorySelector.view model dispatch model.RangeAccessories

    match model.ActionSetting.AccessoryId with
    | Some uniqueId when model.RangeAccessories.ContainsKey uniqueId ->
        let accessory = model.RangeAccessories |> Map.find uniqueId
        characteristicSelector accessory

        match model.ActionSetting.CharacteristicType with
        | Some characteristicType ->
            let ch = accessory |> PiUpdate.getCharacteristic characteristicType

            match ch.minValue, ch.minStep, ch.maxValue with
            | Some minValue, Some minStep, Some maxValue ->
                let maxValue = (maxValue - minValue) / minStep / 2
                let speed = model.ActionSetting.Speed |> Option.defaultValue 1

                Pi.range $"Speed ({speed}x)" [
                    Html.span [ prop.className "clickable"; prop.value minValue; prop.text $"1x" ]
                    Html.input [
                        prop.type' "range"
                        prop.min 1
                        prop.max maxValue
                        prop.step 1
                        prop.value speed
                        prop.onChange(fun (x: int) ->
                            let payload = Some x
                            dispatch <| PiMsg.ChangeSpeed payload)
                    ]
                    Html.span [ prop.className "clickable"; prop.value maxValue; prop.text $"{maxValue}x" ]
                ]
            | _ -> ()
        | _ -> ()

        if model.IsDevMode then
            Html.div [
                prop.style [ style.display.flex; style.maxWidth 344 ]
                prop.children [
                    Html.div [
                        prop.style [ style.flexGrow 1 ]
                        prop.children [
                            Pi.button "Emit Left Rotation" (fun _ -> dispatch <| PiMsg.EmitEvent(Some -1))
                        ]
                    ]
                    Html.div [
                        prop.style [ style.flexGrow 1 ]
                        prop.children [
                            Pi.button "Emit Right Rotation" (fun _ -> dispatch <| PiMsg.EmitEvent(Some 1))
                        ]
                    ]
                ]
            ]

        PiConfirmation.view
    | _ -> ()
]
