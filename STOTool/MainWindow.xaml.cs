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
        public static int Interval = 5000;
        
        public MainWindow()
        {
            InitializeComponent();
            
            LogWindow.Instance.Show();

            Task.Run(Init);
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
                await Init();
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
