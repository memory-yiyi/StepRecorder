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
    public class ProjectFile(string path, KeyframesInfo keyframes) : IDisposable
    {
        public ProjectFile(string path) : this(path, []) => Load();

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

        public void Save(string? savePath = null)
        {
            savePath ??= Path;
            // COPY_XmlSerialize(1)
            using MemoryStream mStream = new();
            new XmlSerializer(typeof(KeyframesInfo)).Serialize(mStream, keyframes);
            mStream.Position = 0;
            // endCOPY_XmlSerialize
            gifMemoryStream.Position = 0;

            using FileStream fStream = new(savePath, FileMode.Create, FileAccess.Write);
            using TarWriter tarWriter = new(fStream, TarEntryFormat.Gnu);
            tarWriter.WriteEntry(new GnuTarEntry(TarEntryType.RegularFile, SavePath.TarEntryNameOfXML) { DataStream = mStream });
            tarWriter.WriteEntry(new GnuTarEntry(TarEntryType.RegularFile, SavePath.TarEntryNameOfGIF) { DataStream = gifMemoryStream });
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

        public bool AddKeyframe(string? shortNote = null, string? detailNote = null)
        {
            int ckfi = keyframes[CurrentKeyframeIndex].FrameIndex;
            if (CurrentFrameIndex == ckfi || CurrentKeyframeIndex == -1)
                return false;

            if (CurrentFrameIndex > ckfi)
                ++CurrentKeyframeIndex;
            keyframes.Insert(CurrentKeyframeIndex, new KeyframeInfo(++CurrentKeyframeIndex, CurrentFrameIndex, "&AddNote", shortNote, detailNote, true));
            for (int i = CurrentKeyframeIndex--; i < keyframes.Count; ++i)
                ++keyframes[i].Index;
            return true;
        }

        public void RemoveKeyframe()
        {
            if (keyframes.Count == 0 || CurrentKeyframeIndex == -1)
                return;

            keyframes.RemoveAt(CurrentKeyframeIndex);
            for (int i = CurrentKeyframeIndex--; i < keyframes.Count; ++i)
                --keyframes[i].Index;
        }
        #endregion

        #region 帧
        private int currentFrameIndex = 0;
        private GifDecoder? gifDecoder;
        public int CurrentFrameIndex
        {
            get => currentFrameIndex;
            private set
            {
                if (gifDecoder == null)
                    throw new InvalidOperationException("未加载文件");
                else if (value < 0)
                    currentFrameIndex = 0;
                else if (value >= gifDecoder.FrameCount)
                    currentFrameIndex = gifDecoder.FrameCount - 1;
                else
                    currentFrameIndex = value;
            }
        }
        public int FrameCount { get => gifDecoder?.FrameCount ?? -1; }

        public ImageSource? FrameAt(int frameIndex)
        {
            CurrentFrameIndex = frameIndex;
            return gifDecoder!.FrameAt(CurrentFrameIndex);
        }
        #endregion

        #region 释放模式
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    gifMemoryStream.Dispose();
                    gifDecoder?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                keyframes.Clear();

                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ProjectFile()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
