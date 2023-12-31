using StepRecorder.Components.Record;

namespace StepRecorder.Components
{
    /// <summary>
    /// 记录器的状态类，维护记录器当前运行状态
    /// </summary>
    public class RecordState
    {
        private State currentState;

        public RecordState() => currentState = new Stop();

        public void ChangeCurrentState(bool? stopSign) => this.currentState.ChangeState(this, stopSign);

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
                    recordState.ChangeCurrentState(false);
                }   // 完成后自动跳转到Record
                else if (stopSign == false)
                {   // 切换到Pause
                    recordState.SetCurrentState(new Pause());
                }
                else
                {   // 切换到Stop
                    recordState.SetCurrentState(new Stop());
                    recordState.ChangeCurrentState(true);
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
                    recordState.ChangeCurrentState(true);
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
