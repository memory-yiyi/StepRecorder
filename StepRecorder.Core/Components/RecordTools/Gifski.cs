using System.Runtime.InteropServices;

namespace StepRecorder.Core.Components.RecordTools
{
    /// <summary>
    /// Highest-quality GIF encoder
    /// </summary>
    internal class Gifski
    {
        // 标注 Gifski 版本，保留供以后使用
        //private readonly Version _version = new(1, 14, 1);

        #region gifski.h
        /// <summary>
        /// 用于创建新编码器实例的设置。
        /// </summary>
        /// <see cref="NewDelegate"/>
        [StructLayout(LayoutKind.Sequential)]
        private struct GifskiSettings
        {
            /// <summary>
            /// 如果不是0，则调整宽度的最大值。
            /// </summary>
            public uint Width { get; init; }
            /// <summary>
            /// 如果不是0，则调整高度的最大值。请注意，纵横比不会被保留。
            /// </summary>
            public uint Height { get; init; }
            /// <summary>
            /// 1-100，但有用的范围是50-100。建议设置为90。
            /// </summary>
            public byte Quality { get; init; }
            /// <summary>
            /// 质量较低，但编码速度更快。
            /// </summary>
            public bool Fast { get; init; }
            /// <summary>
            /// 序列重复的次数。如果为负数，则禁用循环。0将永远循环。
            /// </summary>
            public short Repeat { get; init; }

            public GifskiSettings() => throw new NotImplementedException("不允许调用无参构造函数");
            public GifskiSettings(uint width, uint height, byte quality, bool fast, short repeat)
            {
                if (width == 0 || height == 0 || quality == 0 || quality > 100)
                    throw new ArgumentException("宽度或高度或质量设置异常");
                Width = width;
                Height = height;
                Quality = quality;
                Fast = fast;
                Repeat = repeat;
            }
        }

        private enum GifskiError
        {
            GIFSKI_OK = 0,
            /// <summary>
            /// 其中一个输入参数为 null
            /// </summary>
            GIFSKI_NULL_ARG,
            /// <summary>
            /// 一个一次性函数被调用了两次，或者函数的调用顺序错误
            /// </summary>
            GIFSKI_INVALID_STATE,
            /// <summary>
            /// 内部错误：palette quantization
            /// </summary>
            GIFSKI_QUANT,
            /// <summary>
            /// 内部错误：gif composing
            /// </summary>
            GIFSKI_GIF,
            /// <summary>
            /// 内部错误：unexpectedly aborted
            /// </summary>
            GIFSKI_THREAD_LOST,
            /// <summary>
            /// I/O错误：找不到文件或目录
            /// </summary>
            GIFSKI_NOT_FOUND,
            /// <summary>
            /// I/O错误：访问被拒绝
            /// </summary>
            GIFSKI_PERMISSION_DENIED,
            /// <summary>
            /// I/O错误：文件已存在
            /// </summary>
            GIFSKI_ALREADY_EXISTS,
            /// <summary>
            /// 传递给函数的参数无效
            /// </summary>
            GIFSKI_INVALID_INPUT,
            /// <summary>
            /// I/O错误：杂项
            /// </summary>
            GIFSKI_TIMED_OUT,
            /// <summary>
            /// I/O错误：杂项
            /// </summary>
            GIFSKI_WRITE_ZERO,
            /// <summary>
            /// I/O错误：杂项
            /// </summary>
            GIFSKI_INTERRUPTED,
            /// <summary>
            /// I/O错误：杂项
            /// </summary>
            GIFSKI_UNEXPECTED_EOF,
            /// <summary>
            /// progress callback returned 0, writing aborted
            /// </summary>
            GIFSKI_ABORTED,
            /// <summary>
            /// 不应该发生，请提交这个 bug
            /// </summary>
            GIFSKI_OTHER,
        }

