using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace StepRecorder.Core.Components
{
    internal class ProcessInfo : AreaInfo
    {
        public override Rect Rect => GetWindowRect(mainWindowHandle, out WinRect rect) ? new Rect(rect.Left / scaling, rect.Top / scaling, (rect.Right - rect.Left) / scaling, (rect.Bottom - rect.Top) / scaling) : new Rect();

        private readonly nint mainWindowHandle;
        private readonly double scaling;
        public ProcessInfo(string name, string description, IntPtr mainWindowHandle) : base(name, description)
        {
            this.mainWindowHandle = mainWindowHandle;
            #region 获取当前屏幕缩放比例
            // 通过程序自身获得屏幕缩放比例，省去了冗杂的WinAPI调用
            GetWindowRect(Process.GetCurrentProcess().MainWindowHandle, out WinRect rect);
            scaling = (rect.Right - rect.Left) / (double)Application.Current.Resources["D.Main.Width"];
            #endregion
        }

        private struct WinRect
        {
            public int Left {  get; set; }
            public int Top {  get; set; }
            public int Right {  get; set; }
            public int Bottom {  get; set; }
        }
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out WinRect rect);
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);
    }
}
