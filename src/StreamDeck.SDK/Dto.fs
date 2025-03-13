module StreamDeck.SDK.Dto

type ApplicationInfo = {
    /// A json object containing information about the application.
    application: ApplicationData
    /// A json object containing information about the plugin.
    plugin: PluginInfo
    /// Pixel ratio value to indicate if the Stream Deck application is running on a HiDPI screen.
    devicePixelRatio: int
    /// A json object containing information about the preferred user colors.
    colors: ColorInfo
    /// A json array containing information about the devices.
    devices: DeviceInfo[]
}

and ApplicationData = {
    font: string
    /// In which language the Stream Deck application is running. Possible values are en, `fr`, `de`, `es`, `ja`, `zh_CN`.
    language: string
    /// On which platform the Stream Deck application is running. Possible values are `kESDSDKApplicationInfoPlatformMac` ("mac") and `kESDSDKApplicationInfoPlatformWindows` ("windows").
    platform: string
    /// The operating system version.
    platformVersion: string
    /// The Stream Deck application version.
    version: string
}

and PluginInfo = {
    /// The unique identifier of the plugin.
    uuid: string
    /// The plugin version as written in the manifest.json.
    version: string
}

and ColorInfo = {
    buttonPressedBackgroundColor: string
    buttonPressedBorderColor: string
    buttonPressedTextColor: string
    disabledColor: string
    highlightColor: string
    mouseDownColor: string
}

and DeviceInfo = {
    /// An opaque value identifying the device.
    id: string option
    /// The name of the device set by the user.
    name: string
    /// The number of columns and rows of keys that the device owns.
    size: DeviceSize
    /// Type of device. Possible values are `kESDSDKDeviceType_StreamDeck` (0), `kESDSDKDeviceType_StreamDeckMini` (1), `kESDSDKDeviceType_StreamDeckXL` (2), `kESDSDKDeviceType_StreamDeckMobile` (3) and `kESDSDKDeviceType_CorsairGKeys` (4). This parameter parameter won't be present if you never plugged a device to the computer.
    ``type``: int
}

and DeviceSize = { columns: int; rows: int }


type ActionInfo = {
    /// The action's unique identifier. If your plugin supports multiple actions, you should use this value to see which action was triggered.
    action: string
    /// An opaque value identifying the instance's action. You will need to pass this opaque value to several APIs like the setTitle API.
    context: string
    /// An opaque value identifying the device.
    device: string
    /// A json object
    payload: ActionInfoPayload
}

and ActionInfoPayload = {
    /// This json object contains data that you can set and are stored persistently.
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates
}

and Coordinates = { column: int; row: int }


type StartArgs = {
    /// The socket's port to communicate with StreamDeck software.
    Port: string
    /// A unique identifier, which StreamDeck uses to communicate with the plugin.
    UUID: string
    /// Identifies, if the event is meant for the property inspector or the plugin.
    MessageType: string
    /// Information about the host (StreamDeck) application.
    ApplicationInfo: ApplicationInfo
    /// A json object containing information about the action. Available only for Property Inspector
    ActionInfo: ActionInfo option
}


type Event = {
    /// The action unique identifier. If your plugin supports multiple actions, you should use this value to see which action was triggered.
    action: string
    /// Event name.
    event: string
    /// An opaque value identifying the instance's action. You will need to pass this opaque value to several APIs like the setTitle API.
    context: string
    /// An opaque value identifying the device.
    device: string
    /// A json object.
    payload: obj
}

type ActionPayload = {
    /// This json object contains persistently stored data.
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates
    /// This is a parameter that is only set when the action has multiple states defined in its manifest.json. The 0-based value contains the current state of the action.
    state: int option
    /// This is a parameter that is only set when the action is triggered with a specific value from a Multi Action. For example if the user sets the Game Capture Record action to be disabled in a Multi Action, you would see the value 1. Only the value 0 and 1 are valid.
    userDesiredState: int option
    /// Boolean indicating if the action is inside a Multi Action.
    isInMultiAction: bool
}

