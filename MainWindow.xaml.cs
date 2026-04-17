using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace TouchpadToMiddleClick
{
    public partial class MainWindow : Window
    {
        // --------------------------------------------------------
        // 核心大脑实例化与状态标志
        // --------------------------------------------------------
        private TwoFingerLogic wadpy_pn_logicBrain = new TwoFingerLogic();
        private bool wadpy_pn_isEnabled = false;

        public MainWindow()
        {
            InitializeComponent();

            // 1. 系统上电初始化：检测触摸板并注册底层监听
            if (!TouchpadHelper.Exists())
            {
                wadpy_pn_StatusText.Text = "❌ 未检测到兼容的触摸板！";
                wadpy_pn_StatusText.Foreground = Brushes.Red;
                wadpy_pn_ToggleButton.IsEnabled = false;
                return;
            }

            IntPtr hwnd = TouchpadHelper.RegisterInput();
            if (hwnd == IntPtr.Zero) return;

            // 2. 启动鼠标拦截盾牌
            MouseHookManager.Start();

            // 3. 连线：将底层解析出的触摸点数据，喂给我们的处理器
            TouchpadHelper.OnContactsReceived = wadpy_pn_DataProcessor;

            // 4. 连线：绑定你之前在底层写好的 Alt+Shift+M 快捷键
            TouchpadHelper.OnToggleHotkey = () =>
            {
                // 因为快捷键是在后台线程触发的，修改 UI 必须回到主 UI 线程
                Dispatcher.Invoke(() => wadpy_pn_ToggleDriverState());
            };
        }

        // =======================================================
        // 中断服务函数 1：处理源源不断的手指数据
        // =======================================================
        private void wadpy_pn_DataProcessor(List<TouchpadContact> contacts)
        {
            // 告诉盾牌触摸板是否被摸着
            MouseHookManager.IsTouchpadActive = contacts.Count > 0;

            // 只有当驱动开启时，才进行复杂的捏合平移运算
            if (wadpy_pn_isEnabled)
            {
                wadpy_pn_logicBrain.Process(contacts);
            }
        }

        // =======================================================
        // 中断服务函数 2：处理界面按钮被点击
        // =======================================================
        private void wadpy_pn_ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            wadpy_pn_ToggleDriverState();
        }

        // =======================================================
        // 核心状态切换逻辑 (同时操纵硬件盾牌和前端 UI)
        // =======================================================
        private void wadpy_pn_ToggleDriverState()
        {
            wadpy_pn_isEnabled = !wadpy_pn_isEnabled;
            MouseHookManager.IsDriverActive = wadpy_pn_isEnabled;

            if (wadpy_pn_isEnabled)
            {
                wadpy_pn_StatusText.Text = "当前状态：运行中 🚀 (已接管系统滚动)";
                wadpy_pn_StatusText.Foreground = Brushes.Green;
                wadpy_pn_ToggleButton.Content = "暂停驱动";
            }
            else
            {
                wadpy_pn_StatusText.Text = "当前状态：已暂停 ⏸️ (恢复系统滚动)";
                wadpy_pn_StatusText.Foreground = Brushes.Gray;
                wadpy_pn_ToggleButton.Content = "开启终极丝滑驱动 (快捷键: Alt+Shift+M)";

                // 暂停时清空残留的手指状态
                wadpy_pn_logicBrain.Process(new List<TouchpadContact>());
            }
        }

        // =======================================================
        // 窗口销毁保护：拔掉底层钩子，防止系统鼠标卡死
        // =======================================================
        protected override void OnClosed(EventArgs e)
        {
            MouseHookManager.Stop();
            base.OnClosed(e);
        }
    }
}