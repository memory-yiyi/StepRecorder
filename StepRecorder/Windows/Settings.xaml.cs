using System.Windows;

namespace StepRecorder.Windows
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Closed(object sender, EventArgs e)
        {
            this.Owner?.Show();
        }
    }
}
