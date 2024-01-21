using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StepRecorder.ViewModel
{
    internal class ApplicationViewModel
    {
        /// <summary>
        /// 获取即将跳转到的下一个窗体
        /// </summary>
        public static ICommand GetNextWindow
        {
            get
            {
                return new RelayCommand(obj =>
                {
                    if (obj is Control o)
                    {
                        Window window = Application.Current.MainWindow;
                        Type? type = window.GetType().Assembly.GetType($"StepRecorder.Windows.{o.Name}");
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
                                window.Hide();
                                entry.Owner = window.Owner ?? window;
                                entry.Show();
                            }
                        }
                    }
                });
            }
        }
    }
}
