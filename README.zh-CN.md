<div align="center">

# BetterJoy Direct

**接入 Switch 手柄，稳定输出 Xbox 控制。**

BetterJoy 的专注型 Windows x64 分支：更安全的重连、运行时陀螺仪控制，以及清爽的双语界面。

<p>
  <img alt="Windows 10 和 11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64" src="https://img.shields.io/badge/architecture-x64-6C63FF">
  <img alt="381 项回归检查" src="https://img.shields.io/badge/regression%20checks-381%20passing-2EA44F">
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue.svg"></a>
</p>

<p>
  <a href="README.md">English</a>
  ·
  <a href="README.zh-CN.md"><strong>简体中文</strong></a>
  ·
  <a href="https://github.com/zzirui321-rgb/BetterJoy-Direct/releases">下载</a>
  ·
  <a href="#快速开始">快速开始</a>
  ·
  <a href="#项目文档">项目文档</a>
</p>

</div>

---

> [!NOTE]
> BetterJoy Direct 将受支持的 Nintendo Switch 手柄转换为虚拟 Xbox 360/XInput 设备。Steam 是可选项，核心 USB/蓝牙到 XInput 链路不依赖 Steam。

## 为什么选择 BetterJoy Direct？

| | |
|---|---|
| **🎮 直接输出 XInput**<br>通过 ViGEmBus 将 Switch Pro Controller 和 Joy-Con 转换为虚拟 Xbox 360 手柄。 | **🛡️ 更安全的重连**<br>拒绝异常 HID 报文，等待中立释放基线，并在重连后清除陈旧输出。 |
| **🧭 实时陀螺仪模式**<br>无需重连即可在关闭、左摇杆、右摇杆和鼠标之间切换。 | **🌐 双语界面**<br>在可缩放且托盘恢复稳定的窗口中即时切换中英文。 |
| **⌨️ Steam 快捷键**<br>可选 Capture → `F12`、Home → `Shift+Tab`，并通过按下沿保护避免连发。 | **📍 温和定位**<br>使用受强度限制的短促震动定位手柄，连续点击也不会叠加。 |

<p align="center">
  <img src="docs/images/interface-zh.png" alt="BetterJoy Direct 中文界面">
</p>
<p align="center"><sub>四个手柄槽位、连接诊断、运行时陀螺仪控制与一键语言切换。</sub></p>

## 快速开始

### 1. 准备虚拟手柄驱动

- Windows 10 或 Windows 11 x64
- 通过 USB 或蓝牙连接的 Switch Pro Controller 或受支持的 Joy-Con

完整的 x64 发布包内含上游提供的 ViGEmBus `1.17.333.0` x64 安装程序，其 Authenticode 签名者为 Nefarius Software Solutions e.U.。首次启动时，BetterJoy Direct 若检测不到驱动，会先询问是否启动安装程序，随后由 Windows 显示正常的 UAC 管理员授权。驱动绝不会静默安装；选择**否**仍可打开界面，但不会产生虚拟 Xbox/XInput 输出。

ViGEmBus 是系统驱动，无法真正“便携化”，因此仍需完成一次管理员授权安装。其上游项目已经停止维护；BetterJoy Direct 仅为兼容性附带现有签名安装程序，不维护或重新签名该内核驱动。版本和 SHA-256 记录在 [`Drivers/README.txt`](BetterJoyForCemu/Drivers/README.txt)。

### 2. 下载并连接

