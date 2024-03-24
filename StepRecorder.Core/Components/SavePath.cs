namespace StepRecorder.Core.Components
{
    public static class SavePath
    {
        private static readonly string currentProcessDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string TempDirectory => $"{currentProcessDirectory}tmp\\";
        internal static string TempPathOfGIF => $"{TempDirectory}{TarEntryNameOfGIF}";
        public static string DefaultOutputDirectory => $"{currentProcessDirectory}out\\";
        public static string? DefaultOutputPathPrefix { get; set; }
        internal const string TarEntryNameOfXML = "Keyframe.xml";
        internal const string TarEntryNameOfGIF = "Frames.gif";
    }
}
