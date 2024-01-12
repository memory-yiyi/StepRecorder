using StepRecorder.Core.Components;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            areaList = new AreaList();
            recordState = new RecordState();
            InitializeComponent();
            DrawArea.ItemsSource = areaList.AreaInfos;
            this.ShowInTaskbar = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            instance = null;
        }
        #endregion

        #region 区域绘制模块
        private readonly AreaList areaList;
        private Window? regionWindow;

        private void DrawRegion()
        {
            regionWindow = RegionSelection.GetInstance(areaList.AreaInfos[DrawArea.SelectedIndex].Rect);
            regionWindow.Show();
        }

        private void FixWindowPosition()
        {
            Rect currentScreen = areaList.AreaInfos[0].Rect;
            Rect regionWindow = areaList.AreaInfos[DrawArea.SelectedIndex].Rect;
            double reservedSpace = currentScreen.Height * 0.075;

            // 确定窗体左位置
            if (regionWindow.Right > currentScreen.Right)
            {
                this.Left = currentScreen.Right - this.Width;
            }
            else if (regionWindow.Left < currentScreen.Left)
            {
                this.Left = currentScreen.Left;
            }
            else
            {
                this.Left = regionWindow.Right - this.Width;
            }

            // 确定窗体顶位置
            if (regionWindow.Bottom < currentScreen.Bottom - reservedSpace)
            {
                this.Top = regionWindow.Bottom;
            }
            else if (regionWindow.Top > reservedSpace)
            {
                this.Top = regionWindow.Top - this.Height;
            }
            else
            {
                this.Left = regionWindow.Right - regionWindow.Width * 0.125 - this.Width;
                if (regionWindow.Top < currentScreen.Top || regionWindow.Top > currentScreen.Bottom - reservedSpace)
                {
                    this.Top = currentScreen.Top + 3;
                }
                else
                {
                    this.Top = regionWindow.Top + 3;
                }
            }

            this.Show();
            this.Activate();
        }

        private void DrawArea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            regionWindow?.Close();
            DrawRegion();
            FixWindowPosition();
            e.Handled = true;
        }
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
        private void RecordEntry(object sender, RoutedEventArgs e)
        {
            if (e.Source is FrameworkElement source)
            {
                string nextState = source.Name;
                recordState.ChangeCurrentState(nextState);
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
                if (nextState == "Note")
                {
                    SetButtonEnable(false, true, true, true);
                }
                e.Handled = true;
            }
        }
        #endregion

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && recordState.GetCurrentState() == "Stop")
            {
                Owner.Show();
                this.Close();
                regionWindow?.Close();
                e.Handled = true;
            }
        } 
    }
}