1. 从 [GitHub Releases](https://github.com/zzirui321-rgb/BetterJoy-Direct/releases) 下载便携 ZIP，并完整解压。
2. 退出旧版 BetterJoy，以及可能同时打开同一物理手柄的其它映射软件。
3. 运行 `BetterJoyForCemu.exe`。
4. 若出现提示，请选择**是**、通过 Windows UAC，并完成随包 ViGEmBus 安装；如有要求，请重启 BetterJoy Direct 或 Windows。
5. USB 可直接连接；蓝牙请长按手柄同步键，然后在 Windows 蓝牙设置中配对。
6. 首次连接或重连后松开全部按键，等待至少 **300 毫秒**，让中立输入保护完成。
7. 点击 **测试 Xbox 输入**，在 Windows 游戏控制器面板中检查按键与摇杆。

> [!TIP]
> 普通按键和摇杆输出 XInput，因此 Windows 资源管理器不会响应它们。陀螺仪鼠标模式会直接移动光标。

> [!IMPORTANT]
> 不要让 Steam Input 和 BetterJoy 同时映射同一个物理手柄，否则可能出现双重输入。HidHide 仍由用户自行决定和配置；BetterJoy Direct 不会自动修改设备隐藏规则。

## 运行时控制

| 控件 | 输出 | 适用场景 |
|---|---|---|
| **Steam 快捷键** | Capture → `F12`；Home → `Shift+Tab` | Steam 截图和叠加层 |
| **关闭陀螺仪** | 不转换运动输出 | 标准 Xbox 控制 |
| **陀螺仪 → 左摇杆** | 虚拟左摇杆移动 | 实验性的移动控制 |
| **陀螺仪 → 右摇杆** | 虚拟右摇杆移动 | 兼容面更广的陀螺仪瞄准 |
| **陀螺仪 → 鼠标** | 直接移动 Windows 光标 | 鼠标视角游戏与桌面操作 |

Xbox/XInput 没有标准的原生陀螺仪字段。支持 DSU/Cemuhook 的软件可以改用可选 Motion Server。本地 Steam 游戏使用 Steam Input 原生处理陀螺仪时可能更顺滑；此时应退出 BetterJoy，或避免 Steam 同时读取物理手柄，以免产生双重输入。

## 可靠性链路

```text
Switch Pro / Joy-Con
        │ USB 或蓝牙 HID
        ▼
报文验证 → 重连释放门 → 校准与输入解析
        │
        ├─ ViGEmBus → Xbox 360 / XInput → 游戏或远程客户端
        ├─ 可选 Steam 快捷键 → F12 / Shift+Tab
        ├─ 可选陀螺仪 → 左摇杆 / 右摇杆 / 鼠标
        └─ 可选 Motion Server → DSU/Cemuhook 数据
```

BetterJoy Direct 只有在用户明确同意并通过 Windows UAC 后才会启动随包驱动安装程序，绝不会静默安装。它不会修改 Steam 设置、HidHide 或 Windows 设备隐藏规则。远程控制软件必须自身支持游戏手柄转发；BetterJoy Direct 无法为任意远程协议自动增加手柄通道。

## 验证概览

| 范围 | 结果 |
|---|---|
| 输入回归 | **343 项通过**——报文边界、全部 256 个 report ID、时间戳、重连释放门、校准、死区、Steam 快捷键按下沿、陀螺仪方向和 Guide 抑制 |
| 界面与行为 | **38 项通过**——双语文案、驱动安装引导、运行时设置、窗口缩放和托盘恢复 |
| 虚拟 Xbox 生命周期 | **连续 3 轮通过**——创建、移动、归零与断开 |
| 实体硬件 | **704 个 XInput 样本**——Switch Pro 蓝牙连接，包括物理断开与自动重连 |

已验证的实体链路是 Switch Pro Controller 蓝牙连接。USB、其它手柄型号、具体游戏、反作弊系统和具体远程控制软件仍需在各自环境中验证。

## 从源码构建

构建需要 Windows、Python 3，以及 .NET Framework 4.6.1 Developer Pack 或兼容的更新 .NET Framework 4.x 构建环境。依赖只恢复到项目目录，不进行全局安装。

```powershell
python tools/restore.py
.\build-direct.ps1
.\test-direct.ps1
.\test-features.ps1
```

安装 ViGEmBus 后，还可以运行虚拟手柄生命周期测试：

```powershell
.\test-xbox-output.ps1
```

生成便携压缩包：

```powershell
python tools/package-direct.py
```

生成的程序、硬件日志、下载的工具压缩包、构建缓存和发布 ZIP 均不会进入 Git。

## 项目文档

| 文档 | 内容 |
|---|---|
| [功能与配置说明](docs/FEATURES.zh-CN.md) | 数据链路、运行时控制、高级设置、使用边界与排错 |
| [Development journey — English](docs/DEVELOPMENT-JOURNEY.md) | 从问题诊断到可验证 Direct 构建的完整过程 |
| [开发历程 — 中文](docs/DEVELOPMENT-JOURNEY.zh-CN.md) | 中文版开发过程与关键取舍 |
| [架构决策](docs/direct-mode-decision.md) | Direct 模式的范围、边界和未采用方案 |
| [更改日志](CHANGELOG.md) | 各个 Direct 里程碑的用户可见变化 |
| [上游原始 README](docs/UPSTREAM-README.md) | 原始使用说明、致谢和项目历史 |

## 上游与许可证

BetterJoy Direct 是 [Davidobot/BetterJoy](https://github.com/Davidobot/BetterJoy) 的独立衍生版本，基于上游提交 `b6715638a3ed1084f8968e8cafebbc6fe2ed0096`。它并非 BetterJoy 官方发布，原项目作者和贡献者继续保留完整署名。

项目继续使用 [MIT License](LICENSE)。便携压缩包也附带可获得的第三方依赖许可证或包元数据。
