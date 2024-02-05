using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace StepRecorder.Core.Components.RecordTools
{
    /// <summary>
    /// 图像像素处理类，可以提供一个指向图像像素集的地址
    /// </summary>
    internal class PixelTool : IDisposable
    {
        #region 释放模式
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                pbuf.Free();
                data?.Unlock();
                data = null;
                pixels = null;
                GC.Collect(1);

                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~PixelTool()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private readonly BitmapSource source;
        private WriteableBitmap? data;

        internal int Width { get; init; }
        internal int BytesPerRow { get; private set; }
        internal int Height { get; init; }
        private byte[]? pixels;
        private GCHandle pbuf;
        internal IntPtr PixelsAddress { get; private set; }


        public PixelTool(BitmapSource source)
        {
            this.source = source;
            Width = this.source.PixelWidth;
            Height = this.source.PixelHeight;
        }

        internal void HandlePixelInfos()
        {
            // 获取图像色彩深度
            int bpp = source.Format.BitsPerPixel;

            if (bpp != 32 && bpp != 24)
                throw new InvalidOperationException("仅支持24位或32位图像");

            data = new WriteableBitmap(source);

            data.Lock();

            BytesPerRow = Width * bpp >> 3;

            pixels = new byte[BytesPerRow * Height];

            /*
             * 用于判断是否需要移除边界对齐填充
             * 一般的，每行像素数据的边界对齐4字节
             * 不能对齐的部分会填充一些额外的字节，以确保下一行的像素可以从4字节边界开始
             * 此处我们需要的只是像素数据，需要去掉填充字节
             */
            int pad = BytesPerRow & 3;
            pad = pad == 0 ? pad : 4 - pad;
            if (pad == 0)
                Marshal.Copy(data.BackBuffer, pixels, 0, pixels.Length);
            else
            {
                int realityBytesPerRow = BytesPerRow + pad;
                for (int row = 0; row < Height; ++row)
                    Marshal.Copy(new IntPtr(data.BackBuffer.ToInt64() + row * realityBytesPerRow), pixels, row * BytesPerRow, BytesPerRow);
            }

            // 固定缓冲区以获取地址供 API 使用
            pbuf = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            PixelsAddress = pbuf.AddrOfPinnedObject();
        }
    }
}
