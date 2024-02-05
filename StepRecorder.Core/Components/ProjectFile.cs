using System.IO;
using System.Xml.Serialization;
using KeyframesInfo = System.Collections.Generic.List<StepRecorder.Core.Components.KeyframeInfo>;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 与工程文件相关的类
    /// </summary>
    public class ProjectFile
    {
        public string Path { get; init; }
        public KeyframesInfo Keyframes { get; } = [];

        public ProjectFile(string path) => Path = path;

        internal void KeyframesSerializerToFile()
        {
            using FileStream fileStream = new($"{SavePath.DefaultOutputPathPrefix}.xml", FileMode.Create);
            new XmlSerializer(typeof(KeyframesInfo)).Serialize(fileStream, Keyframes);
        }
    }
}
