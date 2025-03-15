module StreamDeck.Homebridge.PiView

open Feliz
open StreamDeck.SDK.Css
open StreamDeck.SDK.Components
open StreamDeck.Homebridge
open StreamDeck.Homebridge.PiModel

let render (model: PiModel) (dispatch: PiMsg -> unit) =
    let accessorySelector = PiAccessorySelector.view model dispatch

    Feliz.Html.div [
        prop.className SdPi.Wrapper
        prop.children [
            match model.IsLoading with
            | Ok true -> Pi.message "info" "orange" "Waiting for Homebridge API response ..."
            | _ ->
                match model.IsLoading with
                | Error error -> Pi.message "caution" "red" error
                | _ -> ()

                match model.Client with
                | Error error -> yield! PiLogin.view model dispatch error
                | Ok _ ->
                    match model.ActionType with
                    | None ->
                        Pi.select "Action Type" [
                            prop.value(model.ActionType |> Option.defaultValue "DEFAULT")
                            prop.children [
                                Html.option [ prop.value "DEFAULT" ]
                                Html.option [ prop.value Domain.ActionName.ConfigUi; prop.text "Config UI" ]
                                Html.option [ prop.value Domain.ActionName.Switch; prop.text "Switch" ]
                                Html.option [ prop.value Domain.ActionName.Set; prop.text "Set State" ]
                                Html.option [ prop.value Domain.ActionName.Adjust; prop.text "Adjust State" ]
                            ]
                            prop.onChange(fun value ->
                                let msg = if value = "DEFAULT" then None else Some value
                                dispatch <| PiMsg.SelectActionType msg)
                        ]
                    | Some Domain.ActionName.ConfigUi -> PiConfirmation.view
                    | Some Domain.ActionName.Switch ->
                        yield! accessorySelector model.SwitchAccessories
                        yield! PiActionSwitch.view model dispatch
                    | Some Domain.ActionName.Set ->
                        yield! accessorySelector model.RangeAccessories
                        yield! PiActionSet.view model dispatch
                    | Some Domain.ActionName.Adjust ->
                        yield! accessorySelector model.RangeAccessories
                        yield! PiActionAdjust.view model dispatch
                    | Some ty -> Html.p [ prop.text $"Unsupported action type {ty}" ]

                    Pi.button "Logout" (fun _ -> dispatch <| PiMsg.Logout)
        ]
    ]
