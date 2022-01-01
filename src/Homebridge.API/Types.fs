namespace rec Homebridge.API.Types

type AuthDto =
    { username: string
      password: string
      otp: Option<string> }
    ///Creates an instance of AuthDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (username: string, password: string): AuthDto =
        { username = username
          password = password
          otp = None }

type HomebridgeNetworkInterfacesDto =
    { adapters: list<string> }
    ///Creates an instance of HomebridgeNetworkInterfacesDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (adapters: list<string>): HomebridgeNetworkInterfacesDto = { adapters = adapters }

type HomebridgeMdnsSettingDto =
    { advertiser: string }
    ///Creates an instance of HomebridgeMdnsSettingDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (advertiser: string): HomebridgeMdnsSettingDto = { advertiser = advertiser }

type AccessorySetCharacteristicDto =
    { characteristicType: string
      value: string }
    ///Creates an instance of AccessorySetCharacteristicDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (characteristicType: string, value: string): AccessorySetCharacteristicDto =
        { characteristicType = characteristicType
          value = value }

type UserDto =
    { id: float
      name: string
      username: string
      admin: bool
      password: string
      otpActive: bool }
    ///Creates an instance of UserDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (id: float, name: string, username: string, admin: bool, password: string, otpActive: bool): UserDto =
        { id = id
          name = name
          username = username
          admin = admin
          password = password
          otpActive = otpActive }

type UserUpdatePasswordDto =
    { currentPassword: string
      newPassword: string }
    ///Creates an instance of UserUpdatePasswordDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (currentPassword: string, newPassword: string): UserUpdatePasswordDto =
        { currentPassword = currentPassword
          newPassword = newPassword }

type UserActivateOtpDto =
    { code: string }
    ///Creates an instance of UserActivateOtpDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (code: string): UserActivateOtpDto = { code = code }

type UserDeactivateOtpDto =
    { password: string }
    ///Creates an instance of UserDeactivateOtpDto with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (password: string): UserDeactivateOtpDto = { password = password }

type HbServiceStartupSettings =
    { HOMEBRIDGE_DEBUG: bool
      HOMEBRIDGE_KEEP_ORPHANS: bool
      HOMEBRIDGE_INSECURE: bool
      ENV_DEBUG: Option<string>
      ENV_NODE_OPTIONS: Option<string> }
    ///Creates an instance of HbServiceStartupSettings with all optional fields initialized to None. The required fields are parameters of this function
    static member Create (hOMEBRIDGE_DEBUG: bool, hOMEBRIDGE_KEEP_ORPHANS: bool, hOMEBRIDGE_INSECURE: bool): HbServiceStartupSettings =
        { HOMEBRIDGE_DEBUG = hOMEBRIDGE_DEBUG
          HOMEBRIDGE_KEEP_ORPHANS = hOMEBRIDGE_KEEP_ORPHANS
          HOMEBRIDGE_INSECURE = hOMEBRIDGE_INSECURE
          ENV_DEBUG = None
          ENV_NODE_OPTIONS = None }

[<RequireQualifiedAccess>]
type AuthControllerSignIn = | Created

[<RequireQualifiedAccess>]
type AuthControllerGetSettings = | OK

[<RequireQualifiedAccess>]
type AuthControllerGetToken = | Created

[<RequireQualifiedAccess>]
type AuthControllerCheckAuth = | OK

[<RequireQualifiedAccess>]
type ServerControllerRestartServer = | OK

[<RequireQualifiedAccess>]
type ServerControllerRestartChildBridge = | OK

[<RequireQualifiedAccess>]
type ServerControllerGetBridgePairingInformation = | OK

[<RequireQualifiedAccess>]
type ServerControllerGetQrCode = | OK

[<RequireQualifiedAccess>]
type ServerControllerResetHomebridgeAccessory = | OK

[<RequireQualifiedAccess>]
type ServerControllerResetCachedAccessories = | OK

[<RequireQualifiedAccess>]
type ServerControllerGetCachedAccessories = | OK

[<RequireQualifiedAccess>]
type ServerControllerDeleteCachedAccessory = | NoContent

[<RequireQualifiedAccess>]
type ServerControllerGetDevicePairings = | OK

[<RequireQualifiedAccess>]
type ServerControllerGetDevicePairingById = | OK

[<RequireQualifiedAccess>]
type ServerControllerDeleteDevicePairing = | NoContent

[<RequireQualifiedAccess>]
type ServerControllerLookupUnusedPort = | OK

[<RequireQualifiedAccess>]
type ServerControllerGetSystemNetworkInterfaces = | OK

[<RequireQualifiedAccess>]
type ServerControllerGetHomebridgeNetworkInterfaces = | OK

[<RequireQualifiedAccess>]
type ServerControllerSetHomebridgeNetworkInterfaces = | OK

[<RequireQualifiedAccess>]
type ServerControllerGetHomebridgeMdnsSetting = | OK

[<RequireQualifiedAccess>]
type ServerControllerSetHomebridgeMdnsSetting = | OK

[<RequireQualifiedAccess>]
type ConfigEditorControllerGetConfig = | OK

[<RequireQualifiedAccess>]
type ConfigEditorControllerUpdateConfig = | Created

