using System;

namespace TouchpadToMiddleClick
{
    // 触摸板设备信息
    public class TouchpadDeviceInfo
    {
        public string DeviceId { get; set; } = "";
        public string VendorId { get; set; } = "";
        public string ProductId { get; set; } = "";
        public IntPtr Hid { get; set; }

        public override string ToString()
        {
            return $"{DeviceId}({ProductId}:{VendorId})";
        }
    }

    // 触摸点信息
    public class TouchpadContact
    {
        public int ContactId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString()
        {
            return $"[手指ID:{ContactId} 坐标:({X},{Y})]";
        }
    }
}