        /// <summary>
        /// Call to start the process
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>Returns a handle for the other functions, or `NULL` on error (if the settings are invalid).</returns>
        /// <see cref="AddPngFrameDelegate"/>
        /// <see cref="gifski_end_adding_frames"/>
        private delegate UIntPtr NewDelegate(GifskiSettings settings);
        /// <summary>
        /// 将帧添加到动画中。此函数是异步的。
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="frameNumber">
        /// 对帧（从0开始的连续数字）进行排序。
        /// 您可以按任何顺序添加帧，它们将按“frame_number”进行排序。
        /// 尽管可以无序添加帧，但当帧数过多时，这种无序性会大幅增加内存开销，甚至可能无法提供足够的内存来处理图像。
        /// </param>
        /// <param name="filePath">文件路径必须是有效的UTF-8。</param>
        /// <param name="presentationTimestamp">
        /// PTS 是指自文件开始以来显示此帧的时间（以秒为单位）。
        /// 对于20fps的视频，它可以是“frame_number/20.0”。
        /// 具有重复或无序 PTS 的帧将被跳过。
        /// 第一帧应当具有 PTS＝0。如果第一帧的 PTS > 0，它将被用作最后一帧之后的延迟。
        /// </param>
        /// <returns>Returns 0 (`GIFSKI_OK`) on success, and non-0 `GIFSKI_*` constant on error.</returns>
        /// <remarks>This function may block and wait until the frame is processed. Make sure to call `gifski_set_write_callback` or `SetFileOutputDelegate` first to avoid a deadlock.</remarks>
        private delegate GifskiError AddPngFrameDelegate(UIntPtr handle, uint frameNumber, [MarshalAs(UnmanagedType.LPUTF8Str)] string filePath, double presentationTimestamp);
        /// <summary>
        /// 将帧添加到动画中。此函数是异步的。
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="frameNumber">
        /// 对帧（从0开始的连续数字）进行排序。
        /// 您可以按任何顺序添加帧，它们将按“frame_number”进行排序。
        /// 尽管可以无序添加帧，但当帧数过多时，这种无序性会大幅增加内存开销，甚至可能无法提供足够的内存来处理图像。
        /// </param>
        /// <param name="width"></param>
        /// <param name="bytesPerRow"></param>
        /// <param name="height"></param>
        /// <param name="pixels"></param>
        /// <param name="presentationTimestamp">
        /// PTS 是指自文件开始以来显示此帧的时间（以秒为单位）。
        /// 对于20fps的视频，它可以是“frame_number/20.0”。
        /// 具有重复或无序 PTS 的帧将被跳过。
        /// 第一帧应当具有 PTS＝0。如果第一帧的 PTS > 0，它将被用作最后一帧之后的延迟。
        /// </param>
        /// <returns>Returns 0 (`GIFSKI_OK`) on success, and non-0 `GIFSKI_*` constant on error.</returns>
        /// <remarks>
        /// Same as `gifski_add_frame_rgba_stride`, except it expects RGB components (3 bytes per pixel)
        /// 
        /// Bytes per row must be multiple of 3, and greater or equal width×3.
        /// If the bytes per row value is invalid (not multiple of 3), frames may look sheared/skewed.
        /// 
        /// Colors are in sRGB, red byte first.
        /// 
        /// `gifski_add_frame_rgba` is preferred over this function.
        /// </remarks>
        /// <see cref="gifski_add_frame_rgba_stride"/>
        /// <seealso cref="gifski_add_frame_rgba"/>
        //private delegate GifskiError AddRgbFrameDelegate(UIntPtr handle, uint frameNumber, uint width, uint bytesPerRow, uint height, IntPtr pixels, double presentationTimestamp);

        /// <summary>
        /// Start writing to the file at `destination_path` (overwrites if needed).
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="destinationPath">文件路径必须是 ASCII 或有效的 UTF-8。</param>
        /// <returns>Returns 0 (`GIFSKI_OK`) on success, and non-0 `GIFSKI_*` constant on error.</returns>
        /// <remarks>
        /// 在添加任何帧（gifski_add_frame_*）之前，必须调用此函数。
        /// This call will not block.
        /// </remarks>
        private delegate GifskiError SetFileOutputDelegate(UIntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string destinationPath);
        /// <summary>
        /// 最后一步：
        /// - 停止接受更多帧（gifski_add_frame_*调用被阻止）
        /// - 阻止并等待，直到所有已添加的帧都完成写入
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>Returns 0 (`GIFSKI_OK`) on success, and non-0 `GIFSKI_*` constant on error.</returns>
        /// <remarks>
        /// Must always be called, otherwise it will leak memory.
        /// 在这个调用后，句柄被释放无法再次使用。
        /// </remarks>
        private delegate GifskiError FinishDelegate(UIntPtr handle);

