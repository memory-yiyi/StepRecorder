﻿using StepRecorder.Core.Components.RecordTools;
using System.Windows;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 记录器的状态类，维护记录器当前运行状态
    /// </summary>
    /// <param name="noteDelegate">一个委托，用于接收从其它类获取的注释帧信息</param>
    /// <param name="getSaveInfo">一个委托，用于接收从其它类获取的有关保存的信息</param>
    public class RecordState(RecordState.NoteDelegate noteDelegate, RecordState.GetSaveInfoDelegate getSaveInfo)
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
            if (recordTool == null)
                throw new NullReferenceException("未指定录制区域");
            recordTool.Start();
            mkbHook.Install();
            mkbHook.CatchKeyframe += recordTool.AddKeyframe;
        }

        internal void PauseRecord()
        {
            mkbHook.Stop();
            recordTool!.Suspend();
        }

        internal void ContinueRecord()
        {
            recordTool!.Resume();
            mkbHook.Start();
        }

        internal void StopRecord()
        {
            mkbHook.Uninstall();
            mkbHook.CatchKeyframe -= recordTool!.AddKeyframe;
            recordTool.Stop();
            Save();
        }

        private RecordTool? recordTool;
        public void SetRecordArea(Rect rect) => recordTool = new RecordTool(
            new System.Drawing.Rectangle(
                (int)(rect.Left * ProcessInfo.Scaling),
                (int)(rect.Top * ProcessInfo.Scaling),
                (int)(rect.Width * ProcessInfo.Scaling),
                (int)(rect.Height * ProcessInfo.Scaling)));

        #region 钩子
        public record NoteContent(string Short, string Detail);
        private record NoteInfo(int KeyframeNo, NoteContent NoteContent);
        /// <summary>
        /// 接收从其它类获取的注释帧信息
        /// </summary>
        /// <returns>返回值为 null 时，取消本次捕获注释帧的请求</returns>
        public delegate NoteContent? NoteDelegate();

        private readonly Hook mkbHook = new();
        private readonly List<NoteInfo> notes = [];

        /// <summary>
        /// 设置鼠标不录制区域，默认禁用该设置
        /// </summary>
        /// <param name="rect">一个区域，当其为 Rect.Empty 时，禁用该功能</param>
        public void SetMouseNotRecordArea(Rect rect) => mkbHook.MouseNotRecordArea = rect;
        /// <summary>
        /// 发起捕获注释帧请求，从其它类获取注释帧信息
        /// </summary>
        internal void GetNoteContent()
        {
            if (noteDelegate() is NoteContent nc)
            {
                mkbHook.RecordNote();
                notes.Add(new NoteInfo(mkbHook.GetCurrentKeyframeCount() - 1, nc));
            }
        }
        #endregion
        #endregion

        #region 保存单元
        /// <summary>
        /// 接收从其它类获取的有关保存的信息
        /// </summary>
        /// <returns>返回 null 时，取消保存</returns>
        public delegate string? GetSaveInfoDelegate();

        public ProjectFile? ProjectFile { get; private set; }
        public Task? SaveTask { get; private set; }

        private void Save()
        {
            if (getSaveInfo() is string savePath)
            {
                ProjectFile = new ProjectFile(savePath, SaveKeyframes());
                SaveTask = Task.Run(() =>
                {
                    while (true)
                    {
                        if (recordTool!.GetCurrentMergeCount() == recordTool.GetCurrentFrameCount())
                        {
                            ProjectFile!.SaveFromRecord();
                            break;
                        }
                        Thread.Sleep(1000);     // 检测间隔
                    }
                });
            }
            else
                recordTool!.CancelMerge();
        }

        private List<KeyframeInfo> SaveKeyframes()
        {
            List<KeyframeInfo> keyframes = [];
            for (int i = 0; i < mkbHook.GetCurrentKeyframeCount(); ++i)
                keyframes.Add(new KeyframeInfo(i + 1, recordTool![i], mkbHook[i]));
            foreach (var note in notes)
            {
                keyframes[note.KeyframeNo].ShortNote = note.NoteContent.Short;
                keyframes[note.KeyframeNo].DetailNote = note.NoteContent.Detail;
                keyframes[note.KeyframeNo].IsKey = false;
            }
            return keyframes;
        }

        public double GetSaveProgress() => (double)recordTool!.GetCurrentMergeCount() / recordTool.GetCurrentFrameCount();
        #endregion
    }
}
