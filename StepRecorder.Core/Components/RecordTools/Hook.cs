using StepRecorder.Core.Events;
using System.Text;
using System.Windows;

namespace StepRecorder.Core.Components.RecordTools
{
    internal class Hook
    {
        #region 键鼠钩子及相关操作
        private readonly InputHook inputHook = new();

        /// <summary>
        /// 安装键鼠钩子
        /// </summary>
        public void Install()
        {
            inputHook.Start();
            Start();
        }

        /// <summary>
        /// 卸载键鼠钩子
        /// </summary>
        public void Uninstall()
        {
            Stop();
            inputHook.Stop();
        }

        /// <summary>
        /// 启用键鼠钩子
        /// </summary>
        public void Start()
        {
            inputHook.MouseOper += RecordInput;
            inputHook.KeyOper += RecordInput;
        }

        /// <summary>
        /// 停用键鼠钩子
        /// </summary>
        public void Stop()
        {
            inputHook.MouseOper -= RecordInput;
            inputHook.KeyOper -= RecordInput;
        }
        #endregion

        private void RecordInput(object sender, DIYInputEventArgs e)
        {
            if (e.Keys[0] == "LB")
                return;
            StringBuilder sb = new();
            foreach (var key in e.Keys)
            {
                sb.Append(key);
            }
            MessageBox.Show(sb.ToString());
        }
    }
}
