using System.Formats.Tar;
using System.IO;
using System.Windows.Media;
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
            // COPY_XmlSerialize(1)
            using MemoryStream mStream = new();
            new XmlSerializer(typeof(KeyframesInfo)).Serialize(mStream, keyframes);
            mStream.Position = 0;
            // endCOPY_XmlSerialize
            using (FileStream fStream = new(Path, FileMode.Create, FileAccess.Write))
            {
                using TarWriter tarWriter = new(fStream, TarEntryFormat.Gnu);
                tarWriter.WriteEntry(new GnuTarEntry(TarEntryType.RegularFile, SavePath.TarEntryNameOfXML) { DataStream = mStream });
                tarWriter.WriteEntry(SavePath.TempPathOfGIF, SavePath.TarEntryNameOfGIF);
            }

            Load(flagXML: false);
        }

        public void Save()
        {
            // COPY_XmlSerialize(1)
            using MemoryStream mStream = new();
            new XmlSerializer(typeof(KeyframesInfo)).Serialize(mStream, keyframes);
            mStream.Position = 0;
            // endCOPY_XmlSerialize

        }

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
                GnuTarEntry curGnuTarEntry = new(currentTarEntry);
                switch (curGnuTarEntry.Name)
                {
                    case SavePath.TarEntryNameOfXML:
                        if (flagXML)
                        {
                            keyframes = new XmlSerializer(typeof(KeyframesInfo)).Deserialize(curGnuTarEntry.DataStream!) as KeyframesInfo ?? [];
                        }
                        break;
                    case SavePath.TarEntryNameOfGIF:
                        if (flagGIF)
                        {
                            curGnuTarEntry.DataStream!.CopyTo(gifMemoryStream);
                            gifMemoryStream.Position = 0;
                            gifDecoder = new GifDecoder(gifMemoryStream);
                        }
                        break;
                }
            }
        }
        #endregion

        #region 关键帧
        public int CurrentKeyframeIndex { get; set; } = -1;

        public IEnumerable<KeyframeInfo> GetKeyframeInfo()
        {
            foreach (KeyframeInfo info in keyframes)
                yield return info;
        }
        #endregion

        #region 帧
        private int currentFrameIndex = 0;
        private GifDecoder? gifDecoder;
        public int CurrentFrameIndex
        {
            get => currentFrameIndex;
            set
            {
                if (gifDecoder == null)
                    throw new InvalidOperationException("未加载文件");
                else if (currentFrameIndex < 0)
                    currentFrameIndex = 0;
                else if (currentFrameIndex > gifDecoder.FrameCount)
                    currentFrameIndex = gifDecoder.FrameCount;
                else
                    currentFrameIndex = value;
            }
        }

        public ImageSource? FrameAt(int frameIndex)
        {
            CurrentFrameIndex = frameIndex;
            return gifDecoder!.FrameAt(CurrentFrameIndex);
        }
        #endregion
    }
}
