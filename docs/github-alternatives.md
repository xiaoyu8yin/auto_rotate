# GitHub 同类项目调研

检索日期：**2026-10-24**。

范围：Windows 屏幕显示方向旋转，优先比较副屏、托盘与快捷键工具。依据项目公开 README 和仓库页面整理，未安装、编译或实测第三方工具；项目自述的驱动修复和兼容性没有独立验证。下文的“参考建议”是基于功能匹配程度的判断。

## 结论

GitHub 上已有类似项目。与本仓库“选定副屏，通过托盘和快捷键来回旋转”最接近的是 [thiagosch/secondary-screen-rotation](https://github.com/thiagosch/secondary-screen-rotation)。如果继续使用 C#/.NET 开发，优先阅读 [BK927/true-rotate](https://github.com/BK927/true-rotate) 的热键、显示服务及设置实现。

本仓库的现有特点是：目标范围限定为副屏选择列表、左键成功后交替旋转方向、快捷键固定、无传感器依赖。这一定位可以保留，但应先处理[代码检查记录](repository-review.md)中的两处已确认问题。

## 功能对比

| 项目 | 技术/形式 | 旋转及操作 | 参考建议 |
| --- | --- | --- | --- |
| [secondary-screen-rotation](https://github.com/thiagosch/secondary-screen-rotation) | Python；使用 infi.systray 和 rotate-screen | 副屏在两个配置方向之间切换；托盘；设置界面可改切换方向和快捷键 | 功能最接近，重点参考双状态切换和设置界面 |
| [TrueRotate](https://github.com/BK927/true-rotate) | C#/.NET 10 | 0/90/180/270°；托盘；可重绑定热键；按鼠标所在屏、主屏或全部屏幕操作；方向记忆；自启 | 同技术栈参考；其 CCD 路径及 NVIDIA 处理值得单独验证 |
| [ScreenRotator](https://github.com/kame404/ScreenRotator) | C/Win32，无独立框架运行时依赖 | Ctrl+Alt+方向键；常驻时操作鼠标所在屏；CLI 支持指定屏幕和方向 | 适合参考小型原生工具及脚本入口 |
| [QuickRotate](https://github.com/ArKT-7/QuickRotate) | C++/Win32/GDI+ | 四种固定方向；托盘点击可选横竖切换或循环；命令行；自启 | 参考托盘点击模式和固定方向入口；README 未明确副屏选择行为，需进一步核实 |
| [DisplaySwitcher](https://github.com/mscottsewell/DisplaySwitcher) | C#/.NET 9 | 保存方向、分辨率、布局、刷新率、缩放等配置；通过托盘或全局热键切换 | 适合需求扩大为整套显示布局切换时参考 |

以上每行的项目链接均为对应功能说明的原始来源。

## 重点项目说明

### secondary-screen-rotation：最贴近副屏需求

README 明确说明用途是让第二显示器在两个旋转状态间切换，设置界面可以修改切换位置和按键，并可通过重置动作或双击托盘图标设定基准状态。仓库将设置、快捷键、托盘和旋转拆为单独文件，与本次源码整理的方向一致。

来源：[项目 README 与文件列表](https://github.com/thiagosch/secondary-screen-rotation)。

### TrueRotate：适合 C# 路线继续开发

项目提供可配置的全局热键、多显示器目标策略、稳定设备标识对应的方向记忆、登录自启，以及监测方向变化后重新应用的功能。其 README 还描述通过 CCD API 和 `SDC_FORCE_MODE_ENUMERATION` 处理某些 NVIDIA 环境旋转后的鼠标坐标异常；这是作者的实现说明，需在目标硬件验证。

使用其自动恢复功能时，外部工具或 Windows 设置产生的方向变化可能被恢复；README 提供关闭 Auto-reapply 的说明。

来源：[TrueRotate 功能与技术说明](https://github.com/BK927/true-rotate)。

### ScreenRotator 和 QuickRotate：原生与命令行路线

ScreenRotator 无参数时常驻托盘并根据鼠标位置选择屏幕；命令行模式可用 `/display:N` 指定显示器，执行后退出。QuickRotate 提供 `0/90/180/270/next` 等命令行操作，并支持配置托盘左键的切换行为。

如未来希望从脚本、桌面快捷方式或外部按键设备触发旋转，可参考这两个项目的命令行接口。

来源：[ScreenRotator 使用说明](https://github.com/kame404/ScreenRotator)、[QuickRotate 使用说明](https://github.com/ArKT-7/QuickRotate)。

### DisplaySwitcher：完整显示配置方案

该项目以保存并恢复显示配置为核心，旋转只是配置的一部分。README 描述其分辨率、方向和布局同样使用 `EnumDisplayDevices`、`EnumDisplaySettings`、`ChangeDisplaySettingsEx`，适合参考配置保存及多显示器配置切换。

来源：[DisplaySwitcher 功能和实现说明](https://github.com/mscottsewell/DisplaySwitcher)。

## 名称相近但需求不同

[yeokm1/auto-rotate-screen](https://github.com/yeokm1/auto-rotate-screen) 主要利用 ThinkPad Active Protection System 的传感器数据自动旋转；没有相应传感器时，也可以作为托盘旋转入口。它更适合研究物理姿态检测，不是本仓库手动副屏切换的首选对标。

## 对本仓库的后续建议

1. 先修复方向映射和原生结构布局，并验证四种方向之间的转换。
2. 加入显示器列表刷新、操作前副屏校验和目标选择保存。
3. 将固定快捷键改为可配置，提供冲突提示。
4. 若主要需求是横竖来回切换，可保存明确的两个目标方向，避免快捷键或外部设置改变当前方向后，交替逻辑偏离用户预期。
5. 只有在确认需要时，再扩展命令行、鼠标所在屏幕或整套布局预设。

未按星数、维护活跃度或下载量排序；功能相近不表示质量或兼容性已经验证。
