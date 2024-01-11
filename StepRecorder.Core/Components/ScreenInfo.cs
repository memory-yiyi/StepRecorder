using System.Windows;

namespace StepRecorder.Core.Components
{
    internal class ScreenInfo(string name, string description) : AreaInfo(name, description)
    {
        public override Rect Rect => new(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
    }
}
