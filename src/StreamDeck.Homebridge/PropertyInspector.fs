module StreamDeck.Homebridge.PI

open Browser
open Elmish
open Fable.React
open Fable.React.Props

type Model =
    { 
        AuthInfo: Client.AuthInfo option
        Accessories: Map<string, Client.AccessoryDetails>
        Layout: Client.RoomLayout[]
        SelectedAccessoryId: string option
        CharacteristicType: string option
    }

type Msg =
    | GetData
    | SetData of Client.AuthInfo * Client.AccessoryDetails[] * Client.RoomLayout[]
    | SelectedAccessory of uniqueId: string
    | SelectedCharacteristic of characteristicType: string
    | ToggleCharacteristic
    | UpdateAccessory of accessory: Client.AccessoryDetails

let init () =
    let state = { 
        AuthInfo = None
        Accessories = Map.empty
        Layout = [||]
        SelectedAccessoryId = None
        CharacteristicType = None
    }
    state, Cmd.ofMsg GetData

let filterBoolCharacteristics (accessory:Client.AccessoryDetails) =
    let characteristics =
        accessory.serviceCharacteristics
        |> Array.filter (fun x -> 
            x.canWrite // we can modify value
            && (x.format = "bool" // it is boolean
                || (x.format = "uint8" && x.minValue = Some 0 && x.maxValue = Some 1)) // or behave like boolean
        )
    { accessory with serviceCharacteristics = characteristics }

let update (msg:Msg) (model:Model) =
    match msg with
    | GetData ->
        let delayedCmd (dispatch: Msg -> unit) : unit =
            async {
                let! token = Client.authenticate "admin" "admin"
                let! layout = Client.getAccessoriesLayout token.Value
                let! data = Client.getAccessories token.Value
                match data, layout with
                | Some(data), Some(layout) ->
                    let accessories = 
                        data 
                        |> Array.choose (fun accessory ->
                            let accessory' = filterBoolCharacteristics accessory
                            if accessory'.serviceCharacteristics.Length > 0
                            then Some accessory' else None
                        )
                    dispatch <| SetData (token.Value, accessories, layout)
                | _ -> ()
            } |> Async.StartImmediate
        model, Cmd.ofSub delayedCmd
    | SetData (authInfo, accessories, layout) ->
        let state = { 
            model with
                AuthInfo = Some authInfo
                Accessories = accessories |> Array.map (fun x-> x.uniqueId,x) |> Map.ofArray
                Layout = layout
        }
        state, Cmd.none
    | SelectedAccessory uniqueId ->
        let state = { 
            model with 
                SelectedAccessoryId = Some uniqueId 
                CharacteristicType = None
        }
        state, Cmd.none
    | SelectedCharacteristic characteristicType ->
        { model with CharacteristicType = Some characteristicType }, Cmd.none
    | ToggleCharacteristic ->
        let delayedCmd (dispatch: Msg -> unit) : unit =
            match model.AuthInfo, model.SelectedAccessoryId, model.CharacteristicType with
            | Some(authInfo), Some(selectedAccessoryId), Some(characteristicType) ->
                let accessory = model.Accessories |> Map.find selectedAccessoryId
                let characteristic = 
                    accessory.serviceCharacteristics 
                    |> Array.find (fun x -> x.``type`` = characteristicType)
                let targetValue = 1 - (characteristic.value :?> int)
                async {
                    let! accessory = Client.setAccessoryCharacteristic authInfo selectedAccessoryId characteristicType targetValue
                    let accessory' = filterBoolCharacteristics accessory.Value
                    dispatch <| UpdateAccessory accessory'
                } |> Async.StartImmediate
            | _ -> ()
        model, Cmd.ofSub delayedCmd
    | UpdateAccessory accessory ->
        let accessories' =
            model.Accessories
            |> Map.remove accessory.uniqueId
            |> Map.add accessory.uniqueId accessory
        { model with Accessories = accessories' }, Cmd.none

let view model dispatch =
    div [Class "sdpi-wrapper"] [

        div [Class "sdpi-item"] [
            div [Class "sdpi-item-label"] [str "Accessory"]
            select [
                Class "sdpi-item-value select"
                OnChange (fun x -> dispatch <| SelectedAccessory x.Value)
            ] [
                option [] []
                for room in model.Layout do
                    optgroup [Label room.name] [
                        yield! room.services
                        |> Array.choose (fun x -> Map.tryFind x.uniqueId model.Accessories)
                        |> Array.sortBy (fun x -> x.serviceName)
                        |> Array.map (fun item -> 
                            option [Value item.uniqueId] [
                                str <| sprintf "%s - %s" item.humanType item.serviceName
                            ]
                        )
                    ]
            ]
        ]

        match model.SelectedAccessoryId with
        | Some(uniqueId) ->
            let accessory = model.Accessories |> Map.find uniqueId
            let ai = accessory.accessoryInformation
            div [Class "sdpi-item"] [
                div [Class "sdpi-item-label"] [str "Accessory Information"]
                table [Class "sdpi-item-value no-select"] [
                    tbody [] [
                        tr [] [
                            td [] [str "Manufacturer"]
                            td [] [str ai.Manufacturer]
                        ]
                        tr [] [
                            td [] [str "Model"]
                            td [] [str ai.Model]
                        ]
                        tr [] [
                            td [] [str "Name"]
                            td [] [str ai.Name]
                        ]
                    ]
                ]
            ]
            div [Class "sdpi-item"] [
                div [Class "sdpi-item-label"] [str "Characteristic"]
                select [
                    Class "sdpi-item-value select"
                    OnChange (fun x -> dispatch <| SelectedCharacteristic x.Value)
                ] [
                    option [] []
                    let characteristics = accessory.serviceCharacteristics |> Array.sortBy (fun x -> x.``type``)
                    for x in characteristics do
                        option [Value x.``type``] [
                            str x.description
                        ]
                ]
            ]
            match model.CharacteristicType with
            | Some _ ->
                div [Class "sdpi-item"] [
                    button [
                        Class "sdpi-item-value"; 
                        OnClick (fun _ -> dispatch <| ToggleCharacteristic)
                    ] [
                        str "Test configuration (Apply)"
                    ]
                ]
            | None -> ()
        | None -> ()
    ]