﻿namespace StepRecorder.Core.Components.RecordTools
{
    internal abstract class State
    {
        internal abstract void ChangeState(RecordState recordState, bool? stopSign);
    }

    internal class Record : State
    {
        internal override void ChangeState(RecordState recordState, bool? stopSign)
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
        internal override void ChangeState(RecordState recordState, bool? stopSign)
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
        internal override void ChangeState(RecordState recordState, bool? stopSign)
        {   // 切换到Record
            recordState.SetCurrentState(new Record());
        }
    }

    internal class Stop : State
    {
        internal override void ChangeState(RecordState recordState, bool? stopSign)
        {
            if (stopSign == true)
            {   // End，录制结束
                recordState.UninstallHook();
            }
            else
            {   // 切换到Record
                recordState.InstallHook();
                recordState.SetCurrentState(new Record());
            }
        }
    }
}
