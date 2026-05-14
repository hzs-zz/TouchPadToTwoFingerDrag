using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TouchpadToMiddleClick;

class TouchpadCore { }

public enum InteractionMode { None, Panning, Scrolling }

public class TwoFingerLogic
{
    private enum GestureMode { Undecided, Panning, Zooming }

    private bool _isDragging = false;
    private bool _isMiddleDown = false;
    private GestureMode _currentMode = GestureMode.Undecided;

    private Dictionary<int, TouchpadContact> _lastContacts = new();

    private const double INTENT_THRESHOLD = 15.0;
    private double _intentPanAcc = 0;
    private double _intentZoomAcc = 0;

    private const double SENSITIVITY = 0.2;
    private double _remX = 0;
    private double _remY = 0;

    private double _lastDistance = 0;
    private double _zoomAccumulator = 0;
    private const double ZOOM_THRESHOLD = 25.0;

    private Timer _watchdogTimer;
    private readonly object _lockObj = new object();

    public TwoFingerLogic()
    {
        _watchdogTimer = new Timer(150);
        _watchdogTimer.AutoReset = false;
        _watchdogTimer.Elapsed += (s, e) => ForceRelease("超时未移动 (看门狗咬断)");
    }

    public void Process(List<TouchpadContact> currentContacts, InteractionMode activeMode)
    {
        lock (_lockObj)
        {
            if (activeMode == InteractionMode.None)
            {
                ForceRelease("模式未激活");
                return;
            }

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
                    if (activeMode == InteractionMode.Scrolling)
                    {
                        if (!_isDragging)
                        {
                            _isDragging = true;
                            _remY = 0;
                        }

                        _remY += avgDy * 0.8;
                        int clicks = (int)_remY;

                        if (clicks != 0)
                        {
                            MouseSimulator.Scroll(clicks * 15);
                            _remY -= clicks;
                        }
                    }
                    else if (activeMode == InteractionMode.Panning)
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

                                if (actX != 0 || actY != 0) MouseSimulator.Move(actX, actY);
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
                    }
                    _watchdogTimer.Start();
                }
                else if (_isDragging) _watchdogTimer.Start();

                _lastContacts = currentContacts.ToDictionary(c => c.ContactId);
                _lastDistance = currentDistance;
            }
            else _lastContacts.Clear();
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

public static class MouseSimulator
{
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    public static void Move(int dx, int dy) { ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, 0)); }
    public static void MiddleDown() { ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0)); }
    public static void MiddleUp() { ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0)); }
    public static void Scroll(int delta) { ThreadPool.QueueUserWorkItem(_ => mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, 0)); }
}

public static class MouseHookManager
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const uint LLMHF_INJECTED = 0x00000001;
    private const uint LLMHF_LOWER_IL_INJECTED = 0x00000002;

    public static Action<InteractionMode>? OnTargetLockChanged;
    public static InteractionMode ActiveMode { get; private set; } = InteractionMode.None;

    public static bool IsPanMasterOn = false;
    public static bool IsScrollMasterOn = false;

    // 🌟 新增：底层同步接收反向规则状态
    public static bool IsPanReverseRule = false;

    public static IEnumerable<string>? PanTargetClasses = null;
    public static IEnumerable<string>? ScrollTargetClasses = null;

    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)] private static extern IntPtr GetParent(IntPtr hWnd);

    private static int _lastTouchpadTick = 0;
    private const int INERTIA_TIMEOUT = 500;
    private static int _lastContactCount = 0;

    private static Timer _restRadarTimer;
    private static POINT _lastCursorPt;
    private static int _restTicks = 0;
    private static readonly object _evalLock = new object();

    public static void UpdateTouchpadState(int contactCount)
    {
        if (contactCount > 0) _lastTouchpadTick = Environment.TickCount;

        if (contactCount > _lastContactCount)
        {
            if (GetCursorPos(out POINT pt))
            {
                Task.Run(() => EvaluateTargetWindow(pt));
            }
        }
        _lastContactCount = contactCount;
    }

    private static void EvaluateTargetWindow(POINT pt)
    {
        lock (_evalLock)
        {
            InteractionMode prevMode = ActiveMode;
            InteractionMode currentMode = InteractionMode.None;

            IntPtr startHwnd = WindowFromPoint(pt);

            // --- 1. 判断是否触发中键平移 (支持家族树遍历与反向黑名单) ---
            if (IsPanMasterOn && PanTargetClasses != null)
            {
                bool foundMatch = false;
                IntPtr tempHwnd = startHwnd;

                // 向上扒皮遍历
                while (tempHwnd != IntPtr.Zero)
                {
                    StringBuilder classNameBuilder = new StringBuilder(256);
                    GetClassName(tempHwnd, classNameBuilder, 256);
                    if (PanTargetClasses.Any(c => classNameBuilder.ToString().IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        foundMatch = true;
                        break; // 只要族谱里有一个符合，立刻跳出
                    }
                    tempHwnd = GetParent(tempHwnd);
                }

                // 🌟 核心逻辑：
                // 如果开启了反向规则（黑名单），没碰到黑名单才开启
                // 如果是正向规则（白名单），碰到了白名单才开启
                if (IsPanReverseRule ? !foundMatch : foundMatch)
                {
                    currentMode = InteractionMode.Panning;
                }
            }

            // --- 2. 判断是否触发滚动模拟 (如果没触发平移) ---
            if (currentMode == InteractionMode.None && IsScrollMasterOn && ScrollTargetClasses != null)
            {
                bool foundMatch = false;
                IntPtr tempHwnd = startHwnd;

                while (tempHwnd != IntPtr.Zero)
                {
                    StringBuilder classNameBuilder = new StringBuilder(256);
                    GetClassName(tempHwnd, classNameBuilder, 256);
                    if (ScrollTargetClasses.Any(c => classNameBuilder.ToString().IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        foundMatch = true;
                        break;
                    }
                    tempHwnd = GetParent(tempHwnd);
                }

                if (foundMatch)
                {
                    currentMode = InteractionMode.Scrolling;
                }
            }

            if (prevMode != currentMode)
            {
                ActiveMode = currentMode;
                OnTargetLockChanged?.Invoke(currentMode);
            }
        }
    }

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

        _restRadarTimer = new Timer(30);
        _restRadarTimer.Elapsed += RestRadar_Tick;
        _restRadarTimer.Start();
    }

    public static void Stop()
    {
        UnhookWindowsHookEx(_hookID);
        _restRadarTimer?.Stop();
    }

    private static void RestRadar_Tick(object sender, ElapsedEventArgs e)
    {
        if (!(IsPanMasterOn || IsScrollMasterOn) || _lastContactCount >= 2) return;

        if (GetCursorPos(out POINT pt))
        {
            if (pt.x != _lastCursorPt.x || pt.y != _lastCursorPt.y)
            {
                _lastCursorPt = pt;
                _restTicks = 0;
            }
            else
            {
                _restTicks++;
                if (_restTicks == 1)
                {
                    EvaluateTargetWindow(pt);
                }
            }
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            if (wParam == (IntPtr)WM_MOUSEWHEEL || wParam == (IntPtr)WM_MOUSEHWHEEL)
            {
                if (ActiveMode == InteractionMode.None)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                if ((hookStruct.flags & LLMHF_INJECTED) == 0 && (hookStruct.flags & LLMHF_LOWER_IL_INJECTED) == 0)
                {
                    int elapsed = Environment.TickCount - _lastTouchpadTick;
                    if (elapsed >= 0 && elapsed < INERTIA_TIMEOUT)
                    {
                        return (IntPtr)1;
                    }
                }
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string lpModuleName);
}