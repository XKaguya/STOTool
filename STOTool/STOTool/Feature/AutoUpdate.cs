using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using STOTool.Core;
using STOTool.Generic;
using STOTool.Settings;

namespace STOTool.Feature
{
    public class AutoUpdate
    {
        private static readonly string Author = "Xkaguya";
        private static readonly string Project = "STOTool";
        private static readonly string ExeName = "STOTool.exe";
        private static readonly string CurrentExePath = Path.Combine(Environment.CurrentDirectory, ExeName);
        private static readonly string NewExePath = Path.Combine(Environment.CurrentDirectory, "STOTool-New.exe");
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public static void StartAutoUpdateTask()
        {
            Task.Run(async () => await AutoUpdateTask(CancellationTokenSource.Token));
        }

        private static async Task AutoUpdateTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                CheckAndUpdate();
                await Task.Delay(TimeSpan.FromHours(1), token);
            }
        }

        public static void CheckAndUpdate()
        {
            if (!GlobalVariables.AutoUpdate)
            {
                return;
            }
            
            try
            {
                string commonUpdaterPath = Path.Combine(Directory.GetCurrentDirectory(), "CommonUpdater.exe");

                if (!File.Exists(commonUpdaterPath))
                {
                    Logger.Info("There's no CommonUpdater in the folder. Failed to update.");
                    return;
                }
                
                string arguments = $"{Project} {ExeName} {Author} {GlobalStaticVariables.Version} \"{CurrentExePath}\" \"{NewExePath}\"";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = commonUpdaterPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true
                };

                Logger.Debug($"Starting CommonUpdater with arguments: {arguments}");
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Logger.Error("Failed to start CommonUpdater: Process.Start returned null.");
                    return;
                }
                
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(error))
                {
                    Logger.Error($"CommonUpdater error: {error}");
                }
                    
                if (process.ExitCode != 0)
                {
                    Logger.Error($"CommonUpdater exited with code {process.ExitCode}");
                }
                else
                {
                    Logger.Debug("CommonUpdater started successfully.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start CommonUpdater: {ex.Message}");
            }
        }
    }
}