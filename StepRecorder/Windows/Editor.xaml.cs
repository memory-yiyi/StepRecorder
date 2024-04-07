using Microsoft.Win32;
using StepRecorder.Core.Components;
using StepRecorder.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StepRecorder.Windows
{
    /// <summary>
    /// 编辑器
    /// </summary>
    public partial class Editor : Window
    {
        public Editor()
        {
            InitializeComponent();
            timer.Tick += Timer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Owner is Recorder recorder)
            {
                Task.Run(() =>
                {
                    this.Dispatcher.Invoke(() => DataContext = recorder.GetProjectFile());

                    while (true)
                    {
                        Progress.Dispatcher.Invoke(() => Progress.Value = recorder.GetSaveProgress());
                        CurrentStatus.Dispatcher.Invoke(() => CurrentStatus.Text = $"正在保存...{Progress.Value:P1}");
                        if (Progress.Dispatcher.Invoke(() => Progress.Value == 1d))
                            break;
                        Thread.Sleep(1000);     // 检测间隔
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        Window owner = Owner;
                        Owner = owner.Owner;
                        owner.Close();
                    });

                    CurrentStatus.Dispatcher.Invoke(() => CurrentStatus.Text = "保存完成");
                });
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((ProjectFile)DataContext).Dispose();
            this.TryShowOwner();
            GC.Collect();
        }

        #region 菜单
        private bool flagLoading = true;

        private void File_Open(object sender, RoutedEventArgs e)
        {
            flagLoading = true;
            var openFileDialog = new OpenFileDialog
            {
                Title = (string)Application.Current.Resources["S.Share.OpenFileDialog.Title"],
                DefaultDirectory = SavePath.DefaultOutputDirectory,
                DefaultExt = ".strcd",
                Filter = $"{(string)Application.Current.Resources["S.Share.FileDialog.Filter.STRCD"]} (*.strcd)|*.strcd|{(string)Application.Current.Resources["S.Share.FileDialog.Filter.All"]} (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ProjectFile pf = new(openFileDialog.FileName);
                DataContext = pf;

                StatusBar.IsEnabled = true;
                Media.IsEnabled = true;
                OperateInfo.ItemsSource = pf.GetKeyframeInfo();
                FrameAt(pf.CurrentFrameIndex);
            }
            flagLoading = false;
        }
        #endregion

        #region 导航区
        private bool flagShortNote = false;
        private bool flagDetailNote = false;
        private bool flagJumpFrame = true;

        private void OperateInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flagLoading)
                return;

            Note_LostFocus();

            if (OperateInfo.SelectedItem is KeyframeInfo kfi)
            {
            ShortNote.Text = kfi.ShortNote;
            DetailNote.Text = kfi.DetailNote;

            if (flagJumpFrame)
                FrameAt(kfi.FrameIndex);
        }
            else
            {
                ShortNote.Text = string.Empty;
                DetailNote.Text = string.Empty;
            }
        }

        private void Note_GotFocus() => UpdateNote.IsEnabled = true;

        private void Note_LostFocus()
        {
            UpdateNote.IsEnabled = false;
            flagShortNote = false;
            flagDetailNote = false;
        }

        private void ShortNote_GotFocus(object sender, RoutedEventArgs e)
        {
            Note_GotFocus();
            flagShortNote = true;
        }

        private void DetailNote_GotFocus(object sender, RoutedEventArgs e)
        {
            Note_GotFocus();
            flagDetailNote = true;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (flagShortNote)
            {
                ((KeyframeInfo)OperateInfo.SelectedItem).ShortNote = ShortNote.Text;
                flagShortNote = false;
            }
            if (flagDetailNote)
            {
                ((KeyframeInfo)OperateInfo.SelectedItem).DetailNote = DetailNote.Text;
                flagDetailNote = false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (flagShortNote)
            {
                ShortNote.Text = ((KeyframeInfo)OperateInfo.SelectedItem).ShortNote;
                flagShortNote = false;
            }
            if (flagDetailNote)
            {
                DetailNote.Text = ((KeyframeInfo)OperateInfo.SelectedItem).DetailNote;
                flagDetailNote = false;
            }
        }
        #endregion

        #region 多媒体区
        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(125) };        // 对于时间间隔如果有需要，请调整录制工具（RecordTool.cs）并进行调用

        private void FrameAt(int frameIndex)
        {
            ProjectFile pf = (ProjectFile)DataContext;
            Screen.Source = pf.FrameAt(frameIndex);
            int cfi = pf.CurrentFrameIndex;
            int fc = pf.FrameCount - 1;
            Progress.Value = (double)cfi / fc;
            CurrentStatus.Text = $"{cfi}/{fc}";
        }

        private void FrameNext()
        {
            flagJumpFrame = false;
            ProjectFile pf = (ProjectFile)DataContext;
            int cfi = pf.CurrentFrameIndex + 1;
            if (OperateInfo.SelectedIndex + 1 < OperateInfo.Items.Count && ((KeyframeInfo)OperateInfo.Items[OperateInfo.SelectedIndex + 1]).FrameIndex == cfi)
                ++OperateInfo.SelectedIndex;
            FrameAt(cfi);
            if (pf.CurrentFrameIndex == pf.FrameCount - 1)
                timer.Stop();
            flagJumpFrame = true;
        }

        private void PreviousKeyframe_Click(object sender, RoutedEventArgs e) => OperateInfo.SelectedIndex = OperateInfo.SelectedIndex <= 0 ? 0 : --OperateInfo.SelectedIndex;

        private void NextKeyframe_Click(object sender, RoutedEventArgs e) => OperateInfo.SelectedIndex = OperateInfo.SelectedIndex >= OperateInfo.Items.Count ? OperateInfo.Items.Count : ++OperateInfo.SelectedIndex;

        private void PreviousFrame_Click(object sender, RoutedEventArgs e)
        {
            int cfi = ((ProjectFile)DataContext).CurrentFrameIndex - 1;
            if (OperateInfo.SelectedIndex - 1 > 0 && ((KeyframeInfo)OperateInfo.Items[OperateInfo.SelectedIndex - 1]).FrameIndex == cfi)
                --OperateInfo.SelectedIndex;
            FrameAt(cfi);       // 如果有需要，请调整解码器（GifDecoder.cs），使其保留两个向后的缓存
        }

        private void NextFrame_Click(object sender, RoutedEventArgs e) => FrameNext();

        private void Timer_Tick(object? sender, EventArgs e) => FrameNext();

        private void Play_Click(object sender, RoutedEventArgs e) => timer.Start();

        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            OperateInfo.SelectedIndex = -1;
            FrameAt(0);
            timer.Start();
        }

        private void Pause_Click(object sender, RoutedEventArgs e) => timer.Stop();

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            flagJumpFrame = false;
            timer.Stop();
            OperateInfo.SelectedIndex = OperateInfo.Items.Count - 1;
            FrameAt(((ProjectFile)DataContext).FrameCount);
            flagJumpFrame = true;
        }
        #endregion
    }
}
