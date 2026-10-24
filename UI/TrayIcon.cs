using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AutoRotate;

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
