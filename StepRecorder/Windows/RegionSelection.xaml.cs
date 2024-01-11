using System.Runtime.CompilerServices;
using System.Windows;

namespace StepRecorder.Windows
{
    /// <summary>
    /// 区域选择窗体
    /// </summary>
    public partial class RegionSelection : Window
    {
        #region 单例模式
        private static readonly object instanceLock = new();
        private static RegionSelection? instance;
        public static RegionSelection GetInstance(Rect rect)
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new RegionSelection
                        {
                            Left = rect.Left,
                            Top = rect.Top,
                            Height = rect.Height,
                            Width = rect.Width
                        };
                    }
                }
            }
            return instance;
        }
        public RegionSelection()
        {
            InitializeComponent();
        }
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            instance = null;
        }
    }
}
