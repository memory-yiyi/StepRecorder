using StepRecorder.Core.Components.RecordTools;
using System.Windows;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 记录器的状态类，维护记录器当前运行状态
    /// </summary>
    public class RecordState
    {
        #region 状态控制
        private State currentState;

        public RecordState() => currentState = new Stop();

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

        #region 键鼠钩子
        private readonly Hook mkbHook = new();

        internal void InstallHook() => mkbHook.Install();
        internal void UninstallHook() => mkbHook.Uninstall();
        internal void StartHook() => mkbHook.Start();
        internal void StopHook() => mkbHook.Stop();
        /// <summary>
        /// 设置鼠标不录制区域
        /// </summary>
        /// <param name="rect">一个区域，当其 Size 属性为 Size.Empty 时，禁用该功能</param>
        public void SetMouseNotRecordArea(Rect rect) => mkbHook.MouseNotRecordArea = rect;
        #endregion
    }
}
