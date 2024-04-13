using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StepRecorder.Core.Components
{
    internal class GifDecoder : IDisposable
    {
        #region 类定义
        private class FramesCache(Image image, int cacheSize)
        {
            #region 字段
            private readonly Image image = image;
            private readonly int frameCount = image.GetFrameCount(frameDimension);

            private readonly object _canWriteCache = new();
            private readonly List<ImageSource?> framesCache = Enumerable.Repeat<ImageSource?>(null, cacheSize).ToList();

            public ImageSource? this[int index] => framesCache[index];
            #endregion

            #region 方法
            /// <summary>
            /// 异步加载GIF帧
            /// </summary>
            /// <param name="baseFrameIndex">基准起始帧索引</param>
            public async void AsyncLoadFrames(int baseFrameIndex)
            {
                await Task.Delay(100);      // 给线程一点时间，避免线程抢占资源
                LoadFrames(baseFrameIndex);
            }

            /// <summary>
            /// 加载GIF帧
            /// </summary>
            /// <param name="baseFrameIndex">基准起始帧索引</param>
            public void LoadFrames(int baseFrameIndex)
            {
                using Image image = (Image)this.image.Clone();
                using Bitmap bmp = new(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using Graphics g = Graphics.FromImage(bmp);
                lock (_canWriteCache)
                {
                    int curIndex;
                    IntPtr hBitmap;

                    for (int i = 0; i < framesCache.Count; ++i)
                    {
                        curIndex = baseFrameIndex + i;
                        if (curIndex >= frameCount)
                        {
                            framesCache[i] = null;
                            continue;
                        }
                        image.SelectActiveFrame(frameDimension, curIndex);
                        g.DrawImage(image, 0, 0);

                        hBitmap = bmp.GetHbitmap();
                        framesCache[i] = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions()
                            );
                        DeleteObject(hBitmap);
                    }
                }
            }
            #endregion

            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            private static extern bool DeleteObject(IntPtr hObject);
        }
        #endregion

        #region 字段
        private readonly Image image;
        private static readonly FrameDimension frameDimension = FrameDimension.Time;

        private readonly int cacheSize;
        private readonly int cacheCount;
        private int baseFrameIndex;
        private int currentCacheIndex;
        private readonly List<FramesCache> framesCache;
        private readonly List<int> frameMap;
        #endregion

        #region 属性
        public int Width { get => image.Width; }
        public int Height { get => image.Height; }
        public int RealFrameCount { get => image.GetFrameCount(frameDimension); }
        public int FrameCount { get => frameMap.Last(); }
        /*
         * 对于时间间隔如果有需要，请调整录制工具（RecordTool.cs）并进行调用
         * 如果开发到可以选择帧率，还需调整工程文件（ProjectFile.cs）
         */
        public int MSPF { get; } = 125;
        #endregion

        #region 构造函数
        private GifDecoder(object obj, int cacheSize = 4, int cacheCount = 6)
        {
            static int FuzzyQuotient(double x, double y) => (int)Math.Round(x / y);

            if (obj is string filePath)
                image = Image.FromFile(filePath);
            else if (obj is Stream stream)
                image = Image.FromStream(stream);
            else
                throw new InvalidOperationException("传参无效");

            int i, j, k, index;
            PropertyItem pi;
            byte[] delayByte = new byte[4];

            this.cacheSize = cacheSize;
            this.cacheCount = cacheCount;
            baseFrameIndex = int.MinValue;
            currentCacheIndex = 0;

            framesCache = [];
            for (i = 0; i < cacheCount; ++i)
                framesCache.Add(new FramesCache(image, cacheSize));

            frameMap = [0];
            for (i = 0; i < RealFrameCount; ++i)
            {
                image.SelectActiveFrame(frameDimension, i);

                if ((index = Array.IndexOf(image.PropertyIdList, 0x5100)) != -1)
                {
                    pi = image.PropertyItems[index];
                    for (j = i * 4, k = 0; k < 4; ++j, ++k)
                        delayByte[k] = pi.Value![j];
                    frameMap.Add(FuzzyQuotient(BitConverter.ToInt32(delayByte, 0) * 10, MSPF) + frameMap[i]);
                }
            }
        }

        public GifDecoder(string filePath) : this(filePath as object) { }
        public GifDecoder(Stream stream) : this(stream as object) { }
        #endregion

        #region 方法
        public ImageSource? FrameAt(int index)
        {
            void GetNextCacheIndex(ref int cacheIndex, int baseIndex)
            {
                cacheIndex = (cacheIndex + 1) % cacheCount;
                if (cacheIndex == baseIndex)
                    cacheIndex = -1;
            }

            int GetNewBaseStartIndex(int loopIndex, int baseIndex) => baseFrameIndex + (loopIndex - baseIndex + cacheCount) % cacheCount * cacheSize;

            int BinaryRegionSearch(int x)
            {
                if (x < frameMap.First() || x >= frameMap.Last())
                    return -1;

                int low = 0;
                int high = frameMap.Count - 1;
                int mid;

                while (low <= high)
                {
                    mid = (low + high) >> 1;
                    if (x >= frameMap[mid])
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid - 1;
                        if (high == -1)
                            return 0;
                    }
                }
                return high;
            }

            if ((index = BinaryRegionSearch(index)) == -1)
                return null;

            if (index >= baseFrameIndex && index < baseFrameIndex + cacheSize * cacheCount)
            {
                if (index >= baseFrameIndex + cacheSize)
                {
                    int indexAdd = (index - baseFrameIndex) / cacheSize;
                    int loopIndex = currentCacheIndex;
                    currentCacheIndex = (currentCacheIndex + indexAdd) % cacheCount;
                    baseFrameIndex += indexAdd * cacheSize;
                    int baseIndex = currentCacheIndex;
                    while (loopIndex != -1)
                    {   // 从跳转处更新缓存
                        framesCache[loopIndex].AsyncLoadFrames(GetNewBaseStartIndex(loopIndex, baseIndex));
                        GetNextCacheIndex(ref loopIndex, baseIndex);
                    }
                }
            }
            else
            {
                baseFrameIndex = index;
                currentCacheIndex = 0;
                framesCache[0].LoadFrames(baseFrameIndex);
                int loopIndex = 1;
                while (loopIndex != -1)
                {   // 从跳转处更新缓存
                    framesCache[loopIndex].AsyncLoadFrames(GetNewBaseStartIndex(loopIndex, 0));
                    GetNextCacheIndex(ref loopIndex, 0);
                }
            }
            return framesCache[currentCacheIndex][index - baseFrameIndex];
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
                    image.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                framesCache.Clear();

                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GifDecoder()
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
