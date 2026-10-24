using System.Windows.Forms;

namespace AutoRotate;

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private readonly GlobalHotKeyWindow hotKeyWindow;
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
        hotKeyWindow = new GlobalHotKeyWindow
        {
            OnClockwise = () => RotateSelectedDisplay(false),
            OnCounterClockwise = () => RotateSelectedDisplay(true)
        };
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
        var startupItem = new ToolStripMenuItem("开机自动启动")
        {
            Checked = StartupManager.IsEnabled()
        };
        startupItem.Click += (_, _) =>
        {
            try
            {
                StartupManager.SetEnabled(!startupItem.Checked);
                startupItem.Checked = StartupManager.IsEnabled();
            }
            catch (Exception exception)
            {
                notifyIcon.ShowBalloonTip(3000, "开机自动启动设置失败", exception.Message, ToolTipIcon.Error);
            }
        };
        menu.Items.Add(startupItem);
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
        hotKeyWindow.Dispose();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
