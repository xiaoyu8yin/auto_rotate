# 仓库整理与代码检查

整理日期：2026-10-24。

## 本次整理范围

原仓库将程序入口、托盘、快捷键、自启、图标绘制及显示器 API 全部放在 `Program.cs`。本次按职责拆分为 `Displays`、`Input`、`Startup` 和 `UI` 四个目录，保留 `AutoRotate` 命名空间、项目配置和原有运行逻辑。

补充了使用方式、构建发布、源码职责、已知限制和 GitHub 同类项目对比。功能修复补丁被工具返回“用户拒绝”，因此未应用修复，也未加入对应的回归检查项目。

## 当前实现

- **托盘和消息循环**：`Program` 启动 `TrayContext`，由 `NotifyIcon` 提供菜单与左键操作。
- **目标选择**：从 `Screen.AllScreens` 排除主屏，默认使用第一个副屏，选择结果仅保存在当前进程。
- **快捷键**：隐藏原生窗口调用 `RegisterHotKey` 注册 `Ctrl+左/右箭头`，通过 `WM_HOTKEY` 分发动作，退出时注销。
- **旋转**：`EnumDisplaySettings` 读取当前显示模式，每次相对旋转 90°；横竖状态变化时交换宽高；先用 `CDS_TEST` 预检，再调用 `ChangeDisplaySettingsEx` 应用。
- **自启**：读写当前用户注册表 `Run` 项中的 `AutoRotate` 值。
- **发布**：.NET 10 Windows Forms、x64，可发布为自包含单文件程序。

## 已确认的待修复问题

### 1. 顺逆时针计算与 Windows 定义相反

位置：`Displays/DisplayRotator.cs` 的 `Rotate` 方法。

当前代码在 `counterClockwise == true` 时使用 `(orientation + 3) % 4`，否则使用 `(orientation + 1) % 4`。Windows 官方规定 `DMDO_90`、`DMDO_180`、`DMDO_270` 按逆时针衡量，因此逆时针应增加 1，顺时针应增加 3，再对 4 取模。

影响：界面所标示的方向与 API 实际请求的方向相反；托盘左键首次方向也受影响。

建议验证：覆盖四种起始方向下的顺、逆时针共八种转换，再验证相反方向往返复原。

依据：[Microsoft DEVMODEW：dmDisplayOrientation](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-devmodew)。

### 2. DEVMODEW 联合字段被重复排列

位置：`Displays/DisplayRotator.cs` 中的 `DevMode` 结构。

原生 `DEVMODEW` 的 `dmDisplayFlags` 和 `dmNup` 属于同一联合体，共用 4 字节；当前 C# 顺序布局把它们声明成相邻的两个 `int`。

影响：当前结构大小为 224 字节，原生布局应为 220 字节；`dmDisplayFrequency` 的偏移变为 188，而原生定义应为 184。后续字段均随之偏移，向 Windows 传递的 `dmSize` 也不正确。是否直接导致旋转失败，仍取决于系统和驱动，尚未实测。

建议：显示器用途只保留 `dmDisplayFlags` 这一共享字段，或显式定义联合体；补充结构大小及关键字段偏移检查。

依据：[Microsoft DEVMODEW 结构定义](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-devmodew)。

## 后续可改善项

以下来自静态代码检查，不表示已在当前机器复现：

| 项目 | 当前情况 | 改善方向 |
| --- | --- | --- |
| 显示器热插拔 | 菜单建立后不会主动随显示器变动刷新 | 打开菜单或收到显示变更消息时重新枚举 |
| 主副屏变更 | 仅枚举时排除主屏，旋转前不重新校验目标角色 | 操作前确认目标仍是当前副屏 |
| 配置保存 | 所选副屏及交替状态只存在内存中 | 保存稳定设备标识和用户设置 |
| 快捷键 | 固定使用 `Ctrl+左/右箭头`，会占用常见文本导航组合 | 提供自定义绑定及冲突提示 |
| 启动冲突 | 注册快捷键失败时构造函数抛异常，入口没有处理 | 清晰提示冲突，并允许保留托盘操作 |
| 重复运行 | 没有单实例保护 | 防止重复启动引发快捷键竞争 |
| 返回状态 | 将需要重启的返回值也计为旋转成功 | 区分已应用与需要重启，避免错误推进交替状态 |

## 验证范围

拆分前后均执行 `dotnet build -c Release --no-restore`，结果为 0 个警告、0 个错误。另对拆分前后的类型正文进行一致性核对，确认本次只调整文件位置。

未启动托盘程序、未注册快捷键、未更改当前显示器方向、未修改登录自启设置，也未安装第三方程序。构建通过不能替代真实多屏环境下的方向、驱动兼容性及热插拔测试。