[<RequireQualifiedAccess>]
type ConfigEditorControllerGetConfigForPlugin = | OK

[<RequireQualifiedAccess>]
type ConfigEditorControllerUpdateConfigForPlugin = | Created

[<RequireQualifiedAccess>]
type ConfigEditorControllerDisablePlugin = | OK

[<RequireQualifiedAccess>]
type ConfigEditorControllerEnablePlugin = | OK

[<RequireQualifiedAccess>]
type ConfigEditorControllerListConfigBackups = | OK

[<RequireQualifiedAccess>]
type ConfigEditorControllerDeleteAllConfigBackups = | OK

[<RequireQualifiedAccess>]
type ConfigEditorControllerGetBackup = | OK

[<RequireQualifiedAccess>]
type PluginsControllerPluginsGet = | OK

[<RequireQualifiedAccess>]
type PluginsControllerPluginsSearch = | OK

[<RequireQualifiedAccess>]
type PluginsControllerPluginLookup = | OK

[<RequireQualifiedAccess>]
type PluginsControllerGetAvailablePluginVersions = | OK

[<RequireQualifiedAccess>]
type PluginsControllerGetPluginConfigSchema = | OK

[<RequireQualifiedAccess>]
type PluginsControllerGetPluginChangeLog = | OK

[<RequireQualifiedAccess>]
type PluginsControllerGetPluginRelease = | OK

[<RequireQualifiedAccess>]
type PluginsControllerGetPluginAlias = | OK

[<RequireQualifiedAccess>]
type AccessoriesControllerGetAccessories = | OK

[<RequireQualifiedAccess>]
type AccessoriesControllerGetAccessoryLayout = | OK

[<RequireQualifiedAccess>]
type AccessoriesControllerGetAccessory = | OK

[<RequireQualifiedAccess>]
type AccessoriesControllerSetAccessoryCharacteristic = | OK

[<RequireQualifiedAccess>]
type HomebridgeHueControllerExchangeCredentials = | OK

[<RequireQualifiedAccess>]
type PluginsSettingsUiControllerServeCustomUiAsset = | OK

[<RequireQualifiedAccess>]
type UsersControllerGetUsers = OK of payload: list<UserDto>

[<RequireQualifiedAccess>]
type UsersControllerAddUser = Created of payload: UserDto

[<RequireQualifiedAccess>]
type UsersControllerUpdateUser = OK of payload: UserDto

[<RequireQualifiedAccess>]
type UsersControllerDeleteUser = | OK

[<RequireQualifiedAccess>]
type UsersControllerUpdateOwnPassword = | Created

[<RequireQualifiedAccess>]
type UsersControllerSetupOtp = | Created

[<RequireQualifiedAccess>]
type UsersControllerActivateOtp = | Created

[<RequireQualifiedAccess>]
type UsersControllerDeactivateOtp = | Created

[<RequireQualifiedAccess>]
type StatusControllerGetServerCpuInfo = | OK

[<RequireQualifiedAccess>]
type StatusControllerGetServerMemoryInfo = | OK

[<RequireQualifiedAccess>]
type StatusControllerGetServerUptimeInfo = | OK

[<RequireQualifiedAccess>]
type StatusControllerCheckHomebridgeStatus = | OK

[<RequireQualifiedAccess>]
type StatusControllerGetChildBridges = | OK

[<RequireQualifiedAccess>]
type StatusControllerGetHomebridgeVersion = | OK

[<RequireQualifiedAccess>]
type StatusControllerGetHomebridgeServerInfo = | OK

[<RequireQualifiedAccess>]
type StatusControllerGetNodeJsVersionInfo = | OK

[<RequireQualifiedAccess>]
type LinuxControllerRestartHost = | OK

[<RequireQualifiedAccess>]
type LinuxControllerShutdownHost = | OK

[<RequireQualifiedAccess>]
type DockerControllerGetStartupScript = | OK

[<RequireQualifiedAccess>]
type DockerControllerUpdateStartupScript = | OK

[<RequireQualifiedAccess>]
type DockerControllerRestartDockerContainer = | OK

[<RequireQualifiedAccess>]
type HbServiceControllerGetHomebridgeStartupSettings = | OK

[<RequireQualifiedAccess>]
type HbServiceControllerSetHomebridgeStartupSettings = | OK

[<RequireQualifiedAccess>]
type HbServiceControllerSetFullServiceRestartFlag = | OK

[<RequireQualifiedAccess>]
type HbServiceControllerDownloadLogFile = | OK

[<RequireQualifiedAccess>]
type HbServiceControllerTruncateLogFile = | OK

[<RequireQualifiedAccess>]
type BackupControllerDownloadBackup = | OK

[<RequireQualifiedAccess>]
type BackupControllerGetNextBackupTime = | OK

[<RequireQualifiedAccess>]
type BackupControllerListScheduledBackups = | OK

[<RequireQualifiedAccess>]
type BackupControllerGetScheduledBackup = | OK

[<RequireQualifiedAccess>]
type BackupControllerRestoreBackup = | Created

[<RequireQualifiedAccess>]
type BackupControllerRestoreHbfx = | Created

[<RequireQualifiedAccess>]
type BackupControllerPostBackupRestoreRestart = | OK
