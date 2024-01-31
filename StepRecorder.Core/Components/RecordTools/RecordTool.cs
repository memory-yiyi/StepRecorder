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

        private readonly string _framesDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}tmp\\";
        private readonly string _gifDefaultOutputDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}out\\";
        private string? _gifDefaultOutputPath;

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

        internal void Start() => recordThread.Start();

        internal void Stop()
        {
            _cts.Cancel();
            MergeFrames(recordArea.Width, recordArea.Height, _framesDirectory, _gifDefaultOutputDirectory, out _gifDefaultOutputPath);
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

        private static void MergeFrames(int width, int height, string framesDirectory, string gifDefaultOutputDirectory, out string gifDefaultOutputPath)
        {
            gifDefaultOutputPath = $"{gifDefaultOutputDirectory}Steps{DateTime.Now:_yyyyMMdd_HHmmss}.gif";
            Gifski gifski = new();
            gifski.Start(gifDefaultOutputPath, (uint)width, (uint)height, 90, true);
            /*
             * 为什么不在创建的时候分出一个表?
             * - 列表增大需要一些时间完成开销，这会给本身帧率就勉强的程序雪上加霜
             * - 静态也回答了你，这使此函数可以独立，你可以在需要的时候调整它的访问性
             *       比如：你需要在类外进行合并操作
             */
            SortedList<uint, (string path, uint ipts)> sortPaths = [];

            foreach (var path in Directory.EnumerateFiles(framesDirectory))
            {
                var infos = Path.GetRelativePath(framesDirectory, path).Split('.');
                sortPaths.Add(uint.Parse(infos[0]), (path, uint.Parse(infos[1])));
            }

            foreach (var infos in sortPaths)
            {
                gifski.AddFrame(infos.Value.path, infos.Key, infos.Value.ipts);
                // 如果内存占用过大，根据情况设计一下阻塞线程
                //Thread.Sleep(30);
            }

            gifski.Stop();
        }
    }
}
