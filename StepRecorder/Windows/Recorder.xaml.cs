using StepRecorder.Components;
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

        private RecordState recordState;

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
                switch (source.Name)
                {
                    case "Record":
                        SetButtonEnable(false, true, true, true);
                        // 根据推算，下面两个都可以，任选其一便可
                        recordState.ChangeCurrentState(false);
                        // recordState.ChangeCurrentState(null);
                        break;
                    case "Pause":
                        SetButtonEnable(true, false, false, true);
                        recordState.ChangeCurrentState(false);
                        break;
                    case "Note":
                        SetButtonEnable(false, false, false, false);
                        recordState.ChangeCurrentState(null);
                        SetButtonEnable(false, true, true, true);
                        break;
                    case "Stop":
                        SetButtonEnable(false, false, false, false);
                        recordState.ChangeCurrentState(true);
                        break;
                }
            }
        }
    }
}
