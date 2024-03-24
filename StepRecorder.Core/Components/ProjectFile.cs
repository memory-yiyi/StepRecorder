using System.Formats.Tar;
using System.IO;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using KeyframesInfo = System.Collections.Generic.List<StepRecorder.Core.Components.KeyframeInfo>;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 与工程文件相关的类
    /// </summary>
    /// <param name="path">工程文件绝对路径</param>
    /// <param name="keyframes">关键帧集合</param>
    public class ProjectFile(string path, KeyframesInfo keyframes)
    {
        public ProjectFile(string path) : this(path, [])
        {
            Load();
        }

        ~ProjectFile()
        {
            gifMemoryStream.Dispose();
        }

        #region 保存
        public string Path { get; init; } = path;

        internal void SaveFromRecord()
        {
            using MemoryStream mStream = new();
            new XmlSerializer(typeof(KeyframesInfo)).Serialize(mStream, keyframes);
            mStream.Position = 0;
            using (FileStream fStream = new(Path, FileMode.Create, FileAccess.Write))
            {
                using TarWriter tarWriter = new(fStream);
                tarWriter.WriteEntry(new GnuTarEntry(TarEntryType.RegularFile, SavePath.TarEntryNameOfXML) { DataStream = mStream });
                tarWriter.WriteEntry(SavePath.TempPathOfGIF, SavePath.TarEntryNameOfGIF);
            }

            Load(false);
        }

        internal void Save() { }

        internal void SerializeKeyframesToFile()
        {
            using FileStream fileStream = new($"{SavePath.DefaultOutputPathPrefix}.xml", FileMode.Create);
            new XmlSerializer(typeof(KeyframesInfo)).Serialize(fileStream, keyframes);
        }
        #endregion

        #region 加载
        private readonly MemoryStream gifMemoryStream = new();

        private void Load(bool flagXML = true, bool flagGIF = true)
        {
            using FileStream fStream = new(Path, FileMode.Open, FileAccess.Read);
            using TarReader tarReader = new(fStream);
            TarEntry? currentTarEntry;

            while ((currentTarEntry = tarReader.GetNextEntry()) != null)
            {
                switch (currentTarEntry.Name)
                {
                    case SavePath.TarEntryNameOfXML:
                        if (flagXML)
                        {
                            keyframes = new XmlSerializer(typeof(KeyframesInfo)).Deserialize(currentTarEntry.DataStream!) as KeyframesInfo ?? [];
                        }
                        break;
                    case SavePath.TarEntryNameOfGIF:
                        if (flagGIF)
                        {
                            currentTarEntry.DataStream!.CopyTo(gifMemoryStream);
                            gifMemoryStream.Position = 0;
                            gifDecoder = new GifBitmapDecoder(gifMemoryStream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                        }
                        break;
                }
            }
        }
        #endregion

        #region 关键帧
        public int CurrentKeyframeIndex { get; set; } = 0;

        public IEnumerable<KeyframeInfo> GetKeyframeInfo()
        {
            foreach (KeyframeInfo info in keyframes)
                yield return info;
        }
        #endregion

        #region 帧
        private int currentFrameIndex = 0;
        private GifBitmapDecoder? gifDecoder;
        public int CurrentFrameIndex
        {
            get => currentFrameIndex;
            set
            {
                if (gifDecoder == null)
                    throw new InvalidOperationException("未加载文件");
                else if (currentFrameIndex < 0)
                    currentFrameIndex = 0;
                else if (currentFrameIndex > gifDecoder.Frames.Count)
                    currentFrameIndex = gifDecoder.Frames.Count;
                else
                    currentFrameIndex = value;
            }
        }

        public IEnumerable<BitmapFrame> GetFrame()
        {
            foreach (BitmapFrame frame in gifDecoder!.Frames)
                yield return frame;
        }
        #endregion
    }
}
