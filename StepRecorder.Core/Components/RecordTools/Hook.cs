using StepRecorder.Core.Events;
using System.Runtime.InteropServices;
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
        internal void Install()
        {
            inputHook.Start();
            Start();
        }

        /// <summary>
        /// 卸载键鼠钩子
        /// </summary>
        internal void Uninstall()
        {
            Stop();
            inputHook.Stop();
        }

        /// <summary>
        /// 启用键鼠钩子
        /// </summary>
        internal void Start()
        {
            inputHook.MouseOper += RecordInput;
            inputHook.KeyOper += RecordInput;
        }

        /// <summary>
        /// 停用键鼠钩子
        /// </summary>
        internal void Stop()
        {
            inputHook.MouseOper -= RecordInput;
            inputHook.KeyOper -= RecordInput;
        }
        #endregion

        private readonly List<string> inputs = [];
        private InputHook.POINT? point;
        /// <summary>
        /// 鼠标不录制区域，你可以向其传递一个 Size 为 Empty 的 Rect 禁用它
        /// </summary>
        internal Rect MouseNotRecordArea { private get; set; }
        private uint? time;
        private readonly uint dbClickTime = GetDoubleClickTime();

        private void RecordInput(object sender, DIYInputEventArgs e)
        {
            if (e.Keys.Count == 1)
            {
                string key = e.Keys[0];
                if (e.Time != null)
                {
                    if (MouseNotRecordArea.Size.Equals(Size.Empty) || MouseNotRecordArea.Contains(e.Point!.Value.x / ProcessInfo.Scaling, e.Point.Value.y / ProcessInfo.Scaling))
                        return;
                    int i = inputs.Count - 1;
                    if (e.Point.Equals(point) && i >= 0 && inputs[i].IndexOf(key) > 0 && e.Time - time <= dbClickTime)
                    {
                        if (inputs[i] == $"&{key}")
                            inputs[i] = $"&DB{key}";
                        else if (inputs[i] == $"&DB{key}")
                            inputs[i] = $"&TP{key}";
                    }
                    else
                        inputs.Add($"&{key}");
                }
                else
                    inputs.Add($"&{key}");
            }
            else
            {
                StringBuilder sb = new();
                foreach (var key in e.Keys)
                    sb.Append($"&{key}");
                inputs.Add(sb.ToString());
            }
            point = e.Point;
            time = e.Time;
        }

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();
    }
}
