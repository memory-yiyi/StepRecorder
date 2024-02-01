using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace StepRecorder.Core.Components.RecordTools
{
    /// <summary>
    /// GIF 录制工具
    /// </summary>
    internal class RecordTool
    {
        private readonly Rectangle recordArea;
        private readonly Thread recordThread;
        private readonly CancellationTokenSource _cts = new();
        private readonly AutoResetEvent _waitToResume = new(false);
        private CancellationTokenSource _waitToSuspend = new();

        private readonly Thread mergeThread;
        private static readonly string _framesDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}tmp\\";
        private static readonly string _gifDefaultOutputDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}out\\";
        private static string? _gifDefaultOutputPath;

        //private readonly int fps = 8;
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
            mergeThread = new Thread(MergeFrames)
            {
                Priority = ThreadPriority.BelowNormal
            };

            if (Directory.Exists(_framesDirectory))
                Directory.Delete(_framesDirectory, true);
            Directory.CreateDirectory(_framesDirectory);
            if (!Directory.Exists(_gifDefaultOutputDirectory))
                Directory.CreateDirectory(_gifDefaultOutputDirectory);
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
            recordThread.Start();
            mergeThread.Start();
        }

        internal void Stop() => _cts.Cancel();

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
            using Bitmap frame = new(recordArea.Width, recordArea.Height, PixelFormat.Format24bppRgb);
            using Graphics g = Graphics.FromImage(frame);
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

                g.CopyFromScreen(recordArea.Left, recordArea.Top, 0, 0, frame.Size);
                // 固定帧率为 8 帧
                Thread.Sleep(125 - (int)ipts.ElapsedMilliseconds % 125);
                frame.Save($"{_framesDirectory}{index++}.png", ImageFormat.Png);
                ++currentFrameNo;

                if (_cts.IsCancellationRequested)
                    break;
            }
        }

        private void MergeFrames()
        {
            uint index = 0;
            _gifDefaultOutputPath = $"{_gifDefaultOutputDirectory}Steps{DateTime.Now:_yyyyMMdd_HHmmss}.gif";
            Gifski gifski = new();
            gifski.Start(_gifDefaultOutputPath, (uint)recordArea.Width, (uint)recordArea.Height, 30);

            // 等待生成第一帧文件
            Thread.Sleep(200);
            while (true)
            {
                if (index != currentFrameNo)
                    gifski.AddFrame($"{_framesDirectory}{index}.png", index++, index * 125);
                if (_cts.IsCancellationRequested)
                    if (index == currentFrameNo)
                        break;
                    else
                    Thread.Sleep(120);
                else
                    Thread.Sleep(800);
            }

            gifski.Stop();
        }
    }
}
