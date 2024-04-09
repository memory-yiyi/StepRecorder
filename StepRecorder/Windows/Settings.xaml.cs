using StepRecorder.Extensions;
using System.Windows;

namespace StepRecorder.Windows
{
    /// <summary>
    /// 设置
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.TryShowOwner();
        }
    }
}
