module StreamDeck.Homebridge.PiAccessorySelector

open Feliz
open StreamDeck.SDK.Components
open StreamDeck.Homebridge.PiModel

let view (model: PiModel) (dispatch: PiMsg -> unit) (accessories: Map<string, Client.AccessoryDetails>) = [
    match model.ActionSetting.AccessoryId with
    | Some uniqueId when not <| accessories.ContainsKey uniqueId ->
        Pi.message
            "caution"
            "red"
            "Configured device is no longer available on Homebridge. Please update Homebridge or Reset button config."

        Pi.button "Reset button config" (fun _ -> dispatch <| PiMsg.SelectAccessory None)
    | _ ->
        Pi.select "Accessory" [
            prop.value(model.ActionSetting.AccessoryId |> Option.defaultValue "DEFAULT")
            prop.children [
                Html.option [ prop.value "DEFAULT" ]
                for room in model.Layout do
                    Html.optgroup [
                        prop.custom("label", room.name)
                        //prop.label room.name
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
                //let value = x.Value
                let msg = if value = "DEFAULT" then None else Some value
                dispatch <| PiMsg.SelectAccessory msg)
        ]
]
