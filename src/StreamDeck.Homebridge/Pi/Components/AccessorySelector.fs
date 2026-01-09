module StreamDeck.Homebridge.PiAccessorySelector

open Feliz
open StreamDeck.SDK.Components
open StreamDeck.Homebridge.PiModel

let private refreshIcon =
    """<svg xmlns='http://www.w3.org/2000/svg' height='16' width='16' viewBox='0 0 24 24' fill='#9C9C9C'><path d='M12 20q-3.35 0-5.675-2.325Q4 15.35 4 12q0-3.35 2.325-5.675Q8.65 4 12 4q1.725 0 3.3.713 1.575.712 2.7 2.037V4h2v7h-7V9h4.2q-.8-1.4-2.187-2.2Q13.625 6 12 6 9.5 6 7.75 7.75T6 12q0 2.5 1.75 4.25T12 18q1.925 0 3.475-1.1T17.65 14h2.1q-.7 2.65-2.85 4.325Q14.75 20 12 20Z'/></svg>"""

let view (model: PiModel) (dispatch: PiMsg -> unit) (accessories: Map<string, Client.AccessoryDetails>) = [
    match model.ActionSetting.AccessoryId with
    | Some uniqueId when not <| accessories.ContainsKey uniqueId ->
        Pi.message
            "caution"
            "red"
            "Configured device is no longer available on Homebridge. Please update Homebridge or Reset button config."

        Pi.button "Reset button config" (fun _ -> dispatch <| PiMsg.SelectAccessory None)
    | _ ->
        Pi.row "Accessory" [
            Pi.selectElement [
                prop.style [ style.marginRight 4 ]
                prop.value(model.ActionSetting.AccessoryId |> Option.defaultValue "DEFAULT")
                prop.children [
                    Html.option [ prop.value "DEFAULT" ]
                    for room in model.Layout do
                        Html.optgroup [
                            prop.custom("label", room.name)
                            prop.children(
                                room.services
                                |> Array.toList
                                |> List.choose(fun itemInfo ->
                                    Map.tryFind itemInfo.uniqueId accessories
                                    |> Option.map(fun accessoryDetails ->
                                        let name =
                                            itemInfo.customName
                                            |> Option.defaultValue accessoryDetails.serviceName

                                        name, accessoryDetails))
                                |> List.sortBy fst
                                |> List.map(fun (name, accessoryDetails) ->
                                    Html.option [
                                        prop.value accessoryDetails.uniqueId
                                        prop.disabled(accessoryDetails.serviceCharacteristics.Length = 0)
                                        prop.text name
                                    ])
                            )
                        ]
                ]
                prop.onChange(fun (value: string) ->
                    let msg = if value = "DEFAULT" then None else Some value
                    dispatch <| PiMsg.SelectAccessory msg)
            ]
            Pi.iconButton refreshIcon (fun () -> dispatch PiMsg.GetData)
        ]
]
