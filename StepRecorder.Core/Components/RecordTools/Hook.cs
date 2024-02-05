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
        private uint? time;
        private int lastListNum = 0;
        internal event EventHandler? CatchKeyframe;
        /// <summary>
        /// 鼠标不录制区域，当其为 Empty 时禁用该功能
        /// </summary>
        internal Rect MouseNotRecordArea { private get; set; } = Rect.Empty;
        private readonly uint dbClickTime = GetDoubleClickTime();

        private void RecordInput(object sender, DIYInputEventArgs e)
        {
            if (e.Keys.Count == 1)
            {
                string key = e.Keys[0];
                if (e.Time != null)
                {
                    if (!MouseNotRecordArea.IsEmpty && MouseNotRecordArea.Contains(e.Point!.Value.x / ProcessInfo.Scaling, e.Point.Value.y / ProcessInfo.Scaling))
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
            if (inputs.Count != lastListNum)
            {
                CatchKeyframe?.Invoke(this, new EventArgs());
                lastListNum = inputs.Count;
            }
        }

        internal void RecordNote()
        {
            inputs.Add("&Note");
            point = null;
            time = null;
            CatchKeyframe?.Invoke(this, new EventArgs());
            ++lastListNum;
        }

        internal int GetCurrentKeyframeCount() => inputs.Count;

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        internal string this[int i] => inputs[i];
    }
}
