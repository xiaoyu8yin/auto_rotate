using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoRotate;

internal sealed record DisplayInfo(string DeviceName, string FriendlyName);
internal sealed record RotationResult(bool Success, string ErrorMessage = "");

internal static class DisplayRotator
{
    private const int EnumCurrentSettings = -1;
    private const int DisplayDeviceAttachedToDesktop = 0x00000001;
    private const int DisplayDevicePrimaryDevice = 0x00000004;
    private const int DispChangeSuccessful = 0;
    private const int DispChangeRestart = 1;
    private const int DispChangeBadMode = -2;
    private const int DispChangeFailed = -1;
    private const uint CdsTest = 0x00000002;
    private const int DmDisplayOrientation = 0x00000080;
    private const int DmPelsWidth = 0x00080000;
    private const int DmPelsHeight = 0x00100000;

    public static List<DisplayInfo> GetSecondaryDisplays()
    {
        var displays = new List<DisplayInfo>();
        foreach (var screen in Screen.AllScreens.Where(item => !item.Primary))
        {
            var monitor = new DisplayDevice { Size = Marshal.SizeOf<DisplayDevice>() };
            var monitorName = $"显示器 {screen.DeviceName}";
            if (EnumDisplayDevices(screen.DeviceName, 0, ref monitor, 0))
            {
                var friendlyName = monitor.DeviceString.TrimEnd('\0');
                if (!string.IsNullOrWhiteSpace(friendlyName))
                {
                    monitorName = friendlyName;
                }
            }

            var resolution = $"{screen.Bounds.Width}x{screen.Bounds.Height}";
            displays.Add(new DisplayInfo(
                screen.DeviceName,
                $"{monitorName} ({resolution}, {screen.DeviceName})"));
        }

        return displays;
    }

    public static RotationResult Rotate(string deviceName, bool counterClockwise)
    {
        var mode = new DevMode
        {
            dmSize = (short)Marshal.SizeOf<DevMode>(),
            dmDeviceName = string.Empty,
            dmFormName = string.Empty
        };
        if (!EnumDisplaySettings(deviceName, EnumCurrentSettings, ref mode))
        {
            return new RotationResult(false, "无法读取副屏当前显示设置。");
        }

        var nextOrientation = counterClockwise
            ? (mode.dmDisplayOrientation + 3) % 4
            : (mode.dmDisplayOrientation + 1) % 4;
        var currentPortrait = mode.dmDisplayOrientation is 1 or 3;
        var nextPortrait = nextOrientation is 1 or 3;
        if (currentPortrait != nextPortrait)
        {
            (mode.dmPelsWidth, mode.dmPelsHeight) = (mode.dmPelsHeight, mode.dmPelsWidth);
        }

        mode.dmDisplayOrientation = nextOrientation;
        mode.dmFields = DmDisplayOrientation | DmPelsWidth | DmPelsHeight;
        var testResult = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CdsTest, IntPtr.Zero);
        if (testResult != DispChangeSuccessful)
        {
            return new RotationResult(false, testResult == DispChangeBadMode
                ? "显示器驱动不支持这个旋转方向或对应的宽高模式。"
                : $"Windows 预检显示设置失败，错误代码：{testResult}。");
        }

        var result = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, 0, IntPtr.Zero);
        return result switch
        {
            DispChangeSuccessful or DispChangeRestart => new RotationResult(true),
            DispChangeBadMode => new RotationResult(false, "显示器不支持这个旋转方向或分辨率。"),
            DispChangeFailed => new RotationResult(false, "Windows 拒绝了显示设置变更。"),
            _ => new RotationResult(false, $"显示设置变更失败，错误代码：{result}。")
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DisplayDevice displayDevice, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DevMode devMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, ref DevMode devMode, IntPtr hwnd, uint flags, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmNup;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
}
