# BetterJoy Direct 功能说明

## 目标与数据链路

BetterJoy Direct 把 Switch Pro / Joy-Con 的 USB 或蓝牙 HID 输入转换为 Windows Xbox 360 / XInput 设备，供游戏和支持手柄转发的远程软件使用。Steam 不是转换链路的依赖。

```text
Switch Pro / Joy-Con
        │ USB / Bluetooth HID
        ▼
有效报文检查 → 重连释放门 → 校准与输入解析
        │
        ├─ ViGEmBus → Xbox 360 / XInput → 游戏 / 远程软件
        ├─ Steam 快捷键 → F12 / Shift+Tab
        ├─ 可选陀螺仪 → 左摇杆 / 右摇杆 / 鼠标
        └─ 可选 Motion Server → UDP 运动数据
```

原始 Switch HID 默认仍对其他程序可见。若 Steam Input 或另一个映射器同时处理物理手柄，可能产生双重输入；可退出另一个映射器，或自行配置 HidHide。不要隐藏 BetterJoy 创建的虚拟 Xbox 设备。

## 主界面快捷设置

### Steam 快捷键

“Steam 快捷键”默认开启：

| Switch 按键 | 发送内容 | Steam 默认行为 |
|---|---|---|
| Capture | `F12` | 游戏截图 |
| Home | `Shift+Tab` | 打开或关闭游戏内叠加层 |

映射只在按键从松开变成按下时发送一次，长按不会连发。成对 Joy-Con 由左侧发送 Capture、右侧发送 Home，既保留两个物理按键，也避免非所属一侧重复发送。重连释放门仍然优先：用于唤醒手柄的 Home/Capture 不会触发快捷键，松开并稳定约 300 ms 后再次短按才会触发。

开启 Steam 快捷键后，Home 不再同时发送虚拟 Xbox Guide，避免两个入口互相抵消。关闭该开关后，Home 恢复 Xbox Guide。旧版自定义键盘/鼠标绑定和全局键鼠监听已删除；普通按键和摇杆始终输出标准 Xbox 输入。

Valve 当前官方说明仍把 `F12` 列为默认截图键、`Shift+Tab` 列为默认叠加层快捷键：

