module StreamDeck.Homebridge.PiLogin

open System.Text.RegularExpressions
open Feliz
open StreamDeck.SDK.Components
open StreamDeck.Homebridge.PiModel

let view (model: PiModel) (dispatch: PiMsg -> unit) error = [
    if not <| System.String.IsNullOrEmpty error then
        Pi.message "caution" "red" error

    Pi.input "Server" [
        prop.value model.ServerInfo.Host
        prop.placeholder "e.g. http://192.168.68.65:8581"
        prop.required true
        prop.pattern(Regex "^(.*:)//([A-Za-z0-9\-\.]+)(:[0-9]+)?$")
        prop.onChange(fun (value: string) ->
            dispatch
            <| PiMsg.UpdateServerInfo {
                model.ServerInfo with
                    Host =
                        if value.Length > 10 then
                            value.TrimEnd [| '/' |]
                        else // we should not trim when user type only "http://"
                            value
            })
    ]

    Pi.input "UserName" [
        prop.value model.ServerInfo.UserName
        prop.required true
        prop.onChange(fun value ->
            dispatch
            <| PiMsg.UpdateServerInfo {
                model.ServerInfo with
                    UserName = value
            })
    ]

    Pi.input "Password" [
        prop.type' "password"
        prop.value model.ServerInfo.Password
        prop.required true
        prop.onChange(fun value ->
            let settings = {
                model.ServerInfo with
                    Password = value
            }

            dispatch <| PiMsg.UpdateServerInfo settings)
    ]

    Pi.select "Update" [
        prop.value model.ServerInfo.UpdateInterval
        prop.children [
            Html.option [ prop.value "0"; prop.text "Never" ]
            Html.option [ prop.value "1"; prop.text "Every second" ]
            Html.option [ prop.value "2"; prop.text "Every 2 seconds" ]
            Html.option [ prop.value "5"; prop.text "Every 5 seconds" ]
            Html.option [ prop.value "10"; prop.text "Every 10 seconds" ]
            Html.option [ prop.value "60"; prop.text "Every minute" ]
        ]
        prop.onChange(fun (value: string) ->
            {
                model.ServerInfo with
                    UpdateInterval = int value
            }
            |> PiMsg.UpdateServerInfo
            |> dispatch)
    ]

    Pi.button "Login" (fun _ -> dispatch <| PiMsg.Login true)
]
