using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoRotate;

internal sealed class GlobalHotKeyWindow : NativeWindow, IDisposable
{
    private const int ModControl = 0x0002;
    private const int WmHotKey = 0x0312;
    private const int HotKeyClockwise = 1;
    private const int HotKeyCounterClockwise = 2;
    private const int VkLeft = 0x25;
    private const int VkRight = 0x27;

    public Action? OnClockwise { get; init; }
    public Action? OnCounterClockwise { get; init; }

    public GlobalHotKeyWindow()
    {
        CreateHandle(new CreateParams());
        if (!RegisterHotKey(Handle, HotKeyClockwise, ModControl, VkLeft)
            || !RegisterHotKey(Handle, HotKeyCounterClockwise, ModControl, VkRight))
        {
            var error = Marshal.GetLastWin32Error();
            UnregisterHotKey(Handle, HotKeyClockwise);
            UnregisterHotKey(Handle, HotKeyCounterClockwise);
            DestroyHandle();
            throw new InvalidOperationException($"无法注册屏幕旋转快捷键，Windows 错误代码：{error}。");
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotKey)
        {
            switch (message.WParam.ToInt32())
            {
                case HotKeyClockwise:
                    OnClockwise?.Invoke();
                    break;
                case HotKeyCounterClockwise:
                    OnCounterClockwise?.Invoke();
                    break;
            }
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            UnregisterHotKey(Handle, HotKeyClockwise);
            UnregisterHotKey(Handle, HotKeyCounterClockwise);
            DestroyHandle();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
