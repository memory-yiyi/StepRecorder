using System.Windows;

namespace StepRecorder.Windows
{
    /// <summary>
    /// 提供注释帧的信息
    /// </summary>
    public partial class Notes : Window
    {
        public Notes()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Grid_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }
    }
}
