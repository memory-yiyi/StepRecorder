using StepRecorder.Core.Components.RecordTools;
using System.Windows;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 记录器的状态类，维护记录器当前运行状态
    /// </summary>
    public class RecordState(RecordState.NoteDelegate noteDelegate)
    {
        #region 状态控制
        private State currentState = new Stop();

        /// <summary>
        /// 切换当前状态为指定状态
        /// </summary>
        /// <param name="nextState">将要切换到的状态</param>
        public void ChangeCurrentState(string nextState)
        {
            if (nextState != GetCurrentState() || nextState == "Stop")
            {
                switch (nextState)
                {
                    case "Record":
                        // 根据推算，下面两个都可以，任选其一便可
                        currentState.ChangeState(this, false);
                        // recordState.ChangeCurrentState(this, null);
                        break;
                    case "Pause":
                        currentState.ChangeState(this, false);
                        break;
                    case "Note":
                        currentState.ChangeState(this, null);
                        break;
                    case "Stop":
                        currentState.ChangeState(this, true);
                        break;
                }
            }
        }

        public string GetCurrentState() => currentState.GetType().Name;
        internal void SetCurrentState(State state) => currentState = state;
        #endregion

        #region 录制工具
        internal void StartRecord()
        {
            mkbHook.Install();
        }

        internal void PauseRecord()
        {
            mkbHook.Stop();
        }

        internal void ContinueRecord()
        {
            mkbHook.Start();
        }

        internal void StopRecord()
        {
            mkbHook.Uninstall();
        }
        #region 键鼠钩子
        public record NoteContent(string Short, string Detail);
        public delegate NoteContent? NoteDelegate();

        private readonly Hook mkbHook = new();
        private readonly List<(int, NoteContent)> notes = [];

        /// <summary>
        /// 设置鼠标不录制区域，默认禁用该设置
        /// </summary>
        /// <param name="rect">一个区域，当其为 Rect.Empty 时，禁用该功能</param>
        public void SetMouseNotRecordArea(Rect rect) => mkbHook.MouseNotRecordArea = rect;
        internal void GetNoteContent()
        {
             if (noteDelegate.Invoke() is NoteContent nc)
            {
                mkbHook.RecordNote();
                notes.Add((mkbHook.GetCurrentKeyframeNum(), nc));
            }
        }
        #endregion
        #endregion
    }
}
