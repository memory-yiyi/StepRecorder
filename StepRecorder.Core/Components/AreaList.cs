using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 前台进程信息列表
    /// </summary>
    public class AreaList
    {
        public AreaList() => GetAreaList();

        public List<AreaInfo> AreaInfos { get; } = [];
        private void GetAreaList()
        {
            AreaInfos.Add(new ScreenInfo((string)Application.Current.Resources["S.Recorder.DrawArea.FullScreen"], ""));
            foreach (Process p in Process.GetProcesses())
            {
                if (p.MainWindowHandle != IntPtr.Zero && !IsIconic(p.MainWindowHandle)) 
                {
                    AreaInfos.Add(new ProcessInfo(p.ProcessName, p.MainWindowTitle, p.MainWindowHandle));
                }
            }
        }

        // 此代码用于强制在启动程序时执行 ProcessInfo 类的静态构造函数
        // 如果获取屏幕缩放比例的方式发生变化，请随之更改
        public static void GetScreenScaling() => _ = new ProcessInfo("", "", 0);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}
