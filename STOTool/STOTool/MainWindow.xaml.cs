using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using STOTool.Enum;
using STOTool.Generic;

namespace STOTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string Version = "1.1.0";
        public static int Interval = 5000;
        private static int _maxRetry = 3;
        
        public static FontFamily StFontFamily { get; private set; }
        
        private static readonly string backgroundImageUri_Down = "/STOTool;component/Background/Bg_Down.png";
        private static readonly string backgroundImageUri_Up = "/STOTool;component/Background/Bg_Up.png";

        public static Image<Rgba32>? BackgroundImageDown { get; private set; } = null;
        public static Image<Rgba32>? BackgroundImageUp { get; private set; } = null;
        
        public MainWindow()
        {
            InitializeComponent();
            LogWindow.Instance.Hide();
            PreInit();
            
#if DEBUG
            Logger.Debug("You're in DEBUG mode.");
            Api.SetProgramLevel(ProgramLevel.Debug);
            Logger.SetLogLevel(LogLevel.Debug);
            
            /*TestMethods();*/
            
            Logger.Debug("Initialization method has been disabled. This is test only.");
#else
            Task.Run(Init);

            Logger.Info($"Welcome to STOTool. This is version {Version}. If you meet any problem, please contact me at github.");
#endif
        }

        private static async Task TestMethods()
        {
            try
            {

            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception occurred: {ex.Message}");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            
            Environment.Exit(0);
        }

        private static async Task Init()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(Interval);
                    await Api.UpdatePerSecond();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + "\n" + e.StackTrace);
                
                if (_maxRetry != 3)
                {
                    _maxRetry++;
                    await Init();
                }
                else
                {
                    Logger.Fatal("Failed to run Init().");
                }
            }
        }

        private void LogButtonClick(object sender, RoutedEventArgs e)
        {
            if (LogWindow.Instance.IsVisible)
            {
                LogWindow.Instance.Hide();
            }
            else
            {
                LogWindow.Instance.Show();
            }
        }

        private void SettingButtonClick(object sender, RoutedEventArgs e)
        {
            if (SetWindow.Instance.IsVisible)
            {
                SetWindow.Instance.Hide();
            }
            else
            {
                SetWindow.Instance.Show();
            }
        }

        private static async void PreInit()
        {
            try
            {
                var fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("STOTool.Font.StarTrek_Embedded.ttf");
                var fontCollection = new FontCollection();
                if (fontStream != null)
                {
                    // How can this be null ?
                    StFontFamily = fontCollection.Add(fontStream);
                }
                
                var resourceInfoDown = Application.GetResourceStream(new Uri(backgroundImageUri_Down, UriKind.Relative));
                var resourceInfoUp = Application.GetResourceStream(new Uri(backgroundImageUri_Up, UriKind.Relative));

                if (resourceInfoDown == null || resourceInfoUp == null)
                {
                    Logger.Error("Resource stream not found.");
                    return;
                }

                var backgroundImageDownTask = Helper.LoadImageAsync(resourceInfoDown.Stream);
                var backgroundImageUpTask = Helper.LoadImageAsync(resourceInfoUp.Stream);

                var backgroundImages = await Task.WhenAll(backgroundImageDownTask, backgroundImageUpTask);

                BackgroundImageDown = backgroundImages[0];
                BackgroundImageUp = backgroundImages[1];
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error loading background images: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
