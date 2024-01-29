using StepRecorder.Core.Events;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace StepRecorder.Core.Components
{
    /// <summary>
    /// 键鼠钩子，用于记录键鼠操作
    /// </summary>
    internal class InputHook
    {
        #region Windows SDKs -> WinUser.h
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private enum MouseDown : uint { LB = 0x0201, RB = 0x0204, MB = 0x0207 }
        private enum MouseUp : uint { LB = 0x0202, RB = 0x0205, MB = 0x0208 }

        /// <summary>
        /// 定义点的 x 坐标和 y 坐标。
        /// </summary>
        /// <see cref="https://learn.microsoft.com/zh-cn/windows/win32/api/windef/ns-windef-point"/>
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int x;
            public int y;
        }

        /// <summary>
        /// 包含有关低级别鼠标输入事件的信息。
        /// </summary>
        /// <see cref="https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-msllhookstruct"/>
        [StructLayout(LayoutKind.Sequential)]
        private class MSLLHOOK
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }


        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        private enum VKSingle { Esc = 0x1B, End = 0x23, Home, F1 = 0x70, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 }
        private enum VKCombo { LCtrl = 0xA2, RCtrl, LAlt, RAlt, LWin = 0x5B, RWin }
        private enum VKOpt { LShift = 0xA0, RShift }

        /// <summary>
        /// 包含有关低级别键盘输入事件的信息。
        /// </summary>
        /// <see cref="https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-kbdllhookstruct"/>
        [StructLayout(LayoutKind.Sequential)]
        private class KBDLLHOOK
        {
            public int vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowsHookEx(int idHook, Hookproc lpfn, IntPtr hmod, int dwThreadId);
        [DllImport("user32.dll")]
        private static extern int UnhookWindowsHookEx(int hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(int hhk, int nCode, uint wParam, IntPtr lParam);
        #endregion

        #region 构建方法
        // 构建时就创建钩子，减轻了启动任务时分配资源的负担
        // 但是如果启动任务之前的输入操作过多就会浪费资源
        // 请自行权衡利弊
        public InputHook(bool createHook)
        {
            if (createHook)
                Start();
        }
        public InputHook() : this(true) { }
        ~InputHook()
        {
            Stop();
        }
        internal void Start()
        {
            if (mouseHookHandle == 0)
            {
                mouseHookproc = LowLevelMouseProc;
                mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, mouseHookproc, IntPtr.Zero, 0);
                if (mouseHookHandle == 0)
                    throw new InvalidOperationException("鼠标钩子安装异常");
            }
            if (keyboardHookHandle == 0)
            {
                keyboardHookproc = LowLevelKeyboardProc;
                keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardHookproc, IntPtr.Zero, 0);
                if (keyboardHookHandle == 0)
                    throw new InvalidOperationException("键盘钩子安装异常");
            }
        }
        internal void Stop()
        {
            if (mouseHookHandle != 0)
            {
                if (UnhookWindowsHookEx(mouseHookHandle) == 0)
                    throw new InvalidOperationException("鼠标钩子卸载异常");
                mouseHookHandle = 0;
            }
            if (keyboardHookHandle != 0)
            {
                if (UnhookWindowsHookEx(keyboardHookHandle) == 0)
                    throw new InvalidOperationException("键盘钩子卸载异常");
                keyboardHookHandle = 0;
            }
        }
        #endregion

        #region 变量
        private int mouseHookHandle = 0;
        private int keyboardHookHandle = 0;

        private bool isMouseMove = false;
        private bool isComboKey = false;
        private readonly object comboKeyLock = new();
        private readonly HashSet<int> keys = [];
        #endregion

        #region 委托与事件
        private delegate IntPtr Hookproc(int code, uint wParam, IntPtr lParam);
        internal delegate void DIYMouseEventHandler(object sender, DIYMouseEventArgs e);
        internal delegate void DIYKeyEventHandler(object sender, DIYKeyEventArgs e);


        private Hookproc? mouseHookproc;
        private Hookproc? keyboardHookproc;

        internal event DIYMouseEventHandler? MouseOper;
        internal event DIYKeyEventHandler? KeyOper;
        #endregion

        #region 方法
        /// <summary>
        /// 与 SetWindowsHookEx 函数一起使用的应用程序定义或库定义的回调函数。
        /// 每当新的鼠标输入事件即将发布到线程输入队列时，系统都会调用此函数。
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        /// <see cref="https://learn.microsoft.com/zh-cn/windows/win32/winmsg/lowlevelmouseproc"/>
        private IntPtr LowLevelMouseProc(int nCode, uint wParam, IntPtr lParam)
        {
            if (nCode < 0 || MouseOper == null)
                return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);

            bool handled = false;
            if (wParam == WM_MOUSEMOVE)
                isMouseMove = true;
            else
            {
                foreach (var mouseOper in Enum.GetValues<MouseDown>())
                {
                    if (wParam == (uint)mouseOper)
                    {
                        isMouseMove = false;
                        break;
                    }
                    else if (wParam == (uint)Enum.Parse<MouseUp>(mouseOper.ToString()) && !isMouseMove)
                    {
                        MSLLHOOK mouse = Marshal.PtrToStructure<MSLLHOOK>(lParam)!;
                        /*
                         * 代码块复制粘贴标志
                         * 带有相同标志的代码块说明其由复制粘贴实现，修改时注意同步更改
                         * 第一次出现的标志带有数量，代表该代码块有几个副本（你修改时的工作量）
                         * 温馨提示：第一次不算副本
                         */
                        // COPY_ExecuteEvent(2)
                        var e = new DIYMouseEventArgs(mouseOper.ToString(), mouse.pt, mouse.time, Keyboard.IsKeyDown(Key.LeftCtrl));
                        MouseOper.Invoke(this, e);
                        handled = e.Handled;
                        // endCOPY_ExecuteEvent
                        break;
                    }
                }
            }

            return handled ? new IntPtr(1) : CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
        }

        /// <summary>
        /// 与 SetWindowsHookEx 函数一起使用的应用程序定义或库定义的回调函数。
        /// 每当新的键盘输入事件即将发布到线程输入队列时，系统都会调用此函数。
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        /// <see cref="https://learn.microsoft.com/zh-cn/windows/win32/winmsg/lowlevelkeyboardproc"/>
        private IntPtr LowLevelKeyboardProc(int nCode, uint wParam, IntPtr lParam)
        {
            if (nCode < 0 || KeyOper == null)
                return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);

            bool handled = false;
            bool findComboKey = false;
            KBDLLHOOK keyboard = Marshal.PtrToStructure<KBDLLHOOK>(lParam)!;
            lock (comboKeyLock)
            {
                // 判断是否按下了组合键
                foreach (var vk in Enum.GetValues<VKCombo>())
                {
                    if (keyboard.vkCode == (int)vk)
                    {
                        findComboKey = true;
                        keys.Add(keyboard.vkCode);
                        isComboKey = true;
                        break;
                    }
                }
                // 判断是否按下了可选键
                if (isComboKey)
                {
                    foreach (var vk in Enum.GetValues<VKOpt>())
                    {
                        if (keyboard.vkCode == (int)vk)
                        {
                            findComboKey = true;
                            keys.Add(keyboard.vkCode);
                            break;
                        }
                    }
                }
            }
            // 判断是否退出组合键
            if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
            {
                if (isComboKey && !findComboKey)
                {
                    lock (comboKeyLock)
                    {
                        /*
                         * 如果有多个可选键
                         * 1）调整判断策略
                         * 2）将组合键和可选键分开处理
                         * 3）同时按了LShift和RShift
                         * 对于1）和2）意味着增加开销，所以如果遇到3）先了解下情况
                         * 假设是按着好玩，那就叫他爬，不可以因为找茬的而增大开销
                         */
                        if (this.keys.Count != 1 || Enum.GetName((VKOpt)this.keys.ElementAt(0)) == null)
                        {
                            List<string> keys = [];
                            foreach (var key in this.keys)
                                keys.Add(Enum.GetName((VKCombo)key) ?? Enum.GetName((VKOpt)key)!);
                            // COPY_ExecuteEvent
                            var e = new DIYKeyEventArgs(KeyInterop.KeyFromVirtualKey(keyboard.vkCode), keys);
                            KeyOper.Invoke(this, e);
                            handled = e.Handled;
                            // endCOPY_ExecuteEvent
                        }
                        else
                        {
                            keys.Clear();
                            isComboKey = false;
                        }
                    }
                }
            }
            // 判断是否为功能键，以及响应手动取消组合键状态
            if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP)
            {
                if (isComboKey)
                {
                    if (findComboKey)
                    {
                        lock (comboKeyLock)
                        {
                            keys.Remove(keyboard.vkCode);
                            if (keys.Count == 0)
                                isComboKey = false;
                        }
                    }
                }
                else
                {
                    foreach (var vk in Enum.GetValues<VKSingle>())
                    {
                        if (keyboard.vkCode == (int)vk)
                        {
                            // COPY_ExecuteEvent
                            var e = new DIYKeyEventArgs(KeyInterop.KeyFromVirtualKey(keyboard.vkCode));
                            KeyOper.Invoke(this, e);
                            handled = e.Handled;
                            // endCOPY_ExecuteEvent
                            break;
                        }
                    }
                }
            }

            return handled ? new IntPtr(1) : CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }
        #endregion
    }
}
