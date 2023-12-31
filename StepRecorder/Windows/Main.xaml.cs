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

        /// <summary>
        /// 启动入口函数，可以通过控件的Name属性启动相应变量
        /// </summary>
        public void StartEntry(object sender, RoutedEventArgs e)
        {
            if (e.Source is FrameworkElement source)
            {
                Type? type = this.GetType().Assembly.GetType($"StepRecorder.Windows.{source.Name}");
                if (type != null)
                {
                    Window? entry = null;
                    try
                    {
                        entry = Activator.CreateInstance(type) as Window;
                    }
                    catch (MissingMethodException) { }
                    entry ??= type.GetMethod("GetInstance")?.Invoke(null, null) as Window;
                    if (entry != null)
                    {
                        this.Hide();
                        entry.Owner = this;
                        entry.Show();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
