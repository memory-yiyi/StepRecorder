using System.Windows;

namespace StepRecorder.Core.Components
{
    public abstract class AreaInfo(string name, string description)
    {
        public string Name { get; } = name;
        public string Description { get; } = description.Length == 0 ? name : description;
        public abstract Rect Rect { get; }
    }
}
