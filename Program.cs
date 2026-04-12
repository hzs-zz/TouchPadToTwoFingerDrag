using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading;
using Timer = System.Timers.Timer;
namespace TouchpadToMiddleClick
{
    class Program
    {
        static TwoFingerLogic logicBrain = new TwoFingerLogic();
        static bool isEnabled = false;

        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" SolidWorks 触摸板双指中键驱动 (无缝拦截终极版)");
            Console.WriteLine(" 快捷键: 按 [Alt + Shift + M] 随时开启或暂停");
            Console.WriteLine("==================================================\n");
            Console.WriteLine("【重要提示】：请确保 Windows 系统设置中的“双指滚动”处于开启状态！\n");

            if (!TouchpadHelper.Exists()) return;
            IntPtr hwnd = TouchpadHelper.RegisterInput();
            if (hwnd == IntPtr.Zero) return;

            // ================= 【启动拦截器】 =================
            MouseHookManager.Start();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => MouseHookManager.Stop();
            Console.CancelKeyPress += (s, e) => MouseHookManager.Stop();

            TouchpadHelper.OnContactsReceived = MyDataProcessor;

            TouchpadHelper.OnToggleHotkey = () => 
            {
                isEnabled = !isEnabled;
                // 将状态同步给底层的拦截器
                MouseHookManager.IsDriverActive = isEnabled; 

                if (isEnabled)
                {
                    Console.WriteLine("\n>>> [系统] 状态: ON (已接管触摸板，禁用系统滚动)");
                }
                else
                {
                    Console.WriteLine("\n>>> [系统] 状态: OFF (已释放触摸板，恢复系统滚动)");
                    logicBrain.Process(new List<TouchpadContact>()); 
                }
            };

            Console.WriteLine(">>> 驱动初始化成功！当前状态off");
            TouchpadHelper.RunMessageLoop(); 
        }

        static void MyDataProcessor(List<TouchpadContact> contacts)
        {
            // 告诉拦截器：当前触摸板上有没有手指？
            // 只有当有手指在触摸板上时，才拦截滚轮（为了不影响实体鼠标的滚轮）
            MouseHookManager.IsTouchpadActive = contacts.Count > 0;

            if (isEnabled) logicBrain.Process(contacts);
        }
    }

    // =======================================================
    // 状态机大脑：处理双指按下、位移、释放
    // =======================================================
// =======================================================
    // 状态机大脑：处理双指按下、位移、释放 (看门狗防卡死版)
    // =======================================================
// =======================================================
    // 状态机大脑：平移 + 捏合缩放 + 看门狗防卡死版
    // =======================================================
// =======================================================
    // 状态机大脑：意图锁定(防误触) + 平移/缩放 + 看门狗防卡死
    // =======================================================
// =======================================================
    // 状态机大脑：高精度无级缩放 + 平移 + 看门狗防卡死
    // =======================================================
