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
                flagLoading = true;
                File.IsEnabled = false;
                DataContext = recorder.GetProjectFile();
                OperateInfo.ItemsSource = ((ProjectFile)DataContext).GetKeyframeInfo();
                Task.Run(() =>
                {
                    while (true)
                    {
                        SB_Progress.Dispatcher.Invoke(() => SB_Progress.Value = recorder.GetSaveProgress());
                        SB_CurrentStatus.Dispatcher.Invoke(() => SB_CurrentStatus.Text = $"正在保存...{SB_Progress.Value:P1}");
                        if (SB_Progress.Dispatcher.Invoke(() => SB_Progress.Value == 1d))
                            break;
                        Thread.Sleep(1000);     // 检测间隔
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        Window owner = Owner;
                        Owner = owner.Owner;
                        owner.Close();
                    });

                    SB_CurrentStatus.Dispatcher.Invoke(() => SB_CurrentStatus.Text = "保存完成，3s 后自动跳转");

                    Thread.Sleep(3000);

                    this.Dispatcher.Invoke(() =>
                    {
                        ProjectFile pf = (ProjectFile)DataContext;
                        SB_MediaControl.IsEnabled = true;
                        SB_AlterNote.IsEnabled = true;
                        FrameAt(((ProjectFile)DataContext).CurrentFrameIndex);
                        File.IsEnabled = true;
                        flagLoading = false;
                    });
                });
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((ProjectFile)DataContext)?.Dispose();
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

                SB_MediaControl.IsEnabled = true;
                SB_AlterNote.IsEnabled = true;
                ShortNote.Text = string.Empty;
                DetailNote.Text = string.Empty;
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
            if (flagJumpFrame)
            {
                if (OperateInfo.SelectedItem is KeyframeInfo kfi)
                {
                    ShortNote.Text = kfi.ShortNote;
                    DetailNote.Text = kfi.DetailNote;
                    if (!flagLoading)
                        FrameAt(kfi.FrameIndex);
                }
                else
                {
                    ShortNote.Text = string.Empty;
                    DetailNote.Text = string.Empty;
                }
            }
        }

        private void NoteFlush(bool flagNoteFocus = false)
        {
            if (OperateInfo.SelectedIndex == -1)
                return;
            if (((ProjectFile)DataContext).CurrentFrameIndex == ((KeyframeInfo)OperateInfo.SelectedItem).FrameIndex || flagLoading)
            {
                ShortNote.Text = ((KeyframeInfo)OperateInfo.SelectedItem).ShortNote;
                DetailNote.Text = ((KeyframeInfo)OperateInfo.SelectedItem).DetailNote;
                if (flagNoteFocus || flagLoading)
                {
                    flagShortNote = false;
                    flagDetailNote = false;
                    SB_UpdateNote.Visibility = Visibility.Visible;
                }
            }
        }

        private void Note_GotFocus(object sender, RoutedEventArgs e) => NoteFlush(true);

        private void ShortNote_TextChanged(object sender, TextChangedEventArgs e) => flagShortNote = true;

        private void DetailNote_TextChanged(object sender, TextChangedEventArgs e) => flagDetailNote = true;

        private void Note_LostFocus(object sender, RoutedEventArgs e) => SB_UpdateNote.Visibility = Visibility.Hidden;

        private void SB_UpdateNote_GotFocus(object sender, RoutedEventArgs e)
        {
            if (flagShortNote || flagDetailNote)
                SB_UpdateNote.Visibility = Visibility.Visible;
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
            SB_UpdateNote.Visibility = Visibility.Hidden;
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
            SB_UpdateNote.Visibility = Visibility.Hidden;
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
            SB_Progress.Value = (double)cfi / fc;
            SB_CurrentStatus.Text = $"{cfi}/{fc}";
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
            NoteFlush();
            flagJumpFrame = true;
        }

        private void PreviousKeyframe_Click(object sender, RoutedEventArgs e) => OperateInfo.SelectedIndex = OperateInfo.SelectedIndex <= 0 ? 0 : --OperateInfo.SelectedIndex;

        private void NextKeyframe_Click(object sender, RoutedEventArgs e) => OperateInfo.SelectedIndex = OperateInfo.SelectedIndex >= OperateInfo.Items.Count ? OperateInfo.Items.Count : ++OperateInfo.SelectedIndex;

        private void PreviousFrame_Click(object sender, RoutedEventArgs e)
        {
            flagJumpFrame = false;
            int cfi = ((ProjectFile)DataContext).CurrentFrameIndex - 1;
            if (OperateInfo.SelectedIndex - 1 > 0 && ((KeyframeInfo)OperateInfo.Items[OperateInfo.SelectedIndex - 1]).FrameIndex == cfi)
                --OperateInfo.SelectedIndex;
            FrameAt(cfi);       // 如果有需要，请调整解码器（GifDecoder.cs），使其保留两个向后的缓存
            NoteFlush();
            flagJumpFrame = true;
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
