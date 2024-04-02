namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 关键帧信息
    /// </summary>
    /// <param name="index">当前关键帧在所有关键帧中的位置</param>
    /// <param name="frameIndex">当前关键帧在所有帧中的位置</param>
    /// <param name="inputContent">操作代码</param>
    /// <param name="shortNote">概要描述</param>
    /// <param name="detailNote">详细描述</param>
    /// <param name="isKey">关注度，默认最低</param>
    public class KeyframeInfo(int index, int frameIndex, string inputContent, string? shortNote=null, string? detailNote=null, bool? isKey=null)
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int Index { get; set; } = index;
        /// <summary>
        /// 帧序号
        /// </summary>
        public int FrameIndex { get; init; } = frameIndex;
        /// <summary>
        /// 键鼠消息
        /// </summary>
        public string InputContent { get; init; } = inputContent;
        /// <summary>
        /// 概要描述
        /// </summary>
        public string? ShortNote { get; set; } = shortNote;
        /// <summary>
        /// 详细描述
        /// </summary>
        public string? DetailNote { get; set; } = detailNote;

        /// <summary>
        /// 是否为重点关注的关键帧（出现异常或强调注意）
        /// </summary>
        /// <remarks>
        /// 三个级别：
        /// - null 正常（无问题）
        /// - false 需要警惕（可能的问题）
        /// - true 需要关注（出现的问题）
        /// </remarks>
        public bool? IsKey { get; set; } = isKey;

        public KeyframeInfo() : this(-1, -1, "&Error") { }
    }
}
