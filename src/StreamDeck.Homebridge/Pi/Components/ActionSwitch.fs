module StreamDeck.Homebridge.PiActionSwitch

open StreamDeck.SDK.Components
open StreamDeck.Homebridge.PiModel

let view (model: PiModel) (dispatch: PiMsg -> unit) = [
    let characteristicSelector = PiCharacteristicSelector.view model dispatch

    match model.ActionSetting.AccessoryId with
    | Some uniqueId when model.SwitchAccessories.ContainsKey uniqueId ->
        let accessory = model.SwitchAccessories |> Map.find uniqueId
        characteristicSelector accessory

        match model.ActionSetting.CharacteristicType with
        | Some _ ->
            if model.IsDevMode then
                Pi.button "Emit Switch action" (fun _ -> dispatch <| PiMsg.EmitEvent None)

            PiConfirmation.view
        | None -> ()
    | _ -> ()
]
