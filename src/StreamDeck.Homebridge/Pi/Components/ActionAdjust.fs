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
