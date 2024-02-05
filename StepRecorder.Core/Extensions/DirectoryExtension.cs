using System.IO;

namespace StepRecorder.Core.Extensions
{
    public static class DirectoryExtension
    {
        public static DirectoryInfo CreateDirectory(this string directory) => Directory.CreateDirectory(directory);

        public static DirectoryInfo RecreateDirectory(this string directory)
        {
            directory.DeleteDirectory(true);
            return Directory.CreateDirectory(directory);
        }

        public static void DeleteDirectory(this string directory, bool recursive)
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive);
        }
    }
}
