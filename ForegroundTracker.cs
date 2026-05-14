using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

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

        // 🌟 接口升级：传出 (进程名, 中键配置, 滚动配置)
        public Action<string, ProcessConfig?, ProcessConfig?>? OnFocusChanged;

        private ConfigContainer _config;

        public ForegroundTracker(ConfigContainer config)
        {
            _config = config;
            wadpy_pn_RadarTimer = new System.Timers.Timer(300);
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

                    // 在平移列表中寻找
                    var panConfig = _config.PanProcesses.FirstOrDefault(p =>
                        string.Equals(p.ProcessName, currentProcess, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProcessName + ".exe", currentProcess, StringComparison.OrdinalIgnoreCase));

                    // 在模拟滚动列表中寻找
                    var scrollConfig = _config.ScrollProcesses.FirstOrDefault(p =>
                        string.Equals(p.ProcessName, currentProcess, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProcessName + ".exe", currentProcess, StringComparison.OrdinalIgnoreCase));

                    OnFocusChanged?.Invoke(currentProcess, panConfig, scrollConfig);
                }
            }
            catch { }
        }
    }
}