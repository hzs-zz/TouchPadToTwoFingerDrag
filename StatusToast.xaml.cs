using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TouchpadToMiddleClick
{
    public partial class StatusToast : Window
    {
        public StatusToast(string message)
        {
            InitializeComponent();
            wadpy_pn_Msg.Text = message;

            // 1. 定位：将窗口放在屏幕左侧，垂直居中
            this.Left = 0;
            this.Top = SystemParameters.WorkArea.Height - this.Height - 20;

            this.Loaded += async (s, e) => {

                
                // Amplitude 参数决定了“冲出去多远”。值越大，回弹越剧烈 (推荐 0.3 到 0.8 之间)
                BackEase springEase = new BackEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Amplitude = 0.2
                };

                // 2. 准备位移动画：从 -300 滑到 0 (耗时 500 毫秒)
                DoubleAnimation slideIn = new DoubleAnimation(-300, 0, TimeSpan.FromMilliseconds(800));
                slideIn.EasingFunction = springEase; // 绑定回弹曲线

                // 3. 准备缩放动画：从 0.5 倍 变大到 1.0 倍 (耗时 500 毫秒)
                DoubleAnimation scaleUp = new DoubleAnimation(0.5, 1.0, TimeSpan.FromMilliseconds(1000));
                scaleUp.EasingFunction = springEase; // 绑定同一条回弹曲线！

                // 4. 同步开火！三个动画引擎同时启动
                wadpy_pn_Transfer.BeginAnimation(TranslateTransform.XProperty, slideIn);
                wadpy_pn_Scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
                wadpy_pn_Scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

                // 停留 3 秒
                await Task.Delay(3000);

                // 淡出动画 (淡出通常不需要回弹，平滑消失即可)
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
                fadeOut.Completed += (ss, ee) => this.Close();
                this.BeginAnimation(OpacityProperty, fadeOut);
            };
        }

        // 静态方法方便全局调用
        public static void Show(string msg)
        {
            StatusToast toast = new StatusToast(msg);
            toast.Show();
        }
    }
}