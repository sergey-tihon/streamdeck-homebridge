namespace rec Homebridge.API

open Browser.Types
open Fable.SimpleHttp
open Homebridge.API.Types
open Homebridge.API.Http

type HomebridgeAPIClient(url: string, headers: list<Header>) =
    new(url: string) = HomebridgeAPIClient(url, [])

    ///<summary>
    ///Exchange a username and password for an authentication token.
    ///</summary>
    member this.AuthControllerSignIn(body: AuthDto) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]
            let! (status, content) = OpenApiHttp.postAsync url "/api/auth/login" headers requestParts
            return AuthControllerSignIn.Created
        }

    ///<summary>
    ///Return settings required to load the UI before authentication.
    ///</summary>
    member this.AuthControllerGetSettings() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/auth/settings" headers requestParts
            return AuthControllerGetSettings.OK
        }

    ///<summary>
    ///This method can be used to obtain an access token ONLY when authentication has been disabled.
    ///</summary>
    member this.AuthControllerGetToken() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.postAsync url "/api/auth/noauth" headers requestParts
            return AuthControllerGetToken.Created
        }

    ///<summary>
    ///Check to see if an authentication token is still valid.
    ///</summary>
    member this.AuthControllerCheckAuth() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/auth/check" headers requestParts
            return AuthControllerCheckAuth.OK
        }

    ///<summary>
    ///Restart the Homebridge instance.
    ///</summary>
    member this.ServerControllerRestartServer() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.putAsync url "/api/server/restart" headers requestParts
            return ServerControllerRestartServer.OK
        }

    ///<summary>
    ///This method is only supported on setups running hb-service.
    ///</summary>
    member this.ServerControllerRestartChildBridge(deviceId: string) =
        async {
            let requestParts =
                [ RequestPart.path ("deviceId", deviceId) ]

            let! (status, content) = OpenApiHttp.putAsync url "/api/server/restart/{deviceId}" headers requestParts
            return ServerControllerRestartChildBridge.OK
        }

    ///<summary>
    ///Get the Homebridge HomeKit pairing information and status.
    ///</summary>
    member this.ServerControllerGetBridgePairingInformation() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/server/pairing" headers requestParts
            return ServerControllerGetBridgePairingInformation.OK
        }

    ///<summary>
    ///Return the pairing QR code as an SVG.
    ///</summary>
    member this.ServerControllerGetQrCode() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/server/qrcode.svg" headers requestParts
            return ServerControllerGetQrCode.OK
        }

    ///<summary>
    ///Unpair / Reset the Homebridge instance and remove cached accessories.
    ///</summary>
    member this.ServerControllerResetHomebridgeAccessory() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/server/reset-homebridge-accessory" headers requestParts

            return ServerControllerResetHomebridgeAccessory.OK
        }

    ///<summary>
    ///Remove Homebridge cached accessories (hb-service only).
    ///</summary>
    member this.ServerControllerResetCachedAccessories() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/server/reset-cached-accessories" headers requestParts

            return ServerControllerResetCachedAccessories.OK
        }

    ///<summary>
    ///List cached Homebridge accessories.
    ///</summary>
    member this.ServerControllerGetCachedAccessories() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/server/cached-accessories" headers requestParts
            return ServerControllerGetCachedAccessories.OK
        }

    ///<summary>
    ///Remove a single Homebridge cached accessory (hb-service only).
    ///</summary>
    member this.ServerControllerDeleteCachedAccessory(uuid: string, cacheFile: string) =
        async {
            let requestParts =
                [ RequestPart.path ("uuid", uuid)
                  RequestPart.query ("cacheFile", cacheFile) ]

            let! (status, content) =
                OpenApiHttp.deleteAsync url "/api/server/cached-accessories/{uuid}" headers requestParts

            return ServerControllerDeleteCachedAccessory.NoContent
        }

    ///<summary>
    ///List all paired accessories (main bridge, external cameras, TVs etc).
    ///</summary>
    member this.ServerControllerGetDevicePairings() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/server/pairings" headers requestParts
            return ServerControllerGetDevicePairings.OK
        }

    ///<summary>
    ///Get a single device pairing
    ///</summary>
    member this.ServerControllerGetDevicePairingById(deviceId: string) =
        async {
            let requestParts =
                [ RequestPart.path ("deviceId", deviceId) ]

            let! (status, content) = OpenApiHttp.getAsync url "/api/server/pairings/{deviceId}" headers requestParts
            return ServerControllerGetDevicePairingById.OK
        }

    ///<summary>
    ///Remove a single paired accessory (hb-service only).
    ///</summary>
    member this.ServerControllerDeleteDevicePairing(deviceId: string) =
        async {
            let requestParts =
                [ RequestPart.path ("deviceId", deviceId) ]

            let! (status, content) = OpenApiHttp.deleteAsync url "/api/server/pairings/{deviceId}" headers requestParts
            return ServerControllerDeleteDevicePairing.NoContent
        }

    ///<summary>
    ///Return a random, unused port.
    ///</summary>
    member this.ServerControllerLookupUnusedPort() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/server/port/new" headers requestParts
            return ServerControllerLookupUnusedPort.OK
        }

    ///<summary>
    ///Return a list of available network interfaces on the server.
    ///</summary>
    member this.ServerControllerGetSystemNetworkInterfaces() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/server/network-interfaces/system" headers requestParts

            return ServerControllerGetSystemNetworkInterfaces.OK
        }

    ///<summary>
    ///Return a list of the network interface names assigned to Homebridge.
    ///</summary>
    member this.ServerControllerGetHomebridgeNetworkInterfaces() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/server/network-interfaces/bridge" headers requestParts

            return ServerControllerGetHomebridgeNetworkInterfaces.OK
        }

    ///<summary>
    ///Set a list of the network interface names assigned to Homebridge.
    ///</summary>
    member this.ServerControllerSetHomebridgeNetworkInterfaces(body: HomebridgeNetworkInterfacesDto) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/server/network-interfaces/bridge" headers requestParts

            return ServerControllerSetHomebridgeNetworkInterfaces.OK
        }

    ///<summary>
    ///Return the current mdns advertiser settings.
    ///</summary>
    member this.ServerControllerGetHomebridgeMdnsSetting() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/server/mdns-advertiser" headers requestParts
            return ServerControllerGetHomebridgeMdnsSetting.OK
        }

    ///<summary>
    ///Set the mdns advertiser settings.
    ///</summary>
    member this.ServerControllerSetHomebridgeMdnsSetting(body: HomebridgeMdnsSettingDto) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]
            let! (status, content) = OpenApiHttp.putAsync url "/api/server/mdns-advertiser" headers requestParts
            return ServerControllerSetHomebridgeMdnsSetting.OK
        }

    ///<summary>
    ///Return the current Homebridge config.json file.
    ///</summary>
    member this.ConfigEditorControllerGetConfig() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/config-editor" headers requestParts
            return ConfigEditorControllerGetConfig.OK
        }

    ///<summary>
    ///Update the Homebridge config.json file.
    ///</summary>
    member this.ConfigEditorControllerUpdateConfig() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.postAsync url "/api/config-editor" headers requestParts
            return ConfigEditorControllerUpdateConfig.Created
        }

    ///<summary>
    ///An array of config blocks will be returned. An empty array will be returned if the plugin is not configured.
    ///</summary>
    member this.ConfigEditorControllerGetConfigForPlugin(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/config-editor/plugin/{pluginName}" headers requestParts

            return ConfigEditorControllerGetConfigForPlugin.OK
        }

    ///<summary>
    ///An array of all config blocks for the plugin must be provided, missing blocks will be removed. Sending an empty array will remove all plugin config.
    ///</summary>
    member this.ConfigEditorControllerUpdateConfigForPlugin(pluginName: string, body: list<obj>) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName)
                  RequestPart.jsonContent body ]

            let! (status, content) =
                OpenApiHttp.postAsync url "/api/config-editor/plugin/{pluginName}" headers requestParts

            return ConfigEditorControllerUpdateConfigForPlugin.Created
        }

    ///<summary>
    ///Mark the plugin as disabled.
    ///</summary>
    member this.ConfigEditorControllerDisablePlugin(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/config-editor/plugin/{pluginName}/disable" headers requestParts

            return ConfigEditorControllerDisablePlugin.OK
        }

    ///<summary>
    ///Mark the plugin as enabled.
    ///</summary>
    member this.ConfigEditorControllerEnablePlugin(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/config-editor/plugin/{pluginName}/enable" headers requestParts

            return ConfigEditorControllerEnablePlugin.OK
        }

    ///<summary>
    ///List the available Homebridge config.json backups.
    ///</summary>
    member this.ConfigEditorControllerListConfigBackups() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/config-editor/backups" headers requestParts
            return ConfigEditorControllerListConfigBackups.OK
        }

    ///<summary>
    ///Delete all the Homebridge config.json backups.
    ///</summary>
    member this.ConfigEditorControllerDeleteAllConfigBackups() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.deleteAsync url "/api/config-editor/backups" headers requestParts
            return ConfigEditorControllerDeleteAllConfigBackups.OK
        }

    ///<summary>
    ///Return the Homebridge config.json file for the given backup ID.
    ///</summary>
    member this.ConfigEditorControllerGetBackup(backupId: float) =
        async {
            let requestParts =
                [ RequestPart.path ("backupId", backupId) ]

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/config-editor/backups/{backupId}" headers requestParts

            return ConfigEditorControllerGetBackup.OK
        }

    ///<summary>
    ///List of currently installed Homebridge plugins.
    ///</summary>
    member this.PluginsControllerPluginsGet() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/plugins" headers requestParts
            return PluginsControllerPluginsGet.OK
        }

    ///<summary>
    ///Search the NPM registry for Homebridge plugins.
    ///</summary>
    member this.PluginsControllerPluginsSearch(query: string) =
        async {
            let requestParts = [ RequestPart.path ("query", query) ]
            let! (status, content) = OpenApiHttp.getAsync url "/api/plugins/search/{query}" headers requestParts
            return PluginsControllerPluginsSearch.OK
        }

    ///<summary>
    ///Lookup a single plugin from the NPM registry.
    ///</summary>
    member this.PluginsControllerPluginLookup(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) = OpenApiHttp.getAsync url "/api/plugins/lookup/{pluginName}" headers requestParts
            return PluginsControllerPluginLookup.OK
        }

    ///<summary>
    ///Get the available versions and tags for a single plugin from the NPM registry.
    ///</summary>
    member this.PluginsControllerGetAvailablePluginVersions(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/plugins/lookup/{pluginName}/versions" headers requestParts

            return PluginsControllerGetAvailablePluginVersions.OK
        }

    ///<summary>
    ///Get the config.schema.json for a plugin.
    ///</summary>
    member this.PluginsControllerGetPluginConfigSchema(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/plugins/config-schema/{pluginName}" headers requestParts

            return PluginsControllerGetPluginConfigSchema.OK
        }

    ///<summary>
    ///Get the CHANGELOG.md (post install) for a plugin.
    ///</summary>
    member this.PluginsControllerGetPluginChangeLog(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) = OpenApiHttp.getAsync url "/api/plugins/changelog/{pluginName}" headers requestParts
            return PluginsControllerGetPluginChangeLog.OK
        }

    ///<summary>
    ///Get the latest GitHub release notes for a plugin.
    ///</summary>
    member this.PluginsControllerGetPluginRelease(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) = OpenApiHttp.getAsync url "/api/plugins/release/{pluginName}" headers requestParts
            return PluginsControllerGetPluginRelease.OK
        }

    ///<summary>
    ///**Warning**: pluginAlias and pluginType will be `null` if the type or alias could not be resolved.
    ///</summary>
    member this.PluginsControllerGetPluginAlias(pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) = OpenApiHttp.getAsync url "/api/plugins/alias/{pluginName}" headers requestParts
            return PluginsControllerGetPluginAlias.OK
        }

    ///<summary>
    ///Homebridge must be running in "insecure" mode to access the accessory list.
    ///</summary>
    member this.AccessoriesControllerGetAccessories() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/accessories" headers requestParts
            return AccessoriesControllerGetAccessories.OK
        }

    ///<summary>
    ///Get the accessory and room layout for the authenticating user.
    ///</summary>
    member this.AccessoriesControllerGetAccessoryLayout() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/accessories/layout" headers requestParts
            return AccessoriesControllerGetAccessoryLayout.OK
        }

    ///<summary>
    ///Get the "uniqueId" from the GET /api/accessories method.
    ///</summary>
    member this.AccessoriesControllerGetAccessory(uniqueId: string) =
        async {
            let requestParts =
                [ RequestPart.path ("uniqueId", uniqueId) ]

            let! (status, content) = OpenApiHttp.getAsync url "/api/accessories/{uniqueId}" headers requestParts
            return AccessoriesControllerGetAccessory.OK
        }

    ///<summary>
    ///Get the "uniqueId" and "characteristicType" values from the GET /api/accessories method.
    ///</summary>
    member this.AccessoriesControllerSetAccessoryCharacteristic(uniqueId: string, body: AccessorySetCharacteristicDto) =
        async {
            let requestParts =
                [ RequestPart.path ("uniqueId", uniqueId)
                  RequestPart.jsonContent body ]

            let! (status, content) = OpenApiHttp.putAsync url "/api/accessories/{uniqueId}" headers requestParts
            return AccessoriesControllerSetAccessoryCharacteristic.OK
        }

    member this.HomebridgeHueControllerExchangeCredentials() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/plugins/custom-plugins/homebridge-hue/dump-file" headers requestParts

            return HomebridgeHueControllerExchangeCredentials.OK
        }

    ///<summary>
    ///Returns the HTML assets for a plugin's custom UI
    ///</summary>
    member this.PluginsSettingsUiControllerServeCustomUiAsset(origin: string, pluginName: string) =
        async {
            let requestParts =
                [ RequestPart.query ("origin", origin)
                  RequestPart.path ("pluginName", pluginName) ]

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/plugins/settings-ui/{pluginName}/*" headers requestParts

            return PluginsSettingsUiControllerServeCustomUiAsset.OK
        }

    ///<summary>
    ///List of existing users.
    ///</summary>
    member this.UsersControllerGetUsers() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/users" headers requestParts
            return UsersControllerGetUsers.OK(Serializer.deserialize content)
        }

    ///<summary>
    ///Create a new user.
    ///</summary>
    member this.UsersControllerAddUser(body: UserDto) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]
            let! (status, content) = OpenApiHttp.postAsync url "/api/users" headers requestParts
            return UsersControllerAddUser.Created(Serializer.deserialize content)
        }

    ///<summary>
    ///Update a user.
    ///</summary>
    member this.UsersControllerUpdateUser(userId: float, body: UserDto) =
        async {
            let requestParts =
                [ RequestPart.path ("userId", userId)
                  RequestPart.jsonContent body ]

            let! (status, content) = OpenApiHttp.patchAsync url "/api/users/{userId}" headers requestParts
            return UsersControllerUpdateUser.OK(Serializer.deserialize content)
        }

    ///<summary>
    ///Delete a user.
    ///</summary>
    member this.UsersControllerDeleteUser(userId: float) =
        async {
            let requestParts = [ RequestPart.path ("userId", userId) ]
            let! (status, content) = OpenApiHttp.deleteAsync url "/api/users/{userId}" headers requestParts
            return UsersControllerDeleteUser.OK
        }

    ///<summary>
    ///Update the password for the current user.
    ///</summary>
    member this.UsersControllerUpdateOwnPassword(body: UserUpdatePasswordDto) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]
            let! (status, content) = OpenApiHttp.postAsync url "/api/users/change-password" headers requestParts
            return UsersControllerUpdateOwnPassword.Created
        }

    ///<summary>
    ///Start 2FA setup for the current user.
    ///</summary>
    member this.UsersControllerSetupOtp() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.postAsync url "/api/users/otp/setup" headers requestParts
            return UsersControllerSetupOtp.Created
        }

    ///<summary>
    ///Activate 2FA setup for the current user.
    ///</summary>
    member this.UsersControllerActivateOtp(body: UserActivateOtpDto) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]
            let! (status, content) = OpenApiHttp.postAsync url "/api/users/otp/activate" headers requestParts
            return UsersControllerActivateOtp.Created
        }

    ///<summary>
    ///Deactivate 2FA setup for the current user.
    ///</summary>
    member this.UsersControllerDeactivateOtp(body: UserDeactivateOtpDto) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]
            let! (status, content) = OpenApiHttp.postAsync url "/api/users/otp/deactivate" headers requestParts
            return UsersControllerDeactivateOtp.Created
        }

    ///<summary>
    ///Return the current CPU load, load history and temperature (if available).
    ///</summary>
    member this.StatusControllerGetServerCpuInfo() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/status/cpu" headers requestParts
            return StatusControllerGetServerCpuInfo.OK
        }

    ///<summary>
    ///Return total memory, memory usage, and memory usage history in bytes.
    ///</summary>
    member this.StatusControllerGetServerMemoryInfo() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/status/ram" headers requestParts
            return StatusControllerGetServerMemoryInfo.OK
        }

    ///<summary>
    ///Return the host and process (UI) uptime.
    ///</summary>
    member this.StatusControllerGetServerUptimeInfo() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/status/uptime" headers requestParts
            return StatusControllerGetServerUptimeInfo.OK
        }

    ///<summary>
    ///Possible Homebridge statuses are `up`, `pending` or `down`.
    ///</summary>
    member this.StatusControllerCheckHomebridgeStatus() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/status/homebridge" headers requestParts
            return StatusControllerCheckHomebridgeStatus.OK
        }

    ///<summary>
    ///This method is only available when running `hb-service`.
    ///</summary>
    member this.StatusControllerGetChildBridges() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/status/homebridge/child-bridges" headers requestParts

            return StatusControllerGetChildBridges.OK
        }

    ///<summary>
    ///Return the current Homebridge version / package information.
    ///</summary>
    member this.StatusControllerGetHomebridgeVersion() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/status/homebridge-version" headers requestParts
            return StatusControllerGetHomebridgeVersion.OK
        }

    ///<summary>
    ///Return general information about the host environment.
    ///</summary>
    member this.StatusControllerGetHomebridgeServerInfo() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/status/server-information" headers requestParts
            return StatusControllerGetHomebridgeServerInfo.OK
        }

    ///<summary>
    ///Return current Node.js version and update availability information.
    ///</summary>
    member this.StatusControllerGetNodeJsVersionInfo() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/status/nodejs" headers requestParts
            return StatusControllerGetNodeJsVersionInfo.OK
        }

    ///<summary>
    ///Restart / reboot the host server.
    ///</summary>
    member this.LinuxControllerRestartHost() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/platform-tools/linux/restart-host" headers requestParts

            return LinuxControllerRestartHost.OK
        }

    ///<summary>
    ///Shutdown / power off the host server.
    ///</summary>
    member this.LinuxControllerShutdownHost() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/platform-tools/linux/shutdown-host" headers requestParts

            return LinuxControllerShutdownHost.OK
        }

    ///<summary>
    ///Return the oznu/homebridge docker image startup.sh file contents.
    ///</summary>
    member this.DockerControllerGetStartupScript() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/platform-tools/docker/startup-script" headers requestParts

            return DockerControllerGetStartupScript.OK
        }

    ///<summary>
    ///Update the oznu/homebridge docker image startup.sh file contents.
    ///</summary>
    member this.DockerControllerUpdateStartupScript() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/platform-tools/docker/startup-script" headers requestParts

            return DockerControllerUpdateStartupScript.OK
        }

    ///<summary>
    ///Restart the oznu/homebridge docker image container.
    ///</summary>
    member this.DockerControllerRestartDockerContainer() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/platform-tools/docker/restart-container" headers requestParts

            return DockerControllerRestartDockerContainer.OK
        }

    ///<summary>
    ///Return the startup flags and env variables for Homebridge.
    ///</summary>
    member this.HbServiceControllerGetHomebridgeStartupSettings() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.getAsync
                    url
                    "/api/platform-tools/hb-service/homebridge-startup-settings"
                    headers
                    requestParts

            return HbServiceControllerGetHomebridgeStartupSettings.OK
        }

    ///<summary>
    ///Update the startup flags and env variables for Homebridge.
    ///</summary>
    member this.HbServiceControllerSetHomebridgeStartupSettings(body: HbServiceStartupSettings) =
        async {
            let requestParts = [ RequestPart.jsonContent body ]

            let! (status, content) =
                OpenApiHttp.putAsync
                    url
                    "/api/platform-tools/hb-service/homebridge-startup-settings"
                    headers
                    requestParts

            return HbServiceControllerSetHomebridgeStartupSettings.OK
        }

    ///<summary>
    ///When running under hb-service the UI will only restart if it detects it needs to.
    ///</summary>
    member this.HbServiceControllerSetFullServiceRestartFlag() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync
                    url
                    "/api/platform-tools/hb-service/set-full-service-restart-flag"
                    headers
                    requestParts

            return HbServiceControllerSetFullServiceRestartFlag.OK
        }

    ///<summary>
    ///Download the entire log file.
    ///</summary>
    member this.HbServiceControllerDownloadLogFile(?colour: string) =
        async {
            let requestParts =
                [ if colour.IsSome then
                      RequestPart.query ("colour", colour.Value) ]

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/platform-tools/hb-service/log/download" headers requestParts

            return HbServiceControllerDownloadLogFile.OK
        }

    ///<summary>
    ///Truncate / empty the log file.
    ///</summary>
    member this.HbServiceControllerTruncateLogFile() =
        async {
            let requestParts = []

            let! (status, content) =
                OpenApiHttp.putAsync url "/api/platform-tools/hb-service/log/truncate" headers requestParts

            return HbServiceControllerTruncateLogFile.OK
        }

    ///<summary>
    ///Download a .tar.gz of the Homebridge instance.
    ///</summary>
    member this.BackupControllerDownloadBackup() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/backup/download" headers requestParts
            return BackupControllerDownloadBackup.OK
        }

    ///<summary>
    ///Return the date and time of the next scheduled backup.
    ///</summary>
    member this.BackupControllerGetNextBackupTime() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/backup/scheduled-backups/next" headers requestParts
            return BackupControllerGetNextBackupTime.OK
        }

    ///<summary>
    ///List available system generated instance backups.
    ///</summary>
    member this.BackupControllerListScheduledBackups() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.getAsync url "/api/backup/scheduled-backups" headers requestParts
            return BackupControllerListScheduledBackups.OK
        }

    ///<summary>
    ///Download a system generated instance backup.
    ///</summary>
    member this.BackupControllerGetScheduledBackup(backupId: string) =
        async {
            let requestParts =
                [ RequestPart.path ("backupId", backupId) ]

            let! (status, content) =
                OpenApiHttp.getAsync url "/api/backup/scheduled-backups/{backupId}" headers requestParts

            return BackupControllerGetScheduledBackup.OK
        }

    ///<summary>
    ///NOTE: This endpoint does not trigger the restore process.
    ///</summary>
    member this.BackupControllerRestoreBackup(?file: string) =
        async {
            let requestParts =
                [ if file.IsSome then
                      RequestPart.multipartFormData ("file", file.Value) ]

            let! (status, content) = OpenApiHttp.postAsync url "/api/backup/restore" headers requestParts
            return BackupControllerRestoreBackup.Created
        }

    ///<summary>
    ///NOTE: This endpoint does not trigger the restore process.
    ///</summary>
    member this.BackupControllerRestoreHbfx(?file: string) =
        async {
            let requestParts =
                [ if file.IsSome then
                      RequestPart.multipartFormData ("file", file.Value) ]

            let! (status, content) = OpenApiHttp.postAsync url "/api/backup/restore/hbfx" headers requestParts
            return BackupControllerRestoreHbfx.Created
        }

    ///<summary>
    ///Trigger a hard restart of Homebridge (use after restoring backup).
    ///</summary>
    member this.BackupControllerPostBackupRestoreRestart() =
        async {
            let requestParts = []
            let! (status, content) = OpenApiHttp.putAsync url "/api/backup/restart" headers requestParts
            return BackupControllerPostBackupRestoreRestart.OK
        }
