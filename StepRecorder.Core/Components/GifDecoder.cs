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
        #endregion

        #region 属性
        public int Width { get => image.Width; }
        public int Height { get => image.Height; }
        public int FrameCount { get => image.GetFrameCount(frameDimension); }
        #endregion

        #region 构造函数
        private GifDecoder(object obj, int cacheSize = 16, int cacheCount = 2)
        {
            if (obj is string filePath)
                image = Image.FromFile(filePath);
            else if (obj is Stream stream)
                image = Image.FromStream(stream);
            else
                throw new InvalidOperationException("传参无效");

            this.cacheSize = cacheSize;
            this.cacheCount = cacheCount;
            baseFrameIndex = int.MinValue;
            currentCacheIndex = 0;

            framesCache = [];
            for (int i = 0; i < cacheCount; ++i)
                framesCache.Add(new FramesCache(image, cacheSize));
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

            if (index >= baseFrameIndex && index < baseFrameIndex + cacheSize * cacheCount)
            {
                if (index >= baseFrameIndex + cacheSize)
                {
                    int indexAdd = (index - baseFrameIndex + 1) / cacheSize;       // +1 此处为数量，没有时为索引
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
