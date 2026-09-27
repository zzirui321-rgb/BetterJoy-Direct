from pathlib import Path
import hashlib
import json
import zipfile

root = Path(__file__).resolve().parents[1]
out = root / 'outputs'
app = out / 'BetterJoy-Direct'
guide = '''BetterJoy Direct · Windows x64

请完整解压，再双击 BetterJoyForCemu.exe。
先退出旧版 BetterJoy。USB 直接插入；蓝牙在 Windows 中配对。
重连后松开所有按键，等待至少 300 毫秒。
无需启动 Steam。在游戏/远程软件中选择 Xbox 360 / XInput。

主界面点 English / 中文，可立即切换语言并记住选择。
“轻震定位”使用温和短脉冲，连续点击不会叠加震动。
“Steam 快捷键”默认开启：Capture=F12，Home=Shift+Tab。
陀螺仪可直接映射到左右摇杆或鼠标。

旧版自定义键鼠绑定和全局键鼠监听已移除。
普通按键与摇杆保持 Xbox 输入，在 Windows 桌面无响应属于正常现象。
本程序依赖 ViGEmBus。完整发布包内含上游提供、由 Nefarius 签名的 x64 安装程序。
首次启动若检测不到驱动，程序会先征求同意，再启动安装程序和 Windows UAC；绝不静默安装。
若选择稍后安装，界面仍可打开，但不会产生虚拟 Xbox/XInput 输出。
ViGEmBus 已停止维护；安装程序版本与校验值见 Drivers/README.txt。
本程序不会自动改变 Steam、HidHide 或系统设备隐藏配置。
如果其它软件仍在映射物理手柄，先退出该软件以排除重复输入。

点“测试 Xbox 输入”检查按键。高级设置点“保存并重启”生效。
USB 和具体远程软件转发仍需在对应环境验证。
恢复旧版：退出本版，运行原来的 BetterJoy 即可。

源码与完整说明：https://github.com/zzirui321-rgb/BetterJoy-Direct
上游：https://github.com/Davidobot/BetterJoy
'''
(app / '使用说明.txt').write_text(guide, encoding='utf-8-sig')
names = ['BetterJoyForCemu.exe', 'BetterJoyForCemu.exe.config', 'Crc32.NET.dll',
         'JetBrains.Annotations.dll', 'Nefarius.ViGEm.Client.dll', 'WindowsInput.dll',
         'x64/hidapi.dll', 'Drivers/ViGEmBusSetup_x64.msi', 'Drivers/README.txt',
         'LICENSE', '使用说明.txt']
entries = [(app / name, name) for name in names]
entries += [
    (root / 'README.md', 'README.md'),
    (root / 'README.zh-CN.md', 'README.zh-CN.md'),
    (root / 'CHANGELOG.md', 'CHANGELOG.md'),
    (root / 'docs' / 'FEATURES.zh-CN.md', 'docs/FEATURES.zh-CN.md'),
    (root / 'docs' / 'DEVELOPMENT-JOURNEY.md', 'docs/DEVELOPMENT-JOURNEY.md'),
    (root / 'docs' / 'DEVELOPMENT-JOURNEY.zh-CN.md', 'docs/DEVELOPMENT-JOURNEY.zh-CN.md'),
    (root / 'docs' / 'direct-mode-decision.md', 'docs/direct-mode-decision.md'),
    (root / 'docs' / 'UPSTREAM-README.md', 'docs/UPSTREAM-README.md'),
    (root / 'docs' / 'images' / 'interface-en.png', 'docs/images/interface-en.png'),
    (root / 'docs' / 'images' / 'interface-zh.png', 'docs/images/interface-zh.png'),
]
archive = out / 'BetterJoy-Direct-win-x64.zip'
manifest = {}
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as bundle:
    for path, name in entries:
        assert path.is_file() and path.stat().st_size > 0, path
        data = path.read_bytes()
        manifest[name] = hashlib.sha256(data).hexdigest()
        bundle.writestr('BetterJoy-Direct/' + name, data)
    bundle.write(root / 'packages/WindowsInput.6.3.0/LICENSE.txt', 'BetterJoy-Direct/licenses/WindowsInput-LICENSE.txt')
    for name in ['Crc32.NET.1.2.0', 'JetBrains.Annotations.2020.1.0', 'Nefarius.ViGEm.Client.1.17.178']:
        for metadata in (root / 'packages' / name).glob('*.nuspec'):
            bundle.write(metadata, 'BetterJoy-Direct/licenses/' + metadata.name)
    bundle.writestr('BetterJoy-Direct/SHA256.json', json.dumps(manifest, indent=2, ensure_ascii=False))

with zipfile.ZipFile(archive) as bundle:
    assert bundle.testzip() is None
    for name, sha in manifest.items():
        assert hashlib.sha256(bundle.read('BetterJoy-Direct/' + name)).hexdigest() == sha
    bundle.extractall(out / 'package-verification')
print(json.dumps({'archive': str(archive), 'bytes': archive.stat().st_size, 'files_verified': len(manifest)}, ensure_ascii=False))
