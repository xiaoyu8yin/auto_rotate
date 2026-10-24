# 副屏旋转工具（AutoRotate）

适用于 Windows 10/11 x64 的轻量托盘工具，使用 C#、.NET 10 和 Windows Forms 实现，通过 Windows 显示设置 API 旋转副屏。

## 使用方式

1. 启动 `AutoRotate.exe`，在任务栏通知区域找到“副屏旋转”图标；图标可能收在隐藏图标区域。
2. 程序默认选择启动时检测到的第一个非主显示器。存在多个副屏时，右键图标，通过“选择副屏”切换目标。
3. 使用托盘左键、全局快捷键或右键菜单旋转所选副屏。
4. 右键选择“退出”结束程序，释放全局快捷键。

| 操作 | 菜单/代码中的预期行为 |
| --- | --- |
| 左键点击托盘图标 | 首次逆时针旋转 90°，下次顺时针旋转 90°，成功后交替方向 |
| `Ctrl+左箭头` | 顺时针旋转 90° |
| `Ctrl+右箭头` | 逆时针旋转 90° |
| 右键 → 顺时针/逆时针旋转 | 按菜单选定方向旋转 90° |
| 右键 → 开机自动启动 | 切换当前用户登录后自动启动 |

**已知问题：目前代码的顺逆时针计算与 Windows 官方方向定义相反，上表描述的是界面意图。另有显示设置结构布局问题，详见[代码检查记录](docs/repository-review.md)。本次整理保留原有运行逻辑，两项修复尚未应用。**

左键交替状态保存在内存中，快捷键和右键旋转不会重置该状态。程序没有传感器检测功能，物理转动显示器不会自动触发旋转。

## 开机自动启动

自启通过当前用户注册表项 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 中的 `AutoRotate` 值实现，不需要管理员权限。

先将程序放在固定目录，再启用自启。移动程序后，需在新位置重新启用自启以更新路径。所选副屏和左键交替状态不会跨运行保存。

## 构建和运行

需要 Windows 和 .NET 10 SDK。在仓库根目录运行：

```powershell
dotnet build -c Release
.\bin\Release\net10.0-windows\AutoRotate.exe
```

普通构建产物运行时需要 .NET 10 Desktop Runtime。发布为自包含版本后，目标电脑无需另外安装 .NET。

## 发布为可直接运行的单文件程序

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o release
```

最终程序位于 `release\AutoRotate.exe`。发布前应先处理下述代码检查记录中的问题，并在实际多显示器环境验证。

## 源码结构

```text
AutoRotate.csproj             Windows Forms 项目配置
Program.cs                    程序入口和消息循环
Displays/
  DisplayRotator.cs           副屏枚举、显示模式读取、旋转预检和应用、Win32 结构声明
Input/
  GlobalHotKeyWindow.cs       全局快捷键注册、消息处理和注销
Startup/
  StartupManager.cs          当前用户登录自启设置
UI/
  TrayContext.cs             托盘菜单、目标副屏选择、操作调度和错误提示
  TrayIcon.cs                运行时绘制托盘图标
```

旋转调用顺序：托盘或快捷键 → `TrayContext.RotateSelectedDisplay` → `DisplayRotator.Rotate` → 读取当前模式 → 计算目标方向并交换宽高 → Windows 预检 → 应用模式。

## GitHub 同类项目

已找到多个类似项目，检索日期为 **2026-10-24**：

| 项目 | 与本仓库的关系 |
| --- | --- |
| [secondary-screen-rotation](https://github.com/thiagosch/secondary-screen-rotation) | 最接近副屏双状态切换需求，支持托盘和可配置快捷键 |
| [TrueRotate](https://github.com/BK927/true-rotate) | C#/.NET 旋转工具，支持多屏、自定义快捷键、方向记忆；作者另提供 NVIDIA 鼠标坐标问题的处理 |
| [ScreenRotator](https://github.com/kame404/ScreenRotator) | C/Win32 实现，支持鼠标所在屏幕、托盘热键和命令行 |
| [QuickRotate](https://github.com/ArKT-7/QuickRotate) | C++/Win32 实现，支持托盘点击模式和命令行旋转 |
| [DisplaySwitcher](https://github.com/mscottsewell/DisplaySwitcher) | 保存并切换完整显示配置，包含方向、分辨率、布局等 |

比较与参考建议见 [GitHub 同类项目调研](docs/github-alternatives.md)。上述功能依据各项目公开文档整理，未安装或实测第三方程序。
