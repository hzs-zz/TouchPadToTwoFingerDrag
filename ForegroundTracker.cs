using System;
using System.Collections.ObjectModel; // 🌟 必须引入
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using Timer = System.Timers.Timer;
namespace TouchpadToMiddleClick
{
    public class ForegroundTracker
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private System.Timers.Timer wadpy_pn_RadarTimer;
        private string wadpy_pn_LastProcessName = "";

        public Action<string, bool>? OnFocusChanged;

        // 🌟 核心改动：使用可观察集合，方便 UI 同步更新
        public ObservableCollection<string> TargetProcesses { get; set; }

        public ForegroundTracker(ObservableCollection<string> sharedList)
        {
            // 直接引用外部传入的集合地址
            TargetProcesses = sharedList;

            wadpy_pn_RadarTimer = new Timer(300);
            wadpy_pn_RadarTimer.Elapsed += Wadpy_pn_RadarTimer_Elapsed;
        }

        public void Start() => wadpy_pn_RadarTimer.Start();
        public void Stop() => wadpy_pn_RadarTimer.Stop();

        private void Wadpy_pn_RadarTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                Process proc = Process.GetProcessById((int)pid);
                string currentProcess = proc.ProcessName;

                if (currentProcess != wadpy_pn_LastProcessName)
                {
                    wadpy_pn_LastProcessName = currentProcess;

                    // 🌟 动态匹配：检查当前进程是否在用户自定义的名单中
                    bool isTarget = TargetProcesses.Any(p =>
                        string.Equals(p, currentProcess, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p + ".exe", currentProcess, StringComparison.OrdinalIgnoreCase));

                    OnFocusChanged?.Invoke(currentProcess, isTarget);
                }
            }
            catch { }
        }
    }
}