# 副屏旋转工具

适用于 Windows 10/11 的轻量托盘工具：

- 左键点击托盘图标：第一次逆时针旋转 90 度，下一次顺时针旋转 90 度，交替执行。
- 全局快捷键：`Ctrl+左箭头` 顺时针旋转 90 度，`Ctrl+右箭头` 逆时针旋转 90 度。
- 右键托盘图标：选择副屏、顺时针/逆时针旋转、设置开机自动启动或退出。
- 只操作非主显示器；开机自启使用当前用户的启动项，不需要管理员权限。

## 构建

需要 .NET 10 SDK。在 PowerShell 中运行：

```powershell
dotnet build -c Release
```

## 发布为可直接运行的单文件程序

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o release
```

最终程序位于 `release\AutoRotate.exe`，可直接双击运行，无需安装 .NET。
