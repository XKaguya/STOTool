using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using STOTool.Generic;

namespace STOTool
{
    public partial class LogWindow : Window
    {
        public static LogWindow Instance => LazyInstance.Value;

        private RichTextBox? LogRichTextBox { get; set; } = null;
        
        private static readonly Lazy<LogWindow> LazyInstance = new(() => new LogWindow());

        private LogWindow()
        {
            InitializeComponent();
            LogRichTextBox = LogBox;
            Logger.SetLogTarget(LogRichTextBox);
        }

        protected override void OnClosing(CancelEventArgs ev)
        {
            ev.Cancel = true;
            this.Hide();
        }
    }
}