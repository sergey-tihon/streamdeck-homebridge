module StreamDeck.Homebridge.PiActionSet

open Feliz
open StreamDeck.SDK.Components
open StreamDeck.Homebridge.PiModel

let view (model: PiModel) (dispatch: PiMsg -> unit) = [
    let characteristicSelector = PiCharacteristicSelector.view model dispatch

    yield! PiAccessorySelector.view model dispatch model.RangeAccessories

    match model.ActionSetting.AccessoryId with
    | Some uniqueId when model.SwitchAccessories.ContainsKey uniqueId ->
        let accessory = model.SwitchAccessories |> Map.find uniqueId
        characteristicSelector accessory

        match model.ActionSetting.AccessoryId with
        | Some uniqueId when model.RangeAccessories.ContainsKey uniqueId ->
            let accessory = model.RangeAccessories |> Map.find uniqueId
            characteristicSelector accessory

            match model.ActionSetting.CharacteristicType, model.ActionSetting.TargetValue with
            | Some characteristicType, Some targetValue ->
                let ch = accessory |> PiUpdate.getCharacteristic characteristicType

                match ch.minValue, ch.minStep, ch.maxValue with
                | Some minValue, Some minStep, Some maxValue ->
                    Pi.range $"Target value ({targetValue})" [
                        Html.span [ prop.className "clickable"; prop.value minValue; prop.text $"{minValue}" ]
                        Html.input [
                            prop.type' "range"
                            prop.min minValue
                            prop.max maxValue
                            prop.step minStep
                            prop.value targetValue
                            prop.onChange(fun (x: float) ->
                                let payload = Some x
                                dispatch <| PiMsg.ChangeTargetValue payload)
                        ]
                        Html.span [ prop.className "clickable"; prop.value maxValue; prop.text $"{maxValue}" ]
                    ]

                    if model.IsDevMode then
                        Pi.button "Emit Set action" (fun _ -> dispatch <| PiMsg.EmitEvent None)

                    PiConfirmation.view
                | _ -> ()
            | _ -> ()
        | _ -> ()
    | _ -> ()
]
