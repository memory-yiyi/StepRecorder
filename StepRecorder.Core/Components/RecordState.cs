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

        #region 工具
        internal Hook MKbHook { get; } = new();
        public void SetMouseNotRecordArea(Rect rect) => MKbHook.MouseNotRecordArea = rect;
        #endregion
    }
}
