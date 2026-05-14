using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using TouchpadToMiddleClick;
using Timer = System.Timers.Timer;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

namespace TouchpadToMiddleClick
{
    public partial class App : Application
    {
        private TwoFingerLogic wadpy_pn_logicBrain = new TwoFingerLogic();
        private SettingsWindow? wadpy_pn_SettingsWin;
        private ForegroundTracker? wadpy_pn_Radar;

        // 🌟 核心修改：设为 public，让 StatusToast 可以直接读到它！
        public ConfigContainer wadpy_pn_Config = new ConfigContainer();
        private string wadpy_pn_ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TouchpadConfig.xml");

        public void SaveConfig()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigContainer));
                using (StreamWriter writer = new StreamWriter(wadpy_pn_ConfigPath))
                {
                    serializer.Serialize(writer, wadpy_pn_Config);
                }
            }
            catch { }
        }

        private void LoadConfig()
        {
            if (File.Exists(wadpy_pn_ConfigPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ConfigContainer));
                    using (StreamReader reader = new StreamReader(wadpy_pn_ConfigPath))
                    {
                        var loaded = (ConfigContainer?)serializer.Deserialize(reader);
                        if (loaded != null) wadpy_pn_Config = loaded;
                    }
                }
                catch { /* 兼容旧版本，失败则重置 */ }
            }

            if (wadpy_pn_Config.PanProcesses.Count == 0 && wadpy_pn_Config.ScrollProcesses.Count == 0)
            {
                wadpy_pn_Config.PanProcesses.Add(new ProcessConfig { ProcessName = "SLDWORKS", TargetClasses = new ObservableCollection<string> { "AfxWnd140su" } });
                wadpy_pn_Config.ScrollProcesses.Add(new ProcessConfig { ProcessName = "LegacyApp", TargetClasses = new ObservableCollection<string> { "OldCanvas" } });
                SaveConfig();
            }
        }

        public void UpdateHookMasters()
        {
            MouseHookManager.IsPanMasterOn = wadpy_pn_Config.IsPanEnabled;
            MouseHookManager.IsScrollMasterOn = wadpy_pn_Config.IsScrollEnabled;
            SaveConfig();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoadConfig();
            UpdateHookMasters();

            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ShowSettingsWindow();

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                if (TouchpadHelper.Exists())
                {
                    TouchpadHelper.RegisterInput();
                    MouseHookManager.Start();

                    wadpy_pn_Radar = new ForegroundTracker(wadpy_pn_Config);
                    wadpy_pn_Radar.OnFocusChanged = Wadpy_pn_HandleFocusChanged;
                    wadpy_pn_Radar.Start();

                    MouseHookManager.OnTargetLockChanged = (mode) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (mode == InteractionMode.Panning) StatusToast.Show("🎯 锁定视口：中键平移");
                            else if (mode == InteractionMode.Scrolling) StatusToast.Show("📜 锁定视口：模拟滚动");
                            else StatusToast.Show("🖱️ 离开视口：恢复原生鼠标");
                        });
                    };

                    TouchpadHelper.OnContactsReceived = (contacts) =>
                    {
                        MouseHookManager.UpdateTouchpadState(contacts.Count);
                        wadpy_pn_logicBrain.Process(contacts, MouseHookManager.ActiveMode);
                    };

                    TouchpadHelper.OnToggleHotkey = () => {
                        bool newState = !(wadpy_pn_Config.IsPanEnabled || wadpy_pn_Config.IsScrollEnabled);
                        wadpy_pn_Config.IsPanEnabled = newState;
                        wadpy_pn_Config.IsScrollEnabled = newState;
                        UpdateHookMasters();

                        if (!newState) wadpy_pn_logicBrain.Process(new List<TouchpadContact>(), InteractionMode.None);

                        Dispatcher.Invoke(() => {
                            wadpy_pn_SettingsWin?.UpdateSwitches();
                            StatusToast.Show(newState ? "✅ 驱动已全面开启" : "🛑 驱动已全面挂起");
                        });
                    };

                    TouchpadHelper.OnSettingsHotkey = () => Dispatcher.Invoke(() => ShowSettingsWindow());
                }
            }));
        }

        private void ShowSettingsWindow()
        {
            if (wadpy_pn_SettingsWin == null)
            {
                wadpy_pn_SettingsWin = new SettingsWindow(wadpy_pn_Config);
                if (Application.Current.MainWindow == null) Application.Current.MainWindow = wadpy_pn_SettingsWin;
                wadpy_pn_SettingsWin.Closed += (s, args) => {
                    if (Application.Current.MainWindow == wadpy_pn_SettingsWin) Application.Current.MainWindow = null;
                    wadpy_pn_SettingsWin = null;
                };
                wadpy_pn_SettingsWin.Show();
            }
            else
            {
                if (wadpy_pn_SettingsWin.WindowState == WindowState.Minimized) wadpy_pn_SettingsWin.WindowState = WindowState.Normal;
                wadpy_pn_SettingsWin.Activate();
            }
        }

        private void Wadpy_pn_HandleFocusChanged(string processName, ProcessConfig? panConfig, ProcessConfig? scrollConfig)
        {
            Dispatcher.Invoke(() =>
            {
                MouseHookManager.PanTargetClasses = panConfig?.TargetClasses;
                MouseHookManager.ScrollTargetClasses = scrollConfig?.TargetClasses;

                if (panConfig == null && scrollConfig == null)
                {
                    wadpy_pn_logicBrain.Process(new List<TouchpadContact>(), InteractionMode.None);
                }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SaveConfig();
            wadpy_pn_Radar?.Stop();
            MouseHookManager.Stop();
            base.OnExit(e);
        }
    }
}