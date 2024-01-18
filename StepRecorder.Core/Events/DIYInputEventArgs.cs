using StepRecorder.Core.Components;
using System.Collections.ObjectModel;

namespace StepRecorder.Core.Events
{
    internal abstract class DIYInputEventArgs : EventArgs
    {
        public bool Handled { get; set; } = false;
        public abstract ReadOnlyCollection<string> Keys { get; }
        public InputHook.POINT? Point { get; init; } = null;
        public int? Time { get; init; } = null;
    }
}
