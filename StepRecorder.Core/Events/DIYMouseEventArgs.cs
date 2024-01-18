using StepRecorder.Core.Components;
using System.Collections.ObjectModel;

namespace StepRecorder.Core.Events
{
    internal class DIYMouseEventArgs : DIYInputEventArgs
    {
        public override ReadOnlyCollection<string> Keys { get; }

        public DIYMouseEventArgs(string mouse, InputHook.POINT point, int time, bool lCtrl = false)
        {
            if (lCtrl)
                Keys = new ReadOnlyCollection<string>(["LCtrl", mouse]);
            else
                Keys = new ReadOnlyCollection<string>([mouse]);
            Point = point;
            Time = time;
        }
    }
}
