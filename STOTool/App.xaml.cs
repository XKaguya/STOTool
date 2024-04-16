using System;
using System.Windows;

namespace STOTool
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App
  {
      public static MainWindow? MainWindowInstance { get; private set; } = null;
      
      protected override async void OnStartup(StartupEventArgs ev)
      {
          try
          {
              base.OnStartup(ev);
          
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
  }
}
