using Microsoft.Win32;
using StepRecorder.Core.Components;
using StepRecorder.Extensions;
using System.Windows;

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
            }
        }
    }
}
