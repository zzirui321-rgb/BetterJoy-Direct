# BetterJoy Direct

[English](README.md) | [简体中文](README.zh-CN.md)

BetterJoy Direct is a Windows x64 fork of [Davidobot/BetterJoy](https://github.com/Davidobot/BetterJoy), based on upstream commit `b6715638a3ed1084f8968e8cafebbc6fe2ed0096`.

It keeps BetterJoy's Nintendo Switch controller to Xbox 360/XInput pipeline while making reconnects safer, exposing gyro controls at runtime, simplifying desktop input behavior, and replacing the legacy window with a resizable bilingual interface.

![BetterJoy Direct English interface](docs/images/interface-en.png)

## Highlights

- Converts Switch Pro Controllers and Joy-Cons connected over USB or Bluetooth into virtual Xbox 360/XInput controllers through ViGEmBus.
- Validates HID input reports before exporting buttons or axes and waits for a neutral release baseline after connection or reconnection.
- Clears stale Windows XInput axes when a virtual controller is created.
- Offers live gyro modes: off, left stick, right stick, or mouse.
- Provides optional Steam shortcuts: Capture sends `F12`; Home sends `Shift+Tab`.
- Uses a modern Chinese/English interface with four controller cards, connection logs, Windows Bluetooth and controller-test shortcuts, and resizable/tray-safe window behavior.
- Uses a short, strength-limited locate rumble instead of the original strong pulse.
- Removes the misleading custom keyboard/mouse mapping toggle and its global input hooks. Standard buttons and sticks remain Xbox input.

## Download and requirements

Download the current portable ZIP from [GitHub Releases](https://github.com/zzirui321-rgb/BetterJoy-Direct/releases), extract the whole archive, and run `BetterJoyForCemu.exe`.

Requirements:

- Windows 10 or Windows 11 x64
- An installed [ViGEmBus](https://github.com/nefarius/ViGEmBus) driver
- A Switch Pro Controller or supported Joy-Con connection over USB or Bluetooth

The portable package does not install or change drivers, Steam settings, HidHide, or Windows device-hiding rules. ViGEmBus has been retired upstream; this project reuses the installed driver and does not replace its kernel component.

## First use

1. Close other BetterJoy instances and controller mappers that could open the same physical device.
2. Connect by USB, or hold the controller's sync button and pair it in Windows Bluetooth settings.
3. After a connection or reconnection, release every button and wait at least 300 ms for the neutral-input gate.
4. Select **Test Xbox input** to open the Windows game-controller panel, then verify sticks and buttons there or in a game.

Regular buttons and sticks produce XInput. Windows Explorer does not use XInput for desktop navigation, so no desktop response is expected. Gyro mouse mode produces pointer movement directly. A game may also reject simultaneous mouse and gamepad input or switch between the two input modes.

## Runtime controls

| Control | Behavior |
|---|---|
| Steam shortcuts | Capture → `F12`; Home → `Shift+Tab` |
| Gyro off | Motion data is read but not converted to game input |
| Gyro → left stick | Adds gyro movement to the virtual left stick |
| Gyro → right stick | Adds gyro movement to the virtual right stick; recommended for broad game compatibility |
| Gyro → mouse | Moves the Windows pointer directly and does not require another mapping toggle |

Xbox/XInput has no standard native gyro field. Software with DSU/Cemuhook support can instead enable the optional motion server. Local Steam games may feel smoother when Steam Input handles the physical Switch controller directly; avoid letting Steam and BetterJoy map the same physical device at the same time.

## Build from source

Building requires Windows, the .NET Framework 4.6.1 Developer Pack or a compatible newer 4.x build environment, and Python 3. Dependencies are restored into the repository rather than installed globally.

```powershell
python tools/restore.py
.\build-direct.ps1
.\test-direct.ps1
.\test-features.ps1
```

Additional virtual-controller verification is available when ViGEmBus is installed:

```powershell
.\test-xbox-output.ps1
```

The portable package is generated with:

```powershell
python tools/package-direct.py
```

Generated binaries, hardware logs, downloaded tool archives, and build caches are excluded from Git. Release ZIP files belong in GitHub Releases rather than repository history.

## Verification status

- 343 input regression checks cover report bounds, all 256 report IDs, duplicate timestamps and wraparound, reconnect release gating, deadzones, invalid calibration, Steam shortcut edges, gyro Y direction, Guide suppression, paired Joy-Con shortcut ownership, and removal of legacy keyboard/mouse hooks.
- 32 bilingual UI strings and runtime language, Steam shortcut, gyro, resize, and tray-restore behaviors are covered by feature regression tests.
- The virtual Xbox lifecycle passed three consecutive create, move, neutralize, and disconnect cycles on the development machine.
- A physical Switch Pro Controller Bluetooth session recorded 704 XInput samples, a physical disconnect, a successful automatic reconnect, neutral final axes, and no output error.

Switch Pro Bluetooth is the verified hardware path. USB, other controller models, individual games, anti-cheat systems, and specific remote-control software still require testing in their own environments.

Remote-control software must provide its own gamepad-forwarding channel; BetterJoy Direct cannot add controller transport to an arbitrary remote protocol.

## Documentation

- [Feature and configuration reference — 中文](docs/FEATURES.zh-CN.md)
- [Development journey — English](docs/DEVELOPMENT-JOURNEY.md)
- [开发历程 — 中文](docs/DEVELOPMENT-JOURNEY.zh-CN.md)
- [Architecture decision](docs/direct-mode-decision.md)
- [Changelog](CHANGELOG.md)

## Upstream and license

BetterJoy Direct is an independent derivative of [Davidobot/BetterJoy](https://github.com/Davidobot/BetterJoy). It is not an official BetterJoy release. Upstream authors and contributors retain credit for the original project.

The [original upstream README](docs/UPSTREAM-README.md) is preserved with its acknowledgements, usage notes, and project history.

The project remains under the repository's [MIT License](LICENSE). The portable archive also carries the available license or package metadata for bundled third-party dependencies.
