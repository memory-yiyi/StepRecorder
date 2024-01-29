using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace StepRecorder.Core.Components.RecordTools
{
    internal class RecordTool
    {
        private readonly Rectangle recordArea;
        private readonly Thread recordThread;
        private readonly CancellationTokenSource _cts = new();
        //private WaitHandle? _wait;
        //private readonly GifBitmapEncoder recordEncoder = new();
        private readonly Gifski gifski = new();
        private readonly Queue<Task> frameTasks = [];

        private readonly int fps = 10;
        private readonly List<int> keyframeNos = [];
        private int currentFrameNo = 0;
        internal int KeyframeCount { get => keyframeNos.Count; }

        [SupportedOSPlatform("windows")]
        public RecordTool(Rectangle recordArea)
        {
            if (recordArea == Rectangle.Empty || recordArea.Size == Size.Empty)
                throw new ArgumentException("录制区域异常");
            this.recordArea = recordArea;
            recordThread = new Thread(RecordArea);
        }

        internal void NoteKeyframe(object? sender, EventArgs e) => keyframeNos.Add(currentFrameNo);

        internal void Start()
        {
            gifski.Start("test.gif", (uint)recordArea.Width, (uint)recordArea.Height, 50);
            recordThread.Start();
        }

        internal void Stop()
        {
            _cts.Cancel();
            frameTasks.Clear();
            gifski.Stop();
        }

        /// <summary>
        /// 挂起录制线程
        /// </summary>
        internal void Suspend() { }

        /// <summary>
        /// 继续录制进程
        /// </summary>
        internal void Resume()
        {
            //_wait!.Close();
            //_wait = null;
        }

        [SupportedOSPlatform("windows")]
        private void RecordArea()
        {
            // 给 pts 用的计时变量，特点均匀，单位：秒
            uint index = 0;
            double dfps = fps;

            while (true)
            {
                //_wait?.WaitOne();

                frameTasks.Enqueue(new Task(() =>
                {
                    uint id = index++;
                    using Bitmap frame = new(recordArea.Width, recordArea.Height, PixelFormat.Format24bppRgb);
                    using Graphics g = Graphics.FromImage(frame);
                    g.CopyFromScreen(recordArea.Left, recordArea.Top, 0, 0, frame.Size);
                    //frame.Save($"{i++}.png", ImageFormat.Png);
                    if (!_cts.IsCancellationRequested)
                    {
                        frameTasks.Dequeue();
                        if (frameTasks.Count > 0 && frameTasks.Peek().Status == TaskStatus.Created)
                            frameTasks.Peek().Start();
                    }

                    IntPtr handle = frame.GetHbitmap();
                    gifski.AddFrame(
                        new FormatConvertedBitmap(
                            Imaging.CreateBitmapSourceFromHBitmap(
                                handle,
                                IntPtr.Zero,
                                System.Windows.Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions()
                                ),
                            System.Windows.Media.PixelFormats.Rgb24,
                            null,
                            0),
                        id++,
                        id / dfps);
                    DeleteObject(handle);
                }));

                // 线程维护工作
                if (frameTasks.Peek().Status == TaskStatus.Created)
                    frameTasks.Peek().Start();

                ++currentFrameNo;
                Thread.Sleep(100);

                if (_cts.IsCancellationRequested)
                    break;
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
