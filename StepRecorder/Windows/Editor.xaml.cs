using Microsoft.Win32;
using StepRecorder.Core.Components;
using StepRecorder.Extensions;
using System.Windows;
using System.Windows.Controls;

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
            this.TryShowOwner();
        }

        private void Data_Binding()
        {
            StatusBar.IsEnabled = true;
            OperateInfo.ItemsSource = ((ProjectFile)DataContext).GetKeyframeInfo();
        }

        #region 菜单
        private void File_Open(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = (string)Application.Current.Resources["S.Share.OpenFileDialog.Title"],
                DefaultDirectory = SavePath.DefaultOutputDirectory,
                DefaultExt = ".strcd",
                Filter = $"{(string)Application.Current.Resources["S.Share.FileDialog.Filter.STRCD"]} (*.strcd)|*.strcd|{(string)Application.Current.Resources["S.Share.FileDialog.Filter.All"]} (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                DataContext = new ProjectFile(openFileDialog.FileName);
                Data_Binding();
            }
        }
        #endregion

        private bool flagShortNote = false;
        private bool flagDetailNote = false;

        private void OperateInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Note_LostFocus();

            ((ProjectFile)DataContext).CurrentKeyframeIndex = OperateInfo.SelectedIndex;
            ShortNote.Text = ((KeyframeInfo)OperateInfo.SelectedItem).ShortNote;
            DetailNote.Text = ((KeyframeInfo)OperateInfo.SelectedItem).DetailNote;
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
    }
}
