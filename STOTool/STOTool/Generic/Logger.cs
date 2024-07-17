﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using STOTool.Enum;

namespace STOTool.Generic
{
    public class Logger
    {
        private static RichTextBox _logRichTextBox;
        private static readonly string LogFilePath = "Info.log";
        private static readonly string CriticalLogPath;
        private static readonly object LockObject = new();
        private static int _logCount = 0;
        private static int _maxLogCount = 100;
        private static LogLevel _currentLogLevel = LogLevel.Info;

        static Logger()
        {
            _logRichTextBox = new RichTextBox();
            CriticalLogPath = $"[{DateTime.Now.Month} + {DateTime.Now.Day} + {DateTime.Now.Hour} + {DateTime.Now.Second}]Critical.log";
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

        public static bool Info(string message)
        {
            if (_currentLogLevel >= LogLevel.Info)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{GetCallerName()}] [INFO]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, Brushes.CornflowerBlue);
                return true;
            }
            
            return false;
        }

        public static bool Warning(string message)
        {
            if (_currentLogLevel >= LogLevel.Warning)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{GetCallerName()}] [WARNING]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, Brushes.Maroon);
                return true;
            }
            
            return false;
        }

        public static bool Error(string message)
        {
            if (_currentLogLevel >= LogLevel.Error)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Exception Source: {GetCallerName()}] [ERROR]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, Brushes.Red);
                return true;
            }
            
            return false;
        }

        [STAThread]
        public static bool Debug(string message)
        {
            if (_currentLogLevel >= LogLevel.Debug)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{GetCallerName()}] [DEBUG]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, Brushes.LightSlateGray);
                return true;
            }
            
            return false;
        }
        
        [STAThread]
        public static bool Trace(string message)
        {
            if (_currentLogLevel >= LogLevel.Trace)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{GetCallerName()}] [TRACE]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, Brushes.Gold);
                return true;
            }
            
            return false;
        }
        
        [STAThread]
        public static bool Fatal(string message)
        {
            if (_currentLogLevel >= LogLevel.Fatal)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Exception Source: {GetCallerName()}] [FATAL]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, Brushes.Red);
                return true;
            }
            
            return false;
        }
        
        [STAThread]
        public static bool Critical(string message)
        {
            if (_currentLogLevel >= LogLevel.Critical)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Exception Source: {GetCallerName()}] [CRITICAL]: {message}";
                WriteLogToFile(logMessage);
                LogAddLine(logMessage, Brushes.Red);
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
                    
                    Paragraph paragraph = new Paragraph(new Run(message));
                    paragraph.Foreground = color;
                    _logRichTextBox.Document.Blocks.Add(paragraph);
                    _logRichTextBox.ScrollToEnd();
                }
            });
        }

        private static void WriteLogToFile(string message)
        {
            try
            {
                lock (LockObject)
                {
                    using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                    {
                        writer.WriteLine(message);
                    }
                }
            }
            catch (Exception e)
            {
                Error(e.Message + e.StackTrace);
                throw;
            }
        }
        
        private static void CriticalLog(string message)
        {
            try
            {
                lock (LockObject)
                {
                    using (StreamWriter writer = new StreamWriter(CriticalLogPath, true))
                    {
                        writer.WriteLine(message);
                    }
                }
            }
            catch (Exception e)
            {
                Error(e.Message + e.StackTrace);
                throw;
            }
        }

        public static void ClearLogs()
        {
            _logRichTextBox.Dispatcher.Invoke(() => { _logRichTextBox.Document.Blocks.Clear(); });
            _logCount = 0;
        }

        private static string GetCallerName()
        {
            MethodBase currentMethod = MethodBase.GetCurrentMethod();

            int frameCount = 0;

            while (true)
            {
                frameCount++;
                
                StackFrame callerFrame = new StackFrame(frameCount);
                MethodBase callerMethod = callerFrame.GetMethod();

                if (callerMethod == null)
                {
                    return "Unknown Caller";
                }
                else if (callerMethod.DeclaringType.Name == currentMethod.DeclaringType.Name || callerMethod.DeclaringType.Namespace != "STOTool")
                {
                    continue;
                }
                else
                {
                    string cleanMethodName = Regex.Replace(callerMethod.DeclaringType.Name, @"^<(\w+)>.*$", "$1");
                    
                    return cleanMethodName;
                }
            }
        }
    }
}
