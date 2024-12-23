﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using STOTool.Feature;
using STOTool.Generic;

namespace STOTool
{
    public partial class LogWindow
    {
        public static LogWindow Instance => LazyInstance.Value;
        private RichTextBox? LogRichTextBox { get; set; } = null;
        private static readonly Lazy<LogWindow> LazyInstance = new(() => new LogWindow());
        
        public static bool AutoScrollToEnd { get; set; }

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
                if (!AutoScrollToEnd)
                {
                    AutoScrollToEnd = true;
                    Logger.Debug("AutoScrollToEnd active.");
                }
                else
                {
                    AutoScrollToEnd = false;
                    Logger.Debug("AutoScrollToEnd deactive.");
                }
            }
            else
            {
                Logger.Fatal("LogRichTextBox is null.");
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
            
            WebSocketServer.Stop();
            Logger.Info("WebSocket server has stopped.");
            
            Cache.MemoryCache.Dispose();
            Cache.CancelCacheGuard();
            Logger.Info("Cache has been disposed.");
            
            Environment.Exit(0);
        }
    }
}