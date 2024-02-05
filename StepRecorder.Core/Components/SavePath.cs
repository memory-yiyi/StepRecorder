namespace StepRecorder.Core.Components
{
    public static class SavePath
    {
        private static readonly string currentProcessDirectory = AppDomain.CurrentDomain.BaseDirectory;
        internal static string FramesDirectory => $"{currentProcessDirectory}tmp\\";
        public static string DefaultOutputDirectory => $"{currentProcessDirectory}out\\";
        public static string? DefaultOutputPathPrefix { get; set; }
    }
}
