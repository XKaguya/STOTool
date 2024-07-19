using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using STOTool.Generic;

namespace STOTool
{
    public partial class LogWindow
    {
        public static LogWindow Instance => LazyInstance.Value;
        private RichTextBox? LogRichTextBox { get; set; } = null;
        private static readonly Lazy<LogWindow> LazyInstance = new(() => new LogWindow());

        private LogWindow()
        {
            InitializeComponent();
            LogRichTextBox = LogTextBox;
            Logger.SetLogTarget(LogRichTextBox);

            if (LogRichTextBox == null)
            {
                Logger.Critical("Fatal Error. LogRichTextBox is null.");
            }
        }
        
        private void ScrollToBottomClick(object sender, RoutedEventArgs ev)
        {
            if (LogRichTextBox != null)
            {
                LogRichTextBox.Dispatcher.BeginInvoke(() => 
                {
                    LogRichTextBox.ScrollToEnd();
                });
            }
            else
            {
                Logger.Debug("LogRichTextBox is null.");
            }
        }
    
        private void ClearLogClick(object sender, RoutedEventArgs ev)
        {
            Logger.ClearLogs();
        }
        
        private void ReloadClick(object sender, RoutedEventArgs ev)
        {
            Api.ParseConfig();
        }

        protected override void OnClosing(CancelEventArgs ev)
        {
            // Canceled due to in the previous version there's a MainWindow.
            
            // ev.Cancel = true;
            // this.Hide();

            Environment.Exit(0);
        }
    }
}