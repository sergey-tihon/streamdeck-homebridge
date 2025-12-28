module StreamDeck.Homebridge.PiModel

open Elmish
open StreamDeck.Homebridge
open StreamDeck.SDK.Dto
open StreamDeck.SDK.PiModel

type PiModel = {
    IsDevMode: bool
    ReplyAgent: MailboxProcessor<PiCommand> option
    ServerInfo: Domain.GlobalSettings
    Client: Result<Client.HomebridgeClient, string>

    IsLoading: Result<bool, string>
    Accessories: Map<string, Client.AccessoryDetails>
    SwitchAccessories: Map<string, Client.AccessoryDetails>
    RangeAccessories: Map<string, Client.AccessoryDetails>
    Layout: Client.RoomLayout[]

    ActionType: string option
    ActionSetting: Domain.ActionSetting
}

[<RequireQualifiedAccess>]
type PiMsg =
    | PiConnected of startArgs: StartArgs * replyAgent: MailboxProcessor<PiCommand>
    | GlobalSettingsReceived of Domain.GlobalSettings
    | ActionSettingReceived of Domain.ActionSetting

    | UpdateServerInfo of Domain.GlobalSettings
    | Login of manual: bool
    | SetHomebridgeClient of Result<Client.HomebridgeClient, string>
    | Logout
    | GetData
    | SetData of Client.AccessoryDetails[] * Client.RoomLayout[]
    | ResetLoading of error: string
    | SelectActionType of actionType: string option
    | SelectAccessory of uniqueId: string option
    | SelectCharacteristic of characteristicType: string option
    | ChangeTargetValue of targetValue: float option
    | ChangeSpeed of speed: int option
    | EmitEvent of payload: int option

let init isDevMode =
    fun () ->
        let state = {
            IsDevMode = isDevMode
            ReplyAgent = None
            ServerInfo = {
                Host = "http://CHANGE_ME_HOMEBRIDGE_HOST:8581"
                UserName = "admin"
                Password = "admin"
                UpdateInterval = 3
            }
            Client = Error null
            IsLoading = Ok false
            Accessories = Map.empty
            SwitchAccessories = Map.empty
            RangeAccessories = Map.empty
            Layout = [||]
            ActionType = None
            ActionSetting = {
                AccessoryId = None
                CharacteristicType = None
                TargetValue = None
                Speed = None
            }
        }

        state, Cmd.none
