# Changelog

## [1.6.0.0] - 2025-03-09

- Dependencies update (.NET 9, Fable 4.24, Node 22, React 19)

## [1.5.0] - 2023-12-28

- Reusable SDK components, styled to SdPi guidelines
- Dependencies update, Fun.Build and .NET 8 (#31)

## [1.4.0] - 2023-01-01

- Added configurable interval for Switch action state auto-update #7
- Removed analytics #20
- Removed warning response when Homebridge does not change state immediately
- Auth token caching
- F# SDK aligned with [Changes in Stream Deck 6.0](https://developer.elgato.com/documentation/stream-deck/sdk/changelog/)
- Dependencies update (Fable 4, React 18, Feliz 2, Elmish 4 and more)

## [1.3.1] - 2022-03-27

- Fix version in manifest.json

## [1.3.0] - 2022-02-27

- Improved Host validation pattern
- Improved PI error messages
- Relaxed Homebridge schema requirements
- Fallback for layout unavailability
- Exception analytics
- Dependencies update

## [1.2.0] - 2022-02-09

- New accessory selector show all accessories and disable not compatible accessories
- Added support for accessories outside of room layout
- Added loading indicator when waiting for Homebridge responses
- Added reset button option when device is no longer available
- Added timeouts for Homebridge API calls
- Improved error handling and user error message

## [1.41.0] - 2022-01-29

- Added `Set state` action

## [1.0.0] - 2022-01-27

- Initial release
- Added `Switch` action that allow you toggle on/off you accessories or their features
- Added `Config UI` action open web ui of connected Homebridge server
