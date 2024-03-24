using System.Windows;

namespace StepRecorder.Extensions
{
    internal static class WindowExtension
    {
        public static void TryShowOwner(this Window window)
        {
            if (window.Owner.OwnedWindows.Count == 0)
                window.Owner.Show();
        }
    }
}
