using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoRotate;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private string? selectedDisplay;
    private bool nextLeftRotationIsCounterClockwise = true;

    public TrayContext()
    {
        notifyIcon = new NotifyIcon
        {
            Icon = TrayIcon.Create(),
            Text = "副屏旋转",
            Visible = true
        };
        selectedDisplay = DisplayRotator.GetSecondaryDisplays().FirstOrDefault()?.DeviceName;
        notifyIcon.ContextMenuStrip = CreateMenu();
        notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                if (RotateSelectedDisplay(nextLeftRotationIsCounterClockwise))
                {
                    nextLeftRotationIsCounterClockwise = !nextLeftRotationIsCounterClockwise;
                }
            }
        };
    }

    private ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        var displays = DisplayRotator.GetSecondaryDisplays();

        if (displays.Count == 0)
        {
            menu.Items.Add("未检测到副屏").Enabled = false;
        }
        else
        {
            var displayMenu = new ToolStripMenuItem("选择副屏");
            foreach (var display in displays)
            {
                var item = new ToolStripMenuItem(display.FriendlyName)
                {
                    Checked = display.DeviceName == selectedDisplay,
                    Tag = display.DeviceName
                };
                item.Click += (_, _) =>
                {
                    selectedDisplay = (string)item.Tag!;
                    notifyIcon.ContextMenuStrip = CreateMenu();
                };
                displayMenu.DropDownItems.Add(item);
            }

            menu.Items.Add(displayMenu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("顺时针旋转 90°", null, (_, _) => RotateSelectedDisplay(false));
            menu.Items.Add("逆时针旋转 90°", null, (_, _) => RotateSelectedDisplay(true));
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private bool RotateSelectedDisplay(bool counterClockwise)
    {
        if (selectedDisplay is null)
        {
            notifyIcon.ShowBalloonTip(2500, "副屏旋转", "没有可旋转的副屏。", ToolTipIcon.Warning);
            return false;
        }

        var result = DisplayRotator.Rotate(selectedDisplay, counterClockwise);
        if (!result.Success)
        {
            notifyIcon.ShowBalloonTip(3000, "副屏旋转失败", result.ErrorMessage, ToolTipIcon.Error);
        }

        return result.Success;
    }

    protected override void ExitThreadCore()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}

internal static class TrayIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var monitorPen = new Pen(Color.FromArgb(20, 44, 76), 2.5f);
        using var screenBrush = new SolidBrush(Color.FromArgb(52, 177, 196));
        using var arrowPen = new Pen(Color.FromArgb(242, 137, 57), 2.7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.FillRoundedRectangle(screenBrush, new Rectangle(4, 5, 24, 16), new Size(3, 3));
        graphics.DrawRoundedRectangle(monitorPen, new Rectangle(4, 5, 24, 16), new Size(3, 3));
        graphics.DrawLine(monitorPen, 16, 21, 16, 26);
        graphics.DrawLine(monitorPen, 10, 27, 22, 27);
        graphics.DrawArc(arrowPen, new RectangleF(8, 7, 16, 16), 210, 205);
        graphics.DrawLine(arrowPen, 8.5f, 14, 8.5f, 19);
        graphics.DrawLine(arrowPen, 8.5f, 19, 13, 18);

        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}

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
