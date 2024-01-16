using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StepRecorder.Windows
{
    /// <summary>
    /// 区域选择窗体
    /// </summary>
    public partial class RegionSelection : Window
    {
        #region 单例模式
        private static readonly object instanceLock = new();
        private static RegionSelection? instance;
        public static RegionSelection GetInstance(Rect rect)
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new RegionSelection
                        {
                            Left = rect.Left,
                            Top = rect.Top,
                            Height = rect.Height,
                            Width = rect.Width
                        };
                    }
                }
            }
            return instance;
        }
        private RegionSelection()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            instance = null;
        }
        #endregion

        #region 将窗体从'Alt+Tab'中去除
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            _ = SetWindowLongPtr(hWnd, GWL_EXSTYLE, GetWindowLongPtr(hWnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
            e.Handled = true;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_APPWINDOW = 0x40000;
        private const int WS_EX_TOOLWINDOW = 0x80;
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        #endregion
    }
}