        private readonly NewDelegate _new;
        private readonly AddPngFrameDelegate _addPngFrame;
        //private readonly AddRgbFrameDelegate _addRgbFrame;

        private readonly SetFileOutputDelegate _setFileOutput;
        private readonly FinishDelegate _finish;
        #endregion

        private UIntPtr gifskiHandle = 0;

        public Gifski()
        {
            _new = LoadFunction<NewDelegate>("gifski.dll", "gifski_new");
            _addPngFrame = LoadFunction<AddPngFrameDelegate>("gifski.dll", "gifski_add_frame_png_file");
            //_addRgbFrame = LoadFunction<AddRgbFrameDelegate>("gifski.dll", "gifski_add_frame_rgb");

            _setFileOutput = LoadFunction<SetFileOutputDelegate>("gifski.dll", "gifski_set_file_output");
            _finish = LoadFunction<FinishDelegate>("gifski.dll", "gifski_finish");
        }

        ~Gifski() => Stop();

        /// <summary>
        /// 启动 Gifski
        /// </summary>
        /// <param name="outfilePath">Gif 文件输出路径</param>
        /// <param name="width">图像宽度最大值</param>
        /// <param name="height">图像高度最大值</param>
        /// <param name="quality">图像质量</param>
        /// <param name="fast">图像编码速度</param>
        /// <param name="repeat">设置 Gif 循环模式。负数禁止、0永远、正数指定次数</param>
        /// <exception cref="InvalidOperationException"></exception>
        internal void Start(string outfilePath, uint width, uint height, byte quality, bool fast = false, short repeat = -1)
        {
            if (gifskiHandle == 0)
            {
                gifskiHandle = _new(new GifskiSettings(width, height, quality, fast, repeat));
                if (gifskiHandle == 0)
                    throw new InvalidOperationException("新建 Gifski 句柄失败");
                GifskiError result = _setFileOutput(gifskiHandle, outfilePath);
                if (result != GifskiError.GIFSKI_OK)
                    throw new InvalidOperationException($"创建 Gifski 输出文件失败：{result}");
            }
        }

        /// <summary>
        /// 停止 Gifski
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        internal void Stop()
        {
            if (gifskiHandle != 0)
            {
                GifskiError result = _finish(gifskiHandle);
                if (result != GifskiError.GIFSKI_OK)
                    throw new InvalidOperationException($"销毁 Gifski 句柄失败：{result}");
                gifskiHandle = 0;
            }
        }

        /// <summary>
        /// 增加帧
        /// </summary>
        /// <param name="path">图像文件路径</param>
        /// <param name="index">图像在 Gif 文件中的序号</param>
        /// <param name="ipts">图像相对于开始录制时的时间，单位为毫秒</param>
        internal void AddFrame(string path, uint index, uint ipts) => _addPngFrame(gifskiHandle, index, path, ipts / 1000d);

        /// <summary>
        /// 增加帧
        /// </summary>
        /// <param name="source">图像像素集</param>
        /// <param name="index">图像在 Gif 文件中的序号</param>
        /// <param name="ipts">图像相对于开始录制时的时间，单位为毫秒</param>
        //internal void AddFrame(BitmapSource source, uint index, uint ipts)
        //{
        //    PixelTool tool = new(source);
        //    tool.HandlePixelInfos();
        //    _ = _addRgbFrame(gifskiHandle, index, tool.Width, tool.BytesPerRow, tool.Height, tool.PixelsAddress, ipts / 1000d);
        //    tool.Dispose();
        //}

        #region 加载 DLL 函数
        private static T LoadFunction<T>(string dllPath, string functionName) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(GetProcAddress(LoadLibrary(dllPath), functionName));
        #region Windows SDKs -> libloaderapi.h
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpLibFileName);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        #endregion
        #endregion
    }
}
