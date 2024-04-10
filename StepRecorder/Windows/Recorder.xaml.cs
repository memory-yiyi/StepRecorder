using Microsoft.Win32;
using StepRecorder.Core.Components;
using StepRecorder.Extensions;
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
            recordState = new RecordState(SendNoteContent, SendSaveInfo);
            InitializeComponent();
            DrawArea.ItemsSource = areaList.AreaInfos;
            this.ShowInTaskbar = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.TryShowOwner();
            regionWindow?.Close();
            instance = null;
            GC.Collect();
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
        private bool isSave;

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
                if (recordState.GetCurrentState() == "Stop")
                {
                    DrawArea.IsEnabled = false;
                    recordState.SetMouseNotRecordArea(new Rect(this.Left, this.Top, this.Width, this.Height));
                    recordState.SetRecordArea(new Rect(regionWindow!.Left, regionWindow.Top, regionWindow.Width, regionWindow.Height));
                }
                if (nextState == "Note" || nextState == "Stop")
                    SetButtonEnable(false, false, false, false);
                recordState.ChangeCurrentState(nextState);
                switch (nextState)
                {
                    case "Record":
                        SetButtonEnable(false, true, true, true);
                        break;
                    case "Pause":
                        SetButtonEnable(true, false, false, true);
                        break;
                    case "Note":
                        goto case "Record";
                    case "Stop":
                        if (isSave)
                        {
                            regionWindow?.Close();
                            this.Hide();
                            new Editor() { Owner = this }.Show();
                        }
                        else
                            this.Close();
                        break;
                }
                e.Handled = true;
            }
        }

        private static RecordState.NoteContent? SendNoteContent()
        {
            var noteWindow = new Notes();
            if (noteWindow.ShowDialog() == true)
                return new RecordState.NoteContent(noteWindow.Short.Text, noteWindow.Detail.Text);
            else
                return null;
        }

        private string? SendSaveInfo()
        {
            // COPY_SaveFile(1)
            var saveFileDialog = new SaveFileDialog
            {
                Title = (string)Application.Current.Resources["S.Share.SaveFileDialog.Title"],
                DefaultDirectory = SavePath.DefaultOutputDirectory,
                DefaultExt = ".strcd",
                FileName = SavePath.DefaultOutputPathPrefix![(SavePath.DefaultOutputPathPrefix!.LastIndexOf('\\') + 1)..],
                Filter = $"{(string)Application.Current.Resources["S.Share.FileDialog.Filter.STRCD"]} (*.strcd)|*.strcd|{(string)Application.Current.Resources["S.Share.FileDialog.Filter.All"]} (*.*)|*.*"
            };
            // endCOPY_SaveFile
            if (saveFileDialog.ShowDialog() == true)
            {
                isSave = true;
                return saveFileDialog.FileName;
            }
            else
            {
                isSave = false;
                return null;
            }
        }

        internal ProjectFile GetProjectFile() => recordState.ProjectFile!;
        internal Task GetSaveTask() => recordState.SaveTask!;
        internal double GetSaveProgress() => recordState.GetSaveProgress();
        #endregion

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && recordState.GetCurrentState() == "Stop")
            {
                this.Close();
                e.Handled = true;
            }
        }
    }
}