type AppearanceActionPayload = {
    /// This json object contains persistently stored data.
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates
    /// This is a parameter that is only set when the action has multiple states defined in its manifest.json. The 0-based value contains the current state of the action.
    state: int option
    /// Boolean indicating if the action is inside a Multi Action.
    isInMultiAction: bool
    /// The string holds the name of the controller of the current action. Values include "Keypad" and "Encoder".
    controller: string
}

type ActionTitlePayload = {
    /// This json object contains data that you can set and is stored persistently
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates
    /// This value indicates for which state of the action the title or title parameters have been changed.
    state: int
    /// The new title.
    title: string
    /// A json object describing the new title parameters.
    titleParameters: ActionTitleParameters
}

and ActionTitleParameters = {
    /// The font family for the title.
    fontFamily: string
    /// The font size for the title.
    fontSize: int
    /// The font style for the title.
    fontStyle: string
    /// Boolean indicating an underline under the title.
    fontUnderline: bool
    /// Boolean indicating if the title is visible.
    showTitle: bool
    /// Vertical alignment of the title. Possible values are "top", "bottom" and "middle".
    titleAlignment: string
    /// Title color.
    titleColor: string
}

type ApplicationEvent = {
    event: string
    payload: ApplicationPayload
}

and ApplicationPayload = {
    /// The identifier of the application that has been launched.
    application: string
}

type Target = int

type DeviceEvent = {
    /// Event name.
    event: string
    /// An opaque value identifying the device.
    device: string
    /// A json object containing information about the device.
    deviceInfo: DeviceInfo option
}

type SetTitlePayload = {
    /// The title to display. If there is no title parameter, the title is reset to the title set by the user.
    title: string
    /// Specify if you want to display the title on the hardware and software (0), only on the hardware (1) or only on the software (2). Default is 0.
    target: Target option
    /// A 0-based integer value representing the state of an action with multiple states. This is an optional parameter. If not specified, the title is set to all states.
    state: int option
}

type SetImagePayload = {
    /// The image to display encoded in base64 with the image format declared in the mime type (PNG, JPEG, BMP, ...). svg is also supported. If no image is passed, the image is reset to the default image from the manifest.
    image: string
    /// Specify if you want to display the title on the hardware and software (0), only on the hardware (1) or only on the software (2). Default is 0.
    target: Target option
    /// A 0-based integer value representing the state of an action with multiple states. This is an optional parameter. If not specified, the image is set to all states.
    state: int option
}

type SetFeedbackLayoutPayload = {
    /// A predefined layout identifier or the relative path to a json file that contains a custom layout
    layout: string
}

type TouchTapActionPayload = {
    /// This JSON object contains data that you can set and are stored persistently.
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates
    /// The array which holds (x, y) coordinates as a position of tap inside of LCD slot associated with action.
    tapPos: int[]
    /// Boolean which is true when long tap happened
    hold: bool
}

type EncoderPayload = {
    /// This JSON object contains data that you can set and are stored persistently.
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates
    /// Boolean which is true on encoder pressed, else false on released
    pressed: bool
}

type DialRotatePayload = {
    /// This JSON object contains data that you can set and are stored persistently.
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates
    /// The integer which holds the number of "ticks" on encoder rotation. Positive values are for clockwise rotation, negative values are for counterclockwise rotation, zero value is never happen
    ticks: int
    /// Boolean which is true on rotation when encoder pressed
    pressed: bool
}

type SetTriggerDescriptionPayload = {
    /// Touchscreen "long-touch" interaction behavior description; when undefined, the description will not be shown.
    longTouch: string
    /// Dial "push" (press) interaction behavior description; when undefined, the description will not be shown.
    push: string
    /// Dial rotation interaction behavior description; when undefined, the description will not be shown.
    rotate: string
    /// Touchscreen "touch" interaction behavior description; when undefined, the description will not be shown.
    touch: string
}
