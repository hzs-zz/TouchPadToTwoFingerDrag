using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace TouchpadToMiddleClick
{
    public static class TouchpadHelper
    {
        
        // 向外抛出干净数据的委托回调
        public static Action<List<TouchpadContact>>? OnContactsReceived;
public static Action? OnToggleHotkey; 
        public const uint WM_HOTKEY = 0x0312;
        public const uint WM_INPUT = 0x00FF;
        private const uint RIDEV_INPUTSINK = 0x00000100;
        private const uint RIDEV_DEVNOTIFY = 0x00002000;
        private const uint RID_INPUT = 0x10000003;
        private const ushort HID_USAGE_PAGE_DIGITIZER = 0x0D;
        private const ushort HID_USAGE_DIGITIZER_TOUCH_PAD = 0x05;
        private const uint RIDI_PREPARSEDDATA = 0x20000005;
        private const uint HIDP_STATUS_SUCCESS = 0x00110000;

        private const ushort USAGE_PAGE_GENERIC = 0x01;
        private const ushort USAGE_X = 0x30;
        private const ushort USAGE_Y = 0x31;
        private const ushort USAGE_PAGE_DIGITIZER = 0x0D;
        private const ushort USAGE_TIP_SWITCH = 0x42;
        private const ushort USAGE_CONTACT_ID = 0x51;

        #region Win32 API 声明
        [StructLayout(LayoutKind.Sequential)] private struct RAWINPUTHEADER { public uint dwType; public uint dwSize; public IntPtr hDevice; public IntPtr wParam; }
        [StructLayout(LayoutKind.Sequential)] private struct RAWHID { public uint dwSizeHid; public uint dwCount; public IntPtr bRawData; }
        [StructLayout(LayoutKind.Sequential)] private struct RAWINPUT { public RAWINPUTHEADER Header; public RAWHID Hid; }
        [StructLayout(LayoutKind.Sequential)] private struct RID_DEVICE_INFO_HID { public uint dwVendorId; public uint dwProductId; public uint dwVersionNumber; public ushort usUsagePage; public ushort usUsage; }
        [StructLayout(LayoutKind.Sequential)] private struct RID_DEVICE_INFO { public uint cbSize; public uint dwType; public RID_DEVICE_INFO_HID hid; }
        [StructLayout(LayoutKind.Sequential)] private struct RAWINPUTDEVICELIST { public IntPtr hDevice; public uint dwType; }
        [StructLayout(LayoutKind.Sequential)] private struct WNDCLASSEX { public uint cbSize; public uint style; public IntPtr lpfnWndProc; public int cbClsExtra; public int cbWndExtra; public IntPtr hInstance; public IntPtr hIcon; public IntPtr hCursor; public IntPtr hbrBackground; public string? lpszMenuName; public string lpszClassName; public IntPtr hIconSm; }
        [StructLayout(LayoutKind.Sequential)] private struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam; public uint time; public POINT pt; }
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }
        private enum HIDP_REPORT_TYPE { HidP_Input = 0, HidP_Output = 1, HidP_Feature = 2 }
        [StructLayout(LayoutKind.Sequential)] private struct RAWINPUTDEVICE { public ushort usUsagePage; public ushort usUsage; public uint dwFlags; public IntPtr hwndTarget; }

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);
        [DllImport("user32.dll", SetLastError = true)] private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref MSG lpMsg);
        [DllImport("user32.dll")] private static extern void PostQuitMessage(int nExitCode);
        [DllImport("user32.dll")] private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string? lpModuleName);
        [DllImport("Hid.dll", CharSet = CharSet.Auto)] private static extern uint HidP_GetUsageValue(HIDP_REPORT_TYPE ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out uint UsageValue, IntPtr PreparsedData, IntPtr Report, uint ReportLength);
        #endregion

        public static bool Exists()
        {
            uint deviceCount = 0;
            uint structSize = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

            int result = GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, structSize);
            if (result != 0 || deviceCount == 0) return false;

            IntPtr deviceList = Marshal.AllocHGlobal((int)(structSize * deviceCount));
            try
            {
                result = GetRawInputDeviceList(deviceList, ref deviceCount, structSize);
                if (result < 0) return false;

                for (int i = 0; i < deviceCount; i++)
                {
                    IntPtr current = IntPtr.Add(deviceList, (int)(i * structSize));
                    RAWINPUTDEVICELIST ridl = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(current);

                    uint size = 0;
                    GetRawInputDeviceInfo(ridl.hDevice, 0x2000000b, IntPtr.Zero, ref size);

                    if (size > 0)
                    {
                        IntPtr data = Marshal.AllocHGlobal((int)size);
                        try
                        {
                            GetRawInputDeviceInfo(ridl.hDevice, 0x2000000b, data, ref size);
                            RID_DEVICE_INFO info = Marshal.PtrToStructure<RID_DEVICE_INFO>(data);

                            if (info.dwType == 2 && info.hid.usUsagePage == HID_USAGE_PAGE_DIGITIZER &&
                                (info.hid.usUsage == HID_USAGE_DIGITIZER_TOUCH_PAD || info.hid.usUsage == 0x04))
                            {
                                return true;
                            }
                        }
                        finally { Marshal.FreeHGlobal(data); }
                    }
                }
            }
            finally { Marshal.FreeHGlobal(deviceList); }
            return false;
        }

        public static IntPtr RegisterInput()
        {
            string className = "TouchpadMonitorWindowClass_" + Guid.NewGuid().ToString("N");

            WNDCLASSEX wcex = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProc),
                hInstance = GetModuleHandle(null),
                lpszClassName = className,
            };

            if (RegisterClassEx(ref wcex) == 0) return IntPtr.Zero;

            IntPtr hwnd = CreateWindowEx(0, className, "TouchpadMonitor", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
            if (hwnd == IntPtr.Zero) return IntPtr.Zero;

            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = HID_USAGE_PAGE_DIGITIZER;
            rid[0].usUsage = HID_USAGE_DIGITIZER_TOUCH_PAD;
            rid[0].dwFlags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY;
            rid[0].hwndTarget = hwnd;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                DestroyWindow(hwnd);
                return IntPtr.Zero;
            }
            // 注册 Alt (0x0001) + Shift (0x0004) + M (0x4D)
            RegisterHotKey(hwnd, 1, 0x0001 | 0x0004, 0x4D);
            return hwnd;
        }

        private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static readonly WndProcDelegate WndProc = (hwnd, msg, wParam, lParam) =>
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == 1)
            {
                OnToggleHotkey?.Invoke(); // 呼叫主程序
                return IntPtr.Zero;
            }
            if (msg == WM_INPUT)
            {
                var contacts = GetTouchpadContacts(lParam);
                if (contacts != null)
                {
                    OnContactsReceived?.Invoke(contacts);
                }
                return IntPtr.Zero;
            }
            return DefWindowProc(hwnd, msg, wParam, lParam);
        };

        private static List<TouchpadContact>? GetTouchpadContacts(IntPtr lParam)
        {
            uint rawInputSize = 0;
            var rawInputHeaderSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();

            if (GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref rawInputSize, rawInputHeaderSize) != 0) return null;

            IntPtr rawInputPointer = Marshal.AllocHGlobal((int)rawInputSize);
            try
            {
                if (GetRawInputData(lParam, RID_INPUT, rawInputPointer, ref rawInputSize, rawInputHeaderSize) != rawInputSize) return null;

                var rawInput = Marshal.PtrToStructure<RAWINPUT>(rawInputPointer);
                var rawInputData = new byte[rawInputSize];
                Marshal.Copy(rawInputPointer, rawInputData, 0, rawInputData.Length);

                byte[] rawHidRawData = new byte[rawInput.Hid.dwSizeHid * rawInput.Hid.dwCount];
                var rawInputOffset = (int)rawInputSize - rawHidRawData.Length;
                Buffer.BlockCopy(rawInputData, rawInputOffset, rawHidRawData, 0, rawHidRawData.Length);

                uint preparsedDataSize = 0;
                if (GetRawInputDeviceInfo(rawInput.Header.hDevice, RIDI_PREPARSEDDATA, IntPtr.Zero, ref preparsedDataSize) != 0) return null;

                IntPtr preparsedDataPointer = Marshal.AllocHGlobal((int)preparsedDataSize);
                IntPtr rawHidRawDataPointer = Marshal.AllocHGlobal(rawHidRawData.Length);
                Marshal.Copy(rawHidRawData, 0, rawHidRawDataPointer, rawHidRawData.Length);

                try
                {
                    if (GetRawInputDeviceInfo(rawInput.Header.hDevice, RIDI_PREPARSEDDATA, preparsedDataPointer, ref preparsedDataSize) != preparsedDataSize) return null;

                    var contacts = new List<TouchpadContact>();
                    for (ushort lc = 0; lc < 30; lc++)
                    {
                        uint tipSwitch = 0, x = 0, y = 0, contactId = 0;
                        uint statusTip = HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, USAGE_PAGE_DIGITIZER, lc, USAGE_TIP_SWITCH, out tipSwitch, preparsedDataPointer, rawHidRawDataPointer, (uint)rawHidRawData.Length);
                        uint statusX = HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, USAGE_PAGE_GENERIC, lc, USAGE_X, out x, preparsedDataPointer, rawHidRawDataPointer, (uint)rawHidRawData.Length);
                        uint statusY = HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, USAGE_PAGE_GENERIC, lc, USAGE_Y, out y, preparsedDataPointer, rawHidRawDataPointer, (uint)rawHidRawData.Length);
                        uint statusId = HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, USAGE_PAGE_DIGITIZER, lc, USAGE_CONTACT_ID, out contactId, preparsedDataPointer, rawHidRawDataPointer, (uint)rawHidRawData.Length);

                        if (statusX == HIDP_STATUS_SUCCESS && statusY == HIDP_STATUS_SUCCESS)
                        {
                            // 极度关键：过滤掉已经抬起但硬件还在发送残余坐标的死点
                            if (statusTip == HIDP_STATUS_SUCCESS && tipSwitch == 0) continue;
                            
                            if (x > 0 || y > 0)
                            {
                                // 防止同一手指的数据被硬件多次重复广播
                                bool isDuplicate = contacts.Any(c => c.X == (int)x && c.Y == (int)y);
                                if (isDuplicate) continue;

                                int id = (statusId == HIDP_STATUS_SUCCESS) ? (int)contactId : (int)lc;
                                contacts.Add(new TouchpadContact { ContactId = id, X = (int)x, Y = (int)y });
                            }
                        }
                    }
                    return contacts; // 当全松开时，返回的是空的 List，完美触发抬起事件！
                }
                finally { Marshal.FreeHGlobal(preparsedDataPointer); Marshal.FreeHGlobal(rawHidRawDataPointer); }
            }
            finally { Marshal.FreeHGlobal(rawInputPointer); }
        }

        public static void RunMessageLoop()
        {
            MSG msg;
            while (GetMessage(out msg, IntPtr.Zero, 0, 0)) { TranslateMessage(ref msg); DispatchMessage(ref msg); }
        }
    }
}