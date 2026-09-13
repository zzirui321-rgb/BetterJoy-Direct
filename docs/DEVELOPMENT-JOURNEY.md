# BetterJoy Direct: Development Journey

[English](DEVELOPMENT-JOURNEY.md) | [简体中文](DEVELOPMENT-JOURNEY.zh-CN.md)

This document records the engineering path from the upstream BetterJoy snapshot to BetterJoy Direct. It focuses on observations, wrong assumptions, corrections, evidence, and the decisions that shaped the release.

## Starting point

The work began from Davidobot/BetterJoy commit `b6715638a3ed1084f8968e8cafebbc6fe2ed0096`. The practical goal was simple: connect a Switch Pro Controller over Bluetooth or USB and expose a reliable system-wide Xbox 360/XInput device without requiring Steam.

The existing architecture already contained the right core path:

```text
Switch HID → BetterJoy parser → ViGEmBus → virtual Xbox 360 controller
```

Replacing that path with a new driver would have expanded the project into kernel development, signing, distribution, and long-term compatibility work. BetterJoy Direct therefore kept the existing ViGEm pipeline and concentrated on application-level correctness, recovery, and usability.

## Phase 1 — establish an input boundary

The first risk was not button mapping. It was deciding which HID packets were allowed to become controller input.

The upstream reader could pass command replies, short packets, duplicate timestamps, or unexpected report IDs into input processing. During connection and reconnection, the Home or Capture button used to wake a controller could also be mistaken for a fresh user action.

The Direct path added an `InputReportGuard` that:

- accepts the expected `0x30` input layout and exact length;
- rejects command responses, short data and all other report IDs;
- rejects duplicate timestamps while allowing byte wraparound;
- waits for released buttons and approximately 300 ms of stable input before arming output;
- clears buttons and axes while the guard is not armed.

The regression suite intentionally checks every possible one-byte report ID, rather than testing only a few examples.

## Phase 2 — reconnect without stale state

HID handles, polling threads and virtual-controller targets originally had overlapping lifetimes. Closing a handle while its reader was still active could produce intermittent failures, while repeated scans could attach duplicate UI event handlers.

The lifecycle was changed so that polling stops and the reader thread joins before HID and virtual output objects are released. Failed controller initialization now reports a reason and returns the device to the scan path. UI updates initiated by scan timers are marshalled back to the Windows Forms thread.

An inactivity boundary was also made explicit: when valid reports disappear for more than 1.5 seconds, the virtual state is neutralized before the controller is treated as disconnected.

## Phase 3 — the all-zero report surprise

A hardware test revealed a result that looked impossible at first: BetterJoy submitted an all-zero virtual report, yet Windows still exposed non-zero XInput axes from an earlier device state.

A small native probe reproduced the behavior independently of the HID parser. This separated an application-input problem from a virtual-device initialization problem.

The adopted workaround waits until the XInput slot becomes readable, submits a one-unit axis transition below the standard XInput deadzone, and immediately submits a neutral report. The virtual-output test repeats create, move, neutralize and disconnect cycles to guard this behavior.

## Phase 4 — make the state visible

Correct recovery is difficult to trust when the interface hides what the program is doing. The legacy layout was replaced with a modern bilingual Windows Forms interface containing:

- four controller cards and localized disconnected states;
- direct links to Windows Bluetooth settings and the game-controller test panel;
- a connection log that shows driver, enumeration, initialization and shortcut events;
- a Chinese/English toggle persisted in configuration;
- runtime Steam-shortcut and gyro controls;
- advanced configuration in a scrollable panel.

The locate action also changed after its original full-strength, 300 ms rumble felt excessive. It now uses a short `0.18` strength, `120 ms` pulse, clamps unsafe configuration values, and blocks overlapping clicks.

## Phase 5 — window and tray corrections

The first modern window was visually useful but too large and barely resizable. Reducing the window dimensions alone then compressed controller cards and changed the internal composition.

The final correction separated outer size from inner proportions. The default client area became `900×700`, the minimum became approximately `760×620`, and the major rows were scaled from the original `980×820` composition. This preserved the relative height of the title, status, controller cards, actions, quick controls and footer while allowing useful resizing.

A separate tray problem came from showing the form while it was still minimized and only then restoring its state. The restore sequence now prepares normal window state and taskbar visibility before making the form visible, reducing blank frames and repeated layout work.

## Phase 6 — understand desktop input before simplifying it

One confusing observation drove the last major design change: gyro mouse could move the pointer while sticks and face buttons appeared to do nothing on the Windows desktop.

The behavior was valid. Gyro mouse emits mouse movement, while ordinary controls remain XInput. Windows Explorer does not use XInput as a desktop navigation protocol, so those buttons are expected to be silent there and active in `joy.cpl`, a controller test, or a game.

The “custom keyboard/mouse bindings” option did not convert the complete controller into desktop input. It gated a small set of legacy special-button mappings and global keyboard/mouse hooks. Its label implied a much broader capability and made the valid XInput behavior look broken.

The feature was removed instead of renamed:

- the modern toggle and mapping entry were removed;
- `EnableDesktopInput` was removed;
- legacy keyboard/mouse emission was removed from the controller path;
- global keyboard and mouse capture was removed from both startup and the legacy reassignment form;
- gyro mouse became a self-contained runtime mode;
- fixed Steam shortcuts remained separately controllable;
- legacy controller-to-controller `joy_` remaps remain compatible internally.

This was a correction of the mental model, not only a UI cleanup.

## Verification evidence

The release was checked at several levels:

- **343 input regressions:** packet bounds, all 256 report IDs, timestamp duplication and wraparound, release gating, calibration failure, deadzones, shortcut edges, gyro direction, Guide suppression and removal of keyboard/mouse hooks.
- **32 bilingual UI strings and feature checks:** both languages, live controls, window sizing and tray restore ordering.
- **Virtual Xbox lifecycle:** three consecutive create, move, neutralize and disconnect cycles.
- **Physical Bluetooth session:** 704 XInput samples, one physical disconnect, successful automatic reconnection, neutral final axes and no output error.
- **Rendered UI inspection:** both Chinese and English `900×700` previews were opened and checked for missing controls, clipping and unintended wrapping.
- **Portable-package verification:** required files and dependency notices are hashed and reread from the generated ZIP.

## What remains uncertain

The evidence supports the tested Switch Pro Bluetooth and Windows XInput path. It does not prove every controller model, USB chipset, game, anti-cheat system, Steam configuration or remote-control product.

ViGEmBus is also retired upstream. BetterJoy Direct can make its use safer at the application layer, but it cannot provide maintenance or signing for that kernel driver.

## Lessons retained

1. Validate the input boundary before changing mappings.
2. Treat thread, HID-handle and virtual-device lifetimes as one shutdown sequence.
3. Verify output through Windows XInput, not only through managed object state.
4. Separate Windows desktop behavior from game-controller behavior when diagnosing input.
5. Preserve internal layout proportions when changing a window's outer dimensions.
6. Replace ambiguous controls when their mental model cannot be made honest with a label.
7. Publish raw binaries and diagnostic logs as release evidence or local artifacts, while keeping source history reviewable.

The concise version-by-version record is available in the [changelog](../CHANGELOG.md), and the accepted architecture boundary is recorded in the [decision note](direct-mode-decision.md).
