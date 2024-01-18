using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace StepRecorder.Core.Components
{
    internal class ProcessInfo(string name, string description, IntPtr mainWindowHandle) : AreaInfo(name, description)
    {
        public override Rect Rect => GetWindowRect(mainWindowHandle, out WinRect rect) ? new Rect(rect.Left / Scaling, rect.Top / Scaling, (rect.Right - rect.Left) / Scaling, (rect.Bottom - rect.Top) / Scaling) : new Rect();

        /*
         * 你是否疑惑它为什么不在隔壁的 ScreenInfo 里？
         * 哦，这里方便，而且降低了 GetWindowRect 的可访问性
         * 如果你能在其它地方用到这个函数，那再考虑一下
         */
        internal static double Scaling { get; }

        /*
         * 知道为什么分辨率和缩放比例改变时，有些程序需要重启自己才生效了？
         * 你可以把它放构造函数里，这样就不用重启了，但会增大开销
         */
        static ProcessInfo()
        {
            #region 获取当前屏幕缩放比例
            // 通过程序自身获得屏幕缩放比例，省去了冗杂的WinAPI调用
            GetWindowRect(Process.GetCurrentProcess().MainWindowHandle, out WinRect rect);
            Scaling = (rect.Right - rect.Left) / (double)Application.Current.Resources["D.Main.Width"];
            #endregion
        }

        private struct WinRect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out WinRect rect);
    }
}
