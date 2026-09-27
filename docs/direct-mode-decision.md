---
title: BetterJoy Direct input output boundary
type: decision
status: accepted
project: BetterJoy Direct
date: 2026-09-05
---

The user's Pro Controller needs a system-wide Xbox mapping for remote software, with no Steam dependency and no unwanted screenshots or mouse movement. Keep the existing USB/Bluetooth HID → ViGEm Xbox pipeline and ship an independent portable application beside the original installation.

The upstream HID reader accepted command acknowledgements and partial reports as controller input, while Capture defaulted to PrintScreen. Validate input report layout and establish a released-button baseline before exporting inputs.

On 2026-09-08 the custom keyboard/mouse mapping feature was removed because its scope was easy to misread: it affected a few legacy special-button bindings, not ordinary sticks and buttons. Global keyboard/mouse capture, key/mouse emission, the runtime toggle and the modern mapping entry are gone. Gyro-to-mouse remains a direct, self-contained mode. The separate default-on Steam shortcut path emits only Capture→F12 and Home→Shift+Tab.

Hardware testing also found that an initial all-zero virtual report left stale nonzero XInput axes. A separate native probe reproduced it without the HID reader. Force a least-significant axis transition followed immediately by neutral at virtual connection; test via Windows XInput, not just managed report construction.

## Observations

- [decision] Use ViGEmBus already installed on the machine; do not install an unsigned replacement driver or require Steam.
- [decision] Keep raw-device hiding opt-in. HidHide can prevent double input with other mappers, but is not required for Steam-free operation and changes system device visibility.
- [alternative] A wholly new kernel driver requires a separate implementation, signing, distribution and compatibility effort; it is outside this application patch.
- [limitation] ViGEmBus itself is retired upstream. Updating this application does not provide ongoing maintenance of that driver.
- [evidence] Upstream base: b6715638a3ed1084f8968e8cafebbc6fe2ed0096. Local Bluetooth and XInput records: outputs/bluetooth-final-test.txt, outputs/reconnect-test.txt, outputs/virtual-probe.txt.
- [decision] XInput has no standard gyro field. Expose runtime gyro-to-stick and optional gyro-to-mouse modes instead of claiming native Xbox gyro passthrough.
- [location] Public derivative: https://github.com/zzirui321-rgb/BetterJoy-Direct; development started from upstream master at `b6715638a3ed1084f8968e8cafebbc6fe2ed0096`.