// =======================================================
    // 状态机大脑：意图锁定 + 异步平移/缩放 + 看门狗防卡死
    // =======================================================
    public class TwoFingerLogic
    {
        private enum GestureMode { Undecided, Panning, Zooming }

        private bool _isDragging = false;
        private bool _isMiddleDown = false; 
        private GestureMode _currentMode = GestureMode.Undecided;

        private Dictionary<int, TouchpadContact> _lastContacts = new();
        
        // 意图判定阈值
        private const double INTENT_THRESHOLD = 15.0;
        private double _intentPanAcc = 0;
        private double _intentZoomAcc = 0;

        // 旋转平移变量
        private const double SENSITIVITY = 0.2; 
        private double _remX = 0;
        private double _remY = 0;

        // 捏合缩放变量
        private double _lastDistance = 0;
        private double _zoomAccumulator = 0;
        // 缩放触发阈值 (建议 20~40，越小越灵敏)
        private const double ZOOM_THRESHOLD = 25.0; 

        private Timer _watchdogTimer;
        private readonly object _lockObj = new object();

        public TwoFingerLogic()
        {
            _watchdogTimer = new Timer(150);
            _watchdogTimer.AutoReset = false; 
            _watchdogTimer.Elapsed += (s, e) => ForceRelease("超时未移动 (看门狗咬断)");
        }

        public void Process(List<TouchpadContact> currentContacts)
        {
            lock (_lockObj)
            {
                int count = currentContacts.Count;
                _watchdogTimer.Stop(); 

                if (count < 2 && _isDragging)
                {
                    ForceRelease("手指正常抬起");
                    return;
                }

                if (count == 2)
                {
                    int totalDx = 0, totalDy = 0, matchCount = 0;

                    foreach (var current in currentContacts)
                    {
                        if (_lastContacts.TryGetValue(current.ContactId, out var old))
                        {
                            totalDx += (current.X - old.X);
                            totalDy += (current.Y - old.Y);
                            matchCount++;
                        }
                    }

                    double avgDx = matchCount > 0 ? (double)totalDx / matchCount : 0;
                    double avgDy = matchCount > 0 ? (double)totalDy / matchCount : 0;
                    double panMagnitude = Math.Sqrt(avgDx * avgDx + avgDy * avgDy);

                    double currentDistance = CalculateDistance(currentContacts[0], currentContacts[1]);
                    double deltaDistance = _isDragging ? (currentDistance - _lastDistance) : 0;

                    bool hasPhysicalMovement = panMagnitude > 0 || Math.Abs(deltaDistance) > 0;

                    if (hasPhysicalMovement)
                    {
                        if (!_isDragging)
                        {
                            _isDragging = true;
                            _currentMode = GestureMode.Undecided;
                            _intentPanAcc = 0;
                            _intentZoomAcc = 0;
                            _zoomAccumulator = 0;
                            _remX = 0; _remY = 0;
                            _isMiddleDown = false;
                        }
                        else
                        {
                            if (_currentMode == GestureMode.Undecided)
                            {
                                _intentPanAcc += panMagnitude;
                                _intentZoomAcc += Math.Abs(deltaDistance);

                                if (_intentPanAcc > INTENT_THRESHOLD)
                                {
                                    _currentMode = GestureMode.Panning;
                                    _isMiddleDown = true;
                                    MouseSimulator.MiddleDown();
                                }
                                else if (_intentZoomAcc > INTENT_THRESHOLD)
                                {
                                    _currentMode = GestureMode.Zooming;
                                }
                            }

                            if (_currentMode == GestureMode.Panning)
                            {
                                double moveX = avgDx * SENSITIVITY + _remX;
                                double moveY = avgDy * SENSITIVITY + _remY;

                                int actX = (int)moveX;
                                int actY = (int)moveY;

                                _remX = moveX - actX;
                                _remY = moveY - actY;

                                if (actX != 0 || actY != 0)
                                {
                                    MouseSimulator.Move(actX, actY);
                                }
                            }
                            else if (_currentMode == GestureMode.Zooming)
                            {
                                _zoomAccumulator += deltaDistance;

                                if (_zoomAccumulator > ZOOM_THRESHOLD)
                                {
                                    int clicks = (int)(_zoomAccumulator / ZOOM_THRESHOLD);
                                    MouseSimulator.Scroll(clicks * 120); 
                                    _zoomAccumulator -= clicks * ZOOM_THRESHOLD;
                                }
                                else if (_zoomAccumulator < -ZOOM_THRESHOLD)
                                {
                                    int clicks = (int)(-_zoomAccumulator / ZOOM_THRESHOLD);
                                    MouseSimulator.Scroll(-clicks * 120); 
                                    _zoomAccumulator += clicks * ZOOM_THRESHOLD;
                                }
                            }
                        }
                        
                        _watchdogTimer.Start(); 
                    }
                    else if (_isDragging)
                    {
                        _watchdogTimer.Start();
                    }

                    _lastContacts = currentContacts.ToDictionary(c => c.ContactId);
                    _lastDistance = currentDistance; 
                }
                else
                {
                    _lastContacts.Clear();
                }
            }
        }

        private void ForceRelease(string reason)
        {
            lock (_lockObj)
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    if (_isMiddleDown)
                    {
                        MouseSimulator.MiddleUp();
                        _isMiddleDown = false;
                    }
                    _lastContacts.Clear();
                }
            }
        }

        private double CalculateDistance(TouchpadContact p1, TouchpadContact p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    // =======================================================
    // 鼠标模拟器：异步多线程发射版 (彻底解决卡顿与死锁)
    // =======================================================
    public static class MouseSimulator
    {
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800; 

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        // 【核心修改】：利用 ThreadPool 将底层信号发射扔到后台，解放主线程！
        public static void Move(int dx, int dy) 
        { 
            ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, 0)); 
        }
        
        public static void MiddleDown() 
        { 
            ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0)); 
        }
        
        public static void MiddleUp() 
        { 
            ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0)); 
        }
        
        public static void Scroll(int delta) 
        { 
            ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, 0)); 
        }
    }
    // =======================================================
    // 新增：底层系统事件拦截器 (幽灵盾牌)
    // =======================================================
    public static class MouseHookManager
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const uint LLMHF_INJECTED = 0x00000001;
        private const uint LLMHF_LOWER_IL_INJECTED = 0x00000002;

        public static bool IsDriverActive = false;
        public static bool IsTouchpadActive = false;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void Start()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static void Stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // 如果程序开启，并且手指正放在触摸板上
            if (nCode >= 0 && IsDriverActive && IsTouchpadActive)
            {
                if (wParam == (IntPtr)WM_MOUSEWHEEL || wParam == (IntPtr)WM_MOUSEHWHEEL)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    
                    // 检查这是否是系统物理发出的真实滚轮事件 (如果是代码注入的，标志位不为0)
                    if ((hookStruct.flags & LLMHF_INJECTED) == 0 && (hookStruct.flags & LLMHF_LOWER_IL_INJECTED) == 0)
                    {
                        // 拦截掉！不传给 SolidWorks
                        return (IntPtr)1; 
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}