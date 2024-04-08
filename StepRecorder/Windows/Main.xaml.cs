using StepRecorder.Core.Components;
using System.Windows;

namespace StepRecorder.Windows
{
    /// <summary>
    /// 程序主窗体
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 此代码用于强制在启动程序时执行 ProcessInfo 类的静态构造函数
            // 如果获取屏幕缩放比例的方式发生变化，请随之更改
            AreaList.GetScreenScaling();
        }
    }
}
