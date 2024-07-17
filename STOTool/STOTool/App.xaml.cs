using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using STOTool.Generic;

namespace STOTool
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App
  {
      public static MainWindow? MainWindowInstance { get; private set; }

      protected override async void OnStartup(StartupEventArgs ev)
      {
          try
          {
              AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
              DispatcherUnhandledException += App_DispatcherUnhandledException;
              TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException!;
              
              base.OnStartup(ev);
              
              KillExistingInstances();
          
              MainWindowInstance = new MainWindow();
              MainWindowInstance.Hide();

              await Feature.PipeServer.StartServerAsync();
          }
          catch (Exception e)
          {
              MessageBox.Show(e.Message + e.StackTrace);
              throw;
          }
      }
      
      private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
          Exception ex = (Exception)e.ExceptionObject;
          LogException(ex);
      }

      private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
      {
          LogException(e.Exception);
          e.Handled = true;
      }
      
      private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
      {
          LogException(e.Exception);
          e.SetObserved();
      }

      private void LogException(Exception ex)
      {
          Logger.Critical("ERROR! Unhandled exception!");
          Logger.Critical($"Exception:\n{ex.Message}\n{ex.StackTrace}");
      }
      
      private void KillExistingInstances()
      {
          var currentProcess = Process.GetCurrentProcess();
          var processes = Process.GetProcessesByName(currentProcess.ProcessName);

          foreach (var process in processes)
          {
              if (process.Id != currentProcess.Id)
              {
                  try
                  {
                      process.Kill();
                      process.WaitForExit();
                  }
                  catch (Exception ex)
                  {
                      MessageBox.Show($"Failed to kill process {process.Id}: {ex.Message}");
                  }
              }
          }
      }
  }
}
