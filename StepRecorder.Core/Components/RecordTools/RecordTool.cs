using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace StepRecorder.Core.Components.RecordTools
{
    internal class RecordTool
    {
        private readonly Rectangle recordArea;
        private readonly Thread recordThread;
        private readonly CancellationTokenSource _cts = new();
        private readonly AutoResetEvent _waitToResume = new(false);
        private CancellationTokenSource _waitToSuspend = new();
        //private readonly Gifski gifski = new();
        private readonly string _framesDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}tmp\\";

        //private readonly int fps = 10;
        private readonly List<int> keyframeNos = [];
        private int currentFrameNo = 0;
        internal int KeyframeCount { get => keyframeNos.Count; }

        [SupportedOSPlatform("windows")]
        public RecordTool(Rectangle recordArea)
        {
            if (recordArea == Rectangle.Empty || recordArea.Size == Size.Empty)
                throw new ArgumentException("录制区域异常");

            this.recordArea = recordArea;
            recordThread = new Thread(RecordArea)
            {
                Priority = ThreadPriority.AboveNormal
            };

            if (Directory.Exists(_framesDirectory))
                Directory.Delete(_framesDirectory, true);
            Directory.CreateDirectory(_framesDirectory);
        }

        ~RecordTool()
        {
            _cts.Dispose();
            _waitToResume.Dispose();
            _waitToSuspend.Dispose();
        }

        internal void NoteKeyframe(object? sender, EventArgs e) => keyframeNos.Add(currentFrameNo);

        internal void Start()
        {
            //gifski.Start("test.gif", (uint)recordArea.Width, (uint)recordArea.Height, 50);
            recordThread.Start();
        }

        internal void Stop()
        {
            _cts.Cancel();
            //gifski.Stop();
        }

        /// <summary>
        /// 挂起录制线程
        /// </summary>
        internal void Suspend() => _waitToSuspend.Cancel();

        /// <summary>
        /// 继续录制进程
        /// </summary>
        internal void Resume()
        {
            if (_waitToSuspend.IsCancellationRequested)
                _waitToResume.Set();
            else
                throw new InvalidOperationException("非法调用：线程没有被挂起");
        }

        /// <summary>
        /// 基于 Graphics 录制屏幕指定区域
        /// </summary>
        /// <remarks>帧率在 10fps 上下波动，对于此程序勉强可以接受</remarks>
        [SupportedOSPlatform("windows")]
        private void RecordArea()
        {
            uint index = 0;
            Stopwatch ipts = Stopwatch.StartNew();

            while (true)
            {
                if (_waitToSuspend.IsCancellationRequested)
                {
                    ipts.Stop();
                    _waitToResume.WaitOne();
                    _waitToSuspend.Dispose();
                    _waitToSuspend = new CancellationTokenSource();
                    ipts.Start();
                }

                using (Bitmap frame = new(recordArea.Width, recordArea.Height, PixelFormat.Format24bppRgb))
                {
                    using Graphics g = Graphics.FromImage(frame);
                    g.CopyFromScreen(recordArea.Left, recordArea.Top, 0, 0, frame.Size);
                    frame.Save($"{_framesDirectory}{index++}.{ipts.ElapsedMilliseconds}.png", ImageFormat.Png);
                }

                ++currentFrameNo;

                if (_cts.IsCancellationRequested)
                    break;
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