- [Steam 客服：Steam 社区叠加界面](https://help.steampowered.com/zh-cn/faqs/view/3978-072C-18DF-FBF9)

### 陀螺仪模式

Switch Pro 的 IMU 在连接初始化时已经开启，原始陀螺仪与加速度数据也一直被读取。默认“关闭”只表示不把运动数据转换为游戏输入。

| 模式 | 输出 | 用途 |
|---|---|---|
| 关闭 | 无 | 标准 Xbox 控制 |
| 映射左摇杆 | XInput 左摇杆 | 移动类控制实验 |
| 映射右摇杆（推荐） | XInput 右摇杆 | 通用陀螺仪瞄准 |
| 映射鼠标 | Windows 鼠标移动 | 鼠标视角游戏或桌面 |

Xbox 360 / XInput 协议没有标准陀螺仪字段，因此“直连 Xbox”模式不能把原生 gyro 直接交给游戏。映射右摇杆是兼容面最广的方案；支持 Cemuhook/DSU 的软件也可以改用 `MotionServer=true` 读取运动数据。

模式在已连接手柄上立即生效并保存到 `GyroToJoyOrMouse`。灵敏度仍由以下高级设置控制：

- `GyroStickSensitivityX/Y`：摇杆模式灵敏度。
- `GyroStickReduction`：物理摇杆与陀螺仪叠加时的缩放。
- `GyroMouseSensitivityX/Y`：鼠标模式灵敏度。
- `UseFilteredIMU`：使用融合姿态或原始角速度。
- `GyroStickInvertY`：反转陀螺仪到摇杆的垂直方向；默认 `false` 使用正常 XInput 上下方向。
- `GyroHoldToggle` 与旧 `settings` 中的控制器激活键仍可供兼容配置使用；默认值 `0` 表示陀螺仪持续激活。

陀螺仪持续激活时，传感器零偏可能造成轻微视角漂移。先使用“陀螺仪校准”，再降低灵敏度。鼠标模式直接生效，不依赖已删除的自定义键鼠开关。

若主要玩本地 Steam 游戏，Steam Input 的原生陀螺仪通常比“gyro→XInput 右摇杆”更顺滑：让 Steam 直接识别 Switch Pro，在游戏的控制器布局中把 Gyro 设为 `Gyro To Mouse`，或对不能同时接收鼠标与手柄的游戏使用 `Gyro to Joystick (Camera)`。使用这条路径时退出 BetterJoy，或至少避免 Steam 同时处理物理 Switch 手柄和 BetterJoy 虚拟 Xbox，以免双重输入。

Valve 官方资料：[Steam Input 支持的设备](https://partner.steamgames.com/doc/features/steam_controller/device?language=english)、[Steam Input 手柄模拟最佳实践](https://partner.steamgames.com/doc/features/steam_controller/steam_input_gamepad_emulation_bestpractices?language=english)。

继续使用 BetterJoy 时，可在高级设置把 `UseFilteredIMU=false` 获得更直接的原始角速度响应；同时把摇杆灵敏度从滤波模式的 `40.0/10.0` 降到约 `2.6/0.6` 作为起点。原始模式延迟较低，但传感器噪声会更明显，修改后需要保存并重启。

## 连接与重连保护

- 只接受布局匹配的标准 `0x30` 输入报告。
- 丢弃短包、命令响应、重复时间戳和无效 report ID。
- 初次连接或重连时先等待按键全部释放和至少约 300 ms 的稳定输入。
- 无有效输入超过 1.5 秒时，虚拟输出归零并把设备标记为断开。
- HID 读取线程退出后才关闭句柄，扫描重连不会重复注册定位或配对事件。
- 校准响应必须匹配 SPI 地址、长度与确认位；异常校准保持中立值。

## Xbox 输出与 Steam 关系

- `ShowAsXInput=true` 时通过本机 ViGEmBus 创建设备。
- 创建后等待 XInput 槽位可读，再发送最小轴变化和中立帧，清除 Windows 可能保留的旧轴状态。
- Steam 未运行时，Windows 游戏和远程软件仍能识别虚拟 Xbox 设备。
- Steam 快捷键只是可选便利功能，不参与 HID→XInput 转换。
- 远程软件仍必须支持游戏手柄转发；BetterJoy 不会给任意远程协议自动增加手柄通道。

## 界面与定位震动

- `English / 中文` 按钮即时切换主界面、状态、指南和托盘文字，并保存 `UiLanguage`。
- 默认客户区为 `900×700`，各内部区域按原 `980×820` 界面的视觉比例缩放，可缩放到约 `760×620`；窄窗口下操作按钮可自动换行。
- 从托盘恢复时先准备正常窗口状态，再显示并激活；双缓冲减少恢复时的空白帧。
- 四个设备卡片显示连接状态，提供蓝牙设置和 Windows Xbox 输入测试入口。
- “轻震定位”默认使用 `80/160 Hz`、强度 `0.18`、持续 `120 ms`。
- 定位按钮在脉冲期间防重入，连续点击不会叠加震动。
- 配置会把定位强度限制在 `0.05–0.25`、持续时间限制在 `60–180 ms`。

## 关键配置

| 配置键 | 默认值 | 作用 |
|---|---:|---|
| `UiLanguage` | `zh-CN` | 主界面语言，支持 `zh-CN` / `en-US` |
| `EnableSteamShortcuts` | `true` | Capture→F12、Home→Shift+Tab |
| `GyroToJoyOrMouse` | `none` | `none` / `joy_left` / `joy_right` / `mouse` |
| `GyroStickInvertY` | `false` | 是否反转陀螺仪映射摇杆的垂直方向 |
| `ShowAsXInput` | `true` | 输出虚拟 Xbox 360 控制器 |
| `ShowAsDS4` | `false` | 输出虚拟 DS4；与 XInput 二选一更稳妥 |
| `MotionServer` | `false` | 启用 DSU/Cemuhook UDP 运动数据 |
| `UseHIDG` | `false` | 旧 HidGuardian 设备隐藏，默认不使用 |
| `HomeLongPowerOff` | `true` | Home（或单侧 Capture）长按约 2 秒关机 |
| `LocateRumbleStrength` | `0.18` | 定位震动强度，运行时限制到 0.05–0.25 |
| `LocateRumbleDurationMs` | `120` | 定位震动时长，运行时限制到 60–180 ms |

主界面的快捷设置立即生效。高级设置仍通过“保存并重启”应用。

## Steam 叠加层排错

当日志显示 `Steam shortcut: Shift+Tab overlay sent.` 而叠加层没有出现时，按以下顺序检查：

1. Steam → 设置 → 游戏中，确认游戏内叠加层已启用，快捷键仍为 `Shift+Tab`。
2. 游戏库 → 该游戏 → 属性 → 常规，确认此游戏允许 Steam 叠加层。
3. 保证 Steam、游戏和 BetterJoy 处于相同权限级别。Windows 不允许普通权限程序向管理员游戏可靠注入按键。
4. 某些游戏或反作弊会阻止模拟键盘输入；此时可关闭 Steam 快捷键，改用 Steam 对 Xbox Guide 的控制器配置。
5. Home 请短按；长按约 2 秒会按 `HomeLongPowerOff` 设置关闭手柄。

截图也依赖 Steam 叠加层。若 Steam 自定义了截图键，当前固定 `F12` 不会跟随自定义设置。

## 验证与边界

- 输入自动回归：343 项，覆盖 HID 报文边界、Steam 快捷键、陀螺仪方向，并确认旧键鼠发射入口已经删除。
- UI/功能回归：32 组双语文案，以及语言切换与保存、Steam/陀螺仪快捷设置、旧映射入口移除、窗口缩放和托盘恢复顺序。
- 虚拟 Xbox 输出：连续 3 轮创建、移动、归零、断开。
- 实体蓝牙记录：704 个 XInput 样本、1 次物理断开与成功重连；最终四轴归零，无输出错误。
- 已验证 Switch Pro 蓝牙；USB、其他第三方手柄、具体游戏/反作弊和具体远程软件仍需在对应环境验证。
- ViGEmBus 上游已经停止维护；本项目复用已安装驱动，不替换、安装或签名新的内核驱动。

详细历史见 [`../CHANGELOG.md`](../CHANGELOG.md)，架构取舍见 [`direct-mode-decision.md`](direct-mode-decision.md)。
