using StepRecorder.Core.Events;
using System.Text;
using System.Windows;

namespace StepRecorder.Core.Components.RecordTools
{
    internal class Hook
    {
        private readonly InputHook inputHook = new();

        public void Install()
        {
            inputHook.Start();
            // 添加事件
            inputHook.KeyOper += RecordKeyboard;
        }

        public void Uninstall()
        {
            // 释放事件
            inputHook.KeyOper -= RecordKeyboard;
            inputHook.Stop();
        }

        private void RecordKeyboard(object sender, DIYKeyEventArgs e)
        {
            StringBuilder sb = new();
            foreach (var key in e.Keys)
            {
                sb.Append(key);
            }
            MessageBox.Show(sb.ToString());
        }
    }
}
