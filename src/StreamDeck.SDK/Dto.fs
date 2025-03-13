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

/// Information about the newly connected device.
and DeviceInfo = {
    /// Name of the device, as specified by the user in the Stream Deck application.
    name: string
    /// Number of action slots, excluding dials / touchscreens, available to the device.
    size: DeviceSize
    /// Type of the device that was connected, e.g. Stream Deck +, Stream Deck Pedal, etc. See DeviceType.
    ``type``: DeviceType
}

and DeviceSize = { columns: int; rows: int }

/// Type of the device that was connected, e.g. Stream Deck +, Stream Deck Pedal, etc. See DeviceType.
and DeviceType = int


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
    /// Unique identifier of the action as defined within the plugin's manifest (Actions[].UUID) e.g. "com.elgato.wavelink.mute".
    action: string
    /// Name of the event used to identify what occurred.
    event: string
    /// Identifies the instance of an action that caused the event, i.e. the specific key or dial. This identifier can be used to provide feedback to the Stream Deck, persist and request settings associated with the action instance, etc.
    context: string
    /// Unique identifier of the Stream Deck device that this event is associated with.
    device: string
    /// Contextualized information for this event.
    payload: obj
}

type ActionPayload = {
    /// Defines the controller type the action is applicable to. Keypad refers to a standard action on a Stream Deck device, e.g. 1 of the 15 buttons on the Stream Deck MK.2, or a pedal on the Stream Deck Pedal, etc., whereas an Encoder refers to a dial / touchscreen on the Stream Deck +.
    controller: string
    /// Determines whether the action is part of a multi-action.
    isInMultiAction: bool
    /// Settings associated with the action instance.
    settings: obj
    /// The coordinates of the action triggered.
    coordinates: Coordinates option
    /// Current state of the action; only applicable to actions that have multiple states defined within the manifest.json file.
    state: int option
}

type ActionTitlePayload = {
    /// Defines the controller type the action is applicable to. Keypad refers to a standard action on a Stream Deck device, e.g. 1 of the 15 buttons on the Stream Deck MK.2, or a pedal on the Stream Deck Pedal, etc., whereas an Encoder refers to a dial / touchscreen on the Stream Deck +.
    controller: string
    /// Coordinates that identify the location of an action.
    coordinates: Coordinates
    /// Settings associated with the action instance.
    settings: obj
    /// Current state of the action; only applicable to actions that have multiple states defined within the manifest.json file.
    state: int
    /// Title of the action, as specified by the user or dynamically by the plugin.
    title: string
    /// Defines aesthetic properties that determine how the title should be rendered.
    titleParameters: ActionTitleParameters
}

and ActionTitleParameters = {
    /// Font-family the title will be rendered with.
    fontFamily: string
    /// Font-size the title will be rendered in.
    fontSize: int
    /// Typography of the title. "" | "Bold Italic" | "Bold" | "Italic" | "Regular"
    fontStyle: string
    /// Boolean indicating an underline under the title.
    fontUnderline: bool
    /// Determines whether the user has opted to show, or hide the title for this action instance.
    showTitle: bool
    /// Alignment of the title. "bottom" | "middle" | "top"
    titleAlignment: string
    /// Color of the title, represented as a hexadecimal value.
    titleColor: string
}

type ApplicationEvent = {
    event: string
    payload: ApplicationPayload
}

/// Payload containing information about the application that triggered the event.
and ApplicationPayload = {
    /// Name of the application that triggered the event.
    application: string
}

type Target = int

type DeviceEvent = {
    /// Name of the event used to identify what occurred.
    event: string
    /// Unique identifier of the Stream Deck device that this event is associated with.
    device: string
    /// Information about the newly connected device.
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
    /// Defines the controller type the action is applicable to. Keypad refers to a standard action on a Stream Deck device, e.g. 1 of the 15 buttons on the Stream Deck MK.2, or a pedal on the Stream Deck Pedal, etc., whereas an Encoder refers to a dial / touchscreen on the Stream Deck +.
    controller: string
    /// The coordinates of the action triggered.
    coordinates: Coordinates
    /// Boolean which is true when long tap happened
    hold: bool
    /// This JSON object contains data that you can set and are stored persistently.
    settings: obj
    /// The array which holds (x, y) coordinates as a position of tap inside of LCD slot associated with action.
    tapPos: int[]
}

type EncoderPayload = {
    /// Defines the controller type the action is applicable to. Keypad refers to a standard action on a Stream Deck device, e.g. 1 of the 15 buttons on the Stream Deck MK.2, or a pedal on the Stream Deck Pedal, etc., whereas an Encoder refers to a dial / touchscreen on the Stream Deck +.
    controller: string
    /// Settings associated with the action instance.
    settings: obj
    /// Coordinates that identify the location of the action.
    coordinates: Coordinates
}

type DialRotatePayload = {
    /// Settings associated with the action instance.
    settings: obj
    /// Coordinates that identify the location of the action.
    coordinates: Coordinates
    /// Defines the controller type the action is applicable to. Keypad refers to a standard action on a Stream Deck device, e.g. 1 of the 15 buttons on the Stream Deck MK.2, or a pedal on the Stream Deck Pedal, etc., whereas an Encoder refers to a dial / touchscreen on the Stream Deck +.
    controller: string
    /// Number of ticks the dial was rotated; this can be a positive (clockwise) or negative (counter-clockwise) number.
    ticks: int
    /// Determines whether the dial was pressed whilst the rotation occurred.
    pressed: bool
}

/// Payload containing information about the URL that triggered the event.
type DidReceiveDeepLinkPayload = {
    /// The deep-link URL, with the prefix omitted.
    url: string
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
