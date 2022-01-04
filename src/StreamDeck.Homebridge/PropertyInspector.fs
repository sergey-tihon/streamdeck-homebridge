module StreamDeck.Homebridge.PI

open Browser
open Elmish
open Fable.React
open Fable.React.Props

type PiModel =
    { 
        ServerInfo: Client.ServerInfo
        AuthInfo: Result<Client.AuthResult, string>
        Accessories: Map<string, Client.AccessoryDetails>
        Layout: Client.RoomLayout[]
        SelectedAccessoryId: string option
        CharacteristicType: string option
    }

type PiMsg =
    | UpdateServerInfo of Client.ServerInfo
    | Login
    | SetLoginResult of Result<Client.AuthResult,string>
    | Logout
    | GetData
    | SetData of Client.AccessoryDetails[] * Client.RoomLayout[]
    | SelectedAccessory of uniqueId: string
    | SelectedCharacteristic of characteristicType: string
    | ToggleCharacteristic
    | UpdateAccessory of accessory: Client.AccessoryDetails

let init () =
    let state = {
        ServerInfo = {
            Host = "http://192.168.0.213:8581"
            UserName = "admin"
            Password = "admin"
        }
        AuthInfo = Error null
        Accessories = Map.empty
        Layout = [||]
        SelectedAccessoryId = None
        CharacteristicType = None
    }
    state, Cmd.none //Cmd.ofMsg GetData

let filterBoolCharacteristics (accessory:Client.AccessoryDetails) =
    let characteristics =
        accessory.serviceCharacteristics
        |> Array.filter (fun x -> 
            x.canWrite // we can modify value
            && (x.format = "bool" // it is boolean
                || (x.format = "uint8" && x.minValue = Some 0 && x.maxValue = Some 1)) // or behave like boolean
        )
    { accessory with serviceCharacteristics = characteristics }

let getIntValue characteristicType (accessory:Client.AccessoryDetails) = 
    let characteristic = 
        accessory.serviceCharacteristics 
        |> Array.find (fun x -> x.``type`` = characteristicType)
    characteristic.value :?> int

let update (msg:PiMsg) (model:PiModel) =
    match msg with
    | UpdateServerInfo serverInfo ->
        { model with ServerInfo = serverInfo}, Cmd.none
    | Login ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            async {
                let! result = Client.authenticate model.ServerInfo
                dispatch <| SetLoginResult result
            } |> Async.StartImmediate
        model, Cmd.ofSub delayedCmd
    | SetLoginResult authInfo ->
        let cmd = 
            match authInfo with
            | Ok _ -> Cmd.ofMsg GetData
            | _ -> Cmd.none
        { model with AuthInfo = authInfo}, cmd
    | Logout ->
        { model with AuthInfo = Error null}, Cmd.none
    | GetData ->
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            async {
                match model.AuthInfo with 
                | Ok token ->
                    let! layout = Client.getAccessoriesLayout model.ServerInfo.Host token
                    let! data = Client.getAccessories model.ServerInfo.Host token
                    match data, layout with
                    | Some(data), Some(layout) ->
                        let accessories = 
                            data 
                            |> Array.choose (fun accessory ->
                                let accessory' = filterBoolCharacteristics accessory
                                if accessory'.serviceCharacteristics.Length > 0
                                then Some accessory' else None
                            )
                        dispatch <| SetData (accessories, layout)
                    | _ -> ()
                | _ -> ()
            } |> Async.StartImmediate
        model, Cmd.ofSub delayedCmd
    | SetData (accessories, layout) ->
        let state = { 
            model with
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
        let delayedCmd (dispatch: PiMsg -> unit) : unit =
            match model.AuthInfo, model.SelectedAccessoryId, model.CharacteristicType with
            | Ok(authInfo), Some(selectedAccessoryId), Some(characteristicType) ->
                async {
                    let! accessory = Client.getAccessory model.ServerInfo.Host authInfo selectedAccessoryId
                    let currentValue = accessory.Value |> getIntValue characteristicType
                    let targetValue = 1 - currentValue
                    let! accessory' = Client.setAccessoryCharacteristic model.ServerInfo.Host authInfo selectedAccessoryId characteristicType targetValue
                    dispatch <| UpdateAccessory (filterBoolCharacteristics accessory'.Value)
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
        match model.AuthInfo with
        | Error (error) ->
            div [Class "sdpi-item"; Type "field"] [
                div [Class "sdpi-item-label"] [str "Server"]
                input [
                    Class "sdpi-item-value"
                    Value model.ServerInfo.Host
                    Placeholder "e.g. http://192.168.0.213:8581"
                    Required true
                    Pattern "http://d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\:\\d{2,5}"
                    OnChange (fun x -> dispatch <| UpdateServerInfo { model.ServerInfo with Host = x.Value })
                ]
            ]
            div [Class "sdpi-item"; Type "field"] [
                div [Class "sdpi-item-label"] [str "UserName"]
                input [
                    Class "sdpi-item-value"
                    Value model.ServerInfo.UserName
                    Required true
                    OnChange (fun x -> dispatch <| UpdateServerInfo { model.ServerInfo with UserName = x.Value })
                ]
            ]
            div [Class "sdpi-item"; Type "password"] [
                div [Class "sdpi-item-label"] [str "Password"]
                input [
                    Class "sdpi-item-value"
                    Type "password"
                    Value model.ServerInfo.Password
                    Required true
                    OnChange (fun x -> dispatch <| UpdateServerInfo { model.ServerInfo with Password = x.Value })
                ]
            ]
            div [Class "sdpi-item"; Type "button"] [
                button [
                    Class "sdpi-item-value"; 
                    OnClick (fun _ -> dispatch <| Login)
                ] [ str "Login" ]
            ]
            if not <| System.String.IsNullOrEmpty(error) then
                div [Class "sdpi-item"; Type "button"] [
                    textarea [Type "textarea"; MaxLength 300] [str error]
                ]
        | Ok _ ->
            div [Class "sdpi-item"; Type "button"] [
                button [
                    Class "sdpi-item-value"; 
                    OnClick (fun _ -> dispatch <| Logout)
                ] [ str "Logout" ]
            ]
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
                                option [
                                    Value item.uniqueId
                                    Selected <| 
                                        match model.SelectedAccessoryId with
                                        | Some(id) when id = item.uniqueId -> true
                                        | _ -> false
                                ] [
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
                            option [
                                Value x.``type``
                                Selected <| 
                                    match model.CharacteristicType with
                                    | Some(ty) when ty = x.``type`` -> true
                                    | _ -> false
                            ] [
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