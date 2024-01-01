using StepRecorder.Core.Components.Record;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 记录器的状态类，维护记录器当前运行状态
    /// </summary>
    public class RecordState
    {
        private State currentState;

        public RecordState() => currentState = new Stop();

        /// <summary>
        /// 切换当前状态为指定状态
        /// </summary>
        /// <param name="nextState">将要切换到的状态</param>
        public void ChangeCurrentState(string nextState)
        {
            if (nextState != GetCurrentState())
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

        public void SetCurrentState(State state) => currentState = state;
    }

    namespace Record
    {
        public abstract class State
        {
            public abstract void ChangeState(RecordState recordState, bool? stopSign);
        }

        internal class Record : State
        {
            public override void ChangeState(RecordState recordState, bool? stopSign)
            {
                if (stopSign == null)
                {   // 切换到Note
                    recordState.SetCurrentState(new Note());
                    recordState.ChangeCurrentState("Record");
                }   // 完成后自动跳转到Record
                else if (stopSign == false)
                {   // 切换到Pause
                    recordState.SetCurrentState(new Pause());
                }
                else
                {   // 切换到Stop
                    recordState.SetCurrentState(new Stop());
                    recordState.ChangeCurrentState("Stop");
                }   // 完成后自动跳转到End
            }
        }

        internal class Pause : State
        {
            public override void ChangeState(RecordState recordState, bool? stopSign)
            {
                if (stopSign == true)
                {   // 切换到Stop
                    recordState.SetCurrentState(new Stop());
                    recordState.ChangeCurrentState("Stop");
                }   // 完成后自动跳转到End
                else
                {   // 切换到Record
                    recordState.SetCurrentState(new Record());
                }
            }
        }

        internal class Note : State
        {
            public override void ChangeState(RecordState recordState, bool? stopSign)
            {   // 切换到Record
                recordState.SetCurrentState(new Record());
            }
        }

        internal class Stop : State
        {
            public override void ChangeState(RecordState recordState, bool? stopSign)
            {
                if (stopSign == true)
                {   // End，录制结束
                }
                else
                {   // 切换到Record
                    recordState.SetCurrentState(new Record());
                }
            }
        }
    }
}
