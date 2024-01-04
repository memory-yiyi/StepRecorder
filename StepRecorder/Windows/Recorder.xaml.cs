using StepRecorder.Core.Components;
using System.Windows;

namespace StepRecorder.Windows
{
    /// <summary>
    /// 记录器
    /// </summary>
    public partial class Recorder : Window
    {
        #region 单例模式
        /*
         * 请保持懒汉式单例模式，不要改为饿汉式
         * 否则程序在关闭该窗体（释放该静态对象）后无法继续使用
         */
        private static readonly object instanceLock = new();
        private static Recorder? instance;
        public static Recorder GetInstance()
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    instance ??= new Recorder();
                }
            }
            return instance;
        }
        private Recorder()
        {
            recordState = new RecordState();
            InitializeComponent();
        }
        #endregion

        #region 区域绘制模块
        

        #endregion

        #region 录制模块
        private readonly RecordState recordState;

        private void SetButtonEnable(bool record, bool pause, bool note, bool stop)
        {
            Record.IsEnabled = record;
            Pause.IsEnabled = pause;
            Note.IsEnabled = note;
            Stop.IsEnabled = stop;
        }

        /// <summary>
        /// 录制入口函数
        /// </summary>
        public void RecordEntry(object sender, RoutedEventArgs e)
        {
            if (e.Source is FrameworkElement source)
            {
                string nextState = source.Name;
                switch (nextState)
                {
                    case "Record":
                        if (DrawArea.IsEnabled) DrawArea.IsEnabled = false;
                        SetButtonEnable(false, true, true, true);
                        break;
                    case "Pause":
                        SetButtonEnable(true, false, false, true);
                        break;
                    case "Note":
                        SetButtonEnable(false, false, false, false);
                        break;
                    case "Stop":
                        SetButtonEnable(false, false, false, false);
                        break;
                }
                recordState.ChangeCurrentState(nextState);
                if (nextState == "Note")
                {
                    SetButtonEnable(false, true, true, true);
                }
                e.Handled = true;
            }
        }
        #endregion
    }
}
