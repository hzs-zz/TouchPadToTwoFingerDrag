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

            this.Left = 0;
            this.Top = SystemParameters.WorkArea.Height - this.Height - 20;

            this.Loaded += async (s, e) => {

                BackEase springEase = new BackEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Amplitude = 0.2
                };

                DoubleAnimation slideIn = new DoubleAnimation(-300, 0, TimeSpan.FromMilliseconds(800));
                slideIn.EasingFunction = springEase;

                DoubleAnimation scaleUp = new DoubleAnimation(0.5, 1.0, TimeSpan.FromMilliseconds(1000));
                scaleUp.EasingFunction = springEase;

                wadpy_pn_Transfer.BeginAnimation(TranslateTransform.XProperty, slideIn);
                wadpy_pn_Scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
                wadpy_pn_Scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

                await Task.Delay(3000);

                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
                fadeOut.Completed += (ss, ee) => this.Close();
                this.BeginAnimation(OpacityProperty, fadeOut);
            };
        }

        public static void Show(string msg)
        {
            // 🌟 极简流拦截：自己去读一下主程序的配置，如果关闭了直接原路返回！
            if (Application.Current is App app && !app.wadpy_pn_Config.ShowToastNotifications)
            {
                return;
            }

            StatusToast toast = new StatusToast(msg);
            toast.Show();
        }
    }
}