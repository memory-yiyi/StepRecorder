using System.Diagnostics;
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
                if (p.MainWindowHandle != IntPtr.Zero && !ProcessInfo.IsIconic(p.MainWindowHandle)) 
                {
                    AreaInfos.Add(new ProcessInfo(p.ProcessName, p.MainWindowTitle, p.MainWindowHandle));
                }
            }
        }
    }
}
