using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LogLevel = STOTool.Enum.LogLevel;

namespace STOTool.Generic
{
    public class Logger
    {
        private static RichTextBox _logRichTextBox;
        private static readonly string LogFilePath = "Info.log";
        private static readonly string CriticalLogPath;
        private static readonly string ErrorLogPath;
        private static readonly object LockObject = new();
        private static int _logCount = 0;
        private static int _maxLogCount = 100;
        private static LogLevel _currentLogLevel = LogLevel.Info;

        static Logger()
        {
            _logRichTextBox = new RichTextBox();
            CriticalLogPath = $"[{DateTime.Now:MM-dd-HH-mm}]CRITICAL.log";
            ErrorLogPath = $"[{DateTime.Now:MM-dd-HH-mm}]ERROR.log";
            File.WriteAllText(LogFilePath, string.Empty);
        }

        public static bool SetLogLevel(LogLevel newLogLevel)
        {
            _currentLogLevel = newLogLevel;
            return _currentLogLevel == newLogLevel;
        }

        public static bool SetMaxLogCount(int newMaxLogCount)
        {
            _maxLogCount = newMaxLogCount;
            return _maxLogCount == newMaxLogCount;
        }

        public static bool SetLogTarget(RichTextBox richTextBox)
        {
            _logRichTextBox = richTextBox;
            return _logRichTextBox == richTextBox;
        }

        public static bool SetLogBackgroundColor(SolidColorBrush color)
        {
            _logRichTextBox.Background = color;
            return _logRichTextBox.Background == color;
        }

        public static bool Info(string message, [CallerMemberName] string callerName = "")
        {
            return Log(message, LogLevel.Info, callerName);
        }

        public static bool Warning(string message, [CallerMemberName] string callerName = "")
        {
            return Log(message, LogLevel.Warning, callerName);
        }

        public static bool Error(string message, [CallerMemberName] string callerName = "")
        {
            return Log(message, LogLevel.Error, callerName);
        }

        public static bool Debug(string message, [CallerMemberName] string callerName = "")
        {
            return Log(message, LogLevel.Debug, callerName);
        }

        public static bool Trace(string message, [CallerMemberName] string callerName = "")
        {
            return Log(message, LogLevel.Trace, callerName);
        }

        public static bool Fatal(string message, [CallerMemberName] string callerName = "")
        {
            return Log(message, LogLevel.Fatal, callerName);
        }

        public static bool Critical(string message, [CallerMemberName] string callerName = "")
        {
            return Log(message, LogLevel.Critical, callerName);
        }

        private static bool Log(string message, LogLevel level, string callerName)
        {
            if (_currentLogLevel >= level)
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{callerName}]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, GetColorByLogLevel(level));
                if (level == LogLevel.Critical)
                {
                    WriteCriticalLogToFile(logMessage);
                }
                if (level == LogLevel.Fatal || level == LogLevel.Error)
                {
                    WriteErrorLogToFile(logMessage);
                }
                return true;
            }
            return false;
        }

        private static void LogAddLine(string message, SolidColorBrush color)
        {
            _logRichTextBox.Dispatcher.Invoke(() =>
            {
                if (_logCount >= _maxLogCount)
                {
                    ClearLogs();
                }
                else
                {
                    _logCount++;
                    AppendLogMessage(message, color);
                }
            });
        }
        
        private static void AppendLogMessage(string message, SolidColorBrush color)
        {
            Paragraph paragraph = new Paragraph(new Run(message))
            {
                Foreground = color
            };

            _logRichTextBox.Document.Blocks.Add(paragraph);
            
            if (LogWindow.AutoScrollToEnd)
            {
                _logRichTextBox.ScrollToEnd();
            }
        }

        private static void WriteLogToFile(string message)
        {
            lock (LockObject)
            {
                File.AppendAllText(LogFilePath, message + Environment.NewLine);
            }
        }

        private static void WriteCriticalLogToFile(string message)
        {
            lock (LockObject)
            {
                File.AppendAllText(CriticalLogPath, message + Environment.NewLine);
            }
        }

        private static void WriteErrorLogToFile(string message)
        {
            lock (LockObject)
            {
                File.AppendAllText(ErrorLogPath, message + Environment.NewLine);
            }
        }

        public static void ClearLogs()
        {
            _logRichTextBox.Dispatcher.Invoke(() => { _logRichTextBox.Document.Blocks.Clear(); });
            _logCount = 0;
        }

        private static SolidColorBrush GetColorByLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => Brushes.CornflowerBlue,
                LogLevel.Warning => Brushes.Maroon,
                LogLevel.Error => Brushes.Red,
                LogLevel.Debug => Brushes.LightSlateGray,
                LogLevel.Trace => Brushes.Gold,
                LogLevel.Fatal => Brushes.DarkRed,
                LogLevel.Critical => Brushes.Red,
                _ => Brushes.Black,
            };
        }
    }
}