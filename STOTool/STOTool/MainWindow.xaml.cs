using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using STOTool.Feature;
using STOTool.Generic;

namespace STOTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string Version = "1.1.8";
        
        public static FontFamily StFontFamily { get; private set; }
        
        private static readonly string BackgroundImageUriDown = "/STOTool;component/Background/Bg_Down.png";
        private static readonly string BackgroundImageUriUp = "/STOTool;component/Background/Bg_Up.png";

        public static Image<Rgba32>? BackgroundImageDown { get; private set; } = null;
        public static Image<Rgba32>? BackgroundImageUp { get; private set; } = null;
        
        public MainWindow()
        {
            InitializeComponent();
            Hide();
            Logger.ClearLogs();
            PreInit();
            
            LogWindow.Instance.Show();
            Task.Run(PostInit);

            Logger.Info($"Thanks for using STOTool. Current Version: {Version}. If you meet any problem, please contact me at github.");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            
            Environment.Exit(0);
        }

        private static async Task PostInit()
        {
            try
            {
                Logger.Info($"Proceeding PostInit phase.");
                
                var cacheNewsTask = Cache.GetCachedNewsAsync();
                var cacheInfoTask = Cache.GetCachedInfoAsync();
                var cacheMaintenanceTask = Cache.GetFastCachedMaintenanceInfoAsync();

                await Task.WhenAll(cacheNewsTask, cacheInfoTask, cacheMaintenanceTask);
                
                Logger.Info($"PostInit has completed.");
                
                Cache.StartCacheGuard();

                while (true)
                {
                    await Loop1();

                    await Loop2();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        private static async Task Loop1()
        {
            await Task.Run(() => AutoNews.HasHashChanged());
                    
            await Task.Delay(TimeSpan.FromSeconds(20));
        }
        
        private static async Task Loop2()
        {
            await Task.Run(() => DrawNewsImage.DrawImageAsync());
                    
            await Task.Delay(TimeSpan.FromMinutes(10));
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
                
                var resourceInfoDown = Application.GetResourceStream(new Uri(BackgroundImageUriDown, UriKind.Relative));
                var resourceInfoUp = Application.GetResourceStream(new Uri(BackgroundImageUriUp, UriKind.Relative));

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
                
                DrawNewsImage.InitFonts();
                
                await Helper.InitBrowserAsync();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error loading background images: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}