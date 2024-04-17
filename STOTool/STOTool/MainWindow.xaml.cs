using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using STOTool.Enum;
using STOTool.Generic;

namespace STOTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow
    {
        private const string Version = "1.0.1";
        public static int Interval = 5000;
        private static int _maxRetry = 3;
        
        public MainWindow()
        {
            InitializeComponent();
            
#if DEBUG
            Api.SetProgramLevel(ProgramLevel.Debug);
            Logger.SetLogLevel(LogLevel.Debug);
            LogWindow.Instance.Show();
#endif
            LogWindow.Instance.Hide();
            Task.Run(Init);

            Logger.Info($"Welcome to STOTool. This is version {Version}. If you meet any problem, please contact me at github.");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            
            Environment.Exit(0);
        }

        private static async Task Init()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(Interval);
                    await Api.UpdatePerSecond();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + "\n" + e.StackTrace);
                
                if (_maxRetry != 3)
                {
                    _maxRetry++;
                    await Init();
                }
                else
                {
                    Logger.Fatal("Failed to run Init().");
                }
            }
        }

        private void LogButtonClick(object sender, RoutedEventArgs e)
        {
            if (LogWindow.Instance.IsVisible)
            {
                LogWindow.Instance.Hide();
            }
            else
            {
                LogWindow.Instance.Show();
            }
        }

        private void SettingButtonClick(object sender, RoutedEventArgs e)
        {
            if (SetWindow.Instance.IsVisible)
            {
                SetWindow.Instance.Hide();
            }
            else
            {
                SetWindow.Instance.Show();
            }
        }
    }
}
