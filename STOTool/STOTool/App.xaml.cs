using System;
using System.Diagnostics;
using System.Windows;

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
              base.OnStartup(ev);
              
              KillExistingInstances();
          
              MainWindowInstance = new MainWindow();
              MainWindowInstance.Show();

              await Feature.PipeServer.StartServerAsync();
          }
          catch (Exception e)
          {
              Console.WriteLine(e);
              throw;
          }
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
                      Console.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
                  }
              }
          }
      }
  }
}
