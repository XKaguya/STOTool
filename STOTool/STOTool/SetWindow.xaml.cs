using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using STOTool.Enum;
using STOTool.Generic;

namespace STOTool
{
    public partial class SetWindow : Window
    {
        private static readonly Lazy<SetWindow> LazyInstance = new(() => new SetWindow());
        
        public static SetWindow Instance => LazyInstance.Value;
        
        private SetWindow()
        {
            InitializeComponent();
            
            Debug.Checked += DebugChecked;
            Debug.Unchecked += DebugUnchecked;
        }
        
        private void DebugChecked(object sender, RoutedEventArgs ev)
        {
            Api.SetProgramLevel(ProgramLevel.Debug);
            Logger.SetLogLevel(LogLevel.Debug);
        }
    
        private void DebugUnchecked(object sender, RoutedEventArgs ev)
        {
            Api.SetProgramLevel(ProgramLevel.Normal);
            Logger.SetLogLevel(LogLevel.Error);
        }
        
        protected override void OnClosing(CancelEventArgs ev)
        {
            ev.Cancel = true;
            this.Hide();
        }

        private void RefreshIntervalOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RefreshInterval.Text != "" && int.TryParse(RefreshInterval.Text, out int interval))
            {
                MainWindow.Interval = interval;
            }
        }
    }
}