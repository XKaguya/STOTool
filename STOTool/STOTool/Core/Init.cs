using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.Fonts;
using STOTool.Enum;
using STOTool.Feature;
using STOTool.Generic;
using STOTool.Settings;

namespace STOTool.Core
{
    public class Init
    {
        private static CancellationTokenSource _webCancellactionTokenSource = new();
        
        private static void InitDictionary()
        {
            GlobalStaticVariables.BackgroundImageUriDictionary.Clear();
            GlobalStaticVariables.BackgroundImageUriDictionary.Add("Up", "/STOTool;component/Background/Bg_Up.png");
            GlobalStaticVariables.BackgroundImageUriDictionary.Add("Down", "/STOTool;component/Background/Bg_Down.png");

            if (GlobalStaticVariables.BackgroundImageUriDictionary.Count != 2 || GlobalStaticVariables.BackgroundImageUriDictionary["Up"] == null || GlobalStaticVariables.BackgroundImageUriDictionary["Down"] == null)
            {
                Logger.Fatal("BackgroundImageUriDictionary failed to init.");
                Environment.Exit(-1);
            }
        }
        
        public static async void Initialize()
        {
            try
            {
                GlobalStaticVariables.InitializedDateTime = DateTime.Now;
                
                Logger.ClearLogs();
                
                InitDictionary();

                if (System.Enum.Parse<ProgramLevel>(GlobalVariables.ProgramLevel) == ProgramLevel.Debug)
                {
                    Logger.SetLogLevel(LogLevel.Debug);
                }

                if (System.Enum.Parse<ProgramLevel>(GlobalVariables.ProgramLevel) == ProgramLevel.Normal)
                {
                    var fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("STOTool.Font.StarTrek.ttf");
                    var fontCollection = new FontCollection();
                    if (fontStream != null)
                    {
                        // How can this be null ?
                        GlobalStaticVariables.StFontFamily = fontCollection.Add(fontStream);
                    }
                    
                    var resourceInfoDown = Application.GetResourceStream(new Uri(GlobalStaticVariables.BackgroundImageUriDictionary["Down"]!, UriKind.Relative));
                    var resourceInfoUp = Application.GetResourceStream(new Uri(GlobalStaticVariables.BackgroundImageUriDictionary["Up"]!, UriKind.Relative));

                    if (resourceInfoDown == null || resourceInfoUp == null)
                    {
                        Logger.Error("Resource stream not found.");
                        return;
                    }

                    var backgroundImageDownTask = Helper.LoadImageAsync(resourceInfoDown.Stream);
                    var backgroundImageUpTask = Helper.LoadImageAsync(resourceInfoUp.Stream);

                    var backgroundImages = await Task.WhenAll(backgroundImageDownTask, backgroundImageUpTask);

                    GlobalStaticVariables.BackgroundImageDictionary["Down"] = backgroundImages[0];
                    GlobalStaticVariables.BackgroundImageDictionary["Up"] = backgroundImages[1];
                    
                    DrawNewsImage.InitFonts();
                    
                    await Helper.InitBrowserAsync();
                    
                    AutoUpdate.CheckAndUpdate();
                    AutoUpdate.StartAutoUpdateTask();
                    
                    var cacheNewsTask = Cache.GetCachedNewsAsync();
                    var cacheInfoTask = Cache.GetCachedInfoAsync();
                    var cacheMaintenanceTask = Cache.GetFastCachedMaintenanceInfoAsync();
                    await Task.WhenAll(cacheNewsTask, cacheInfoTask, cacheMaintenanceTask);
                    
                    Cache.StartCacheGuard();

                    _ = Task.Run(AutoNewsLoop);
                    _ = Task.Run(DrawImageLoop);
                }

                await InitWebServer();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error on initializing. {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(-1);
            }
        }
        
        private static async Task InitWebServer()
        {
            await Task.Run(async () =>
            {
                try
                {
                    var host = new WebHostBuilder()
                        .UseKestrel(options => { options.Listen(IPAddress.Any, GlobalVariables.UserInterfaceWebSocketPort); })
                        .ConfigureServices(services =>
                        {
                            var webService = new WebUserInterface.Startup();
                            webService.ConfigureServices(services);
                        })
                        .Configure(app =>
                        {
                            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                            var webService = new WebUserInterface.Startup();
                            webService.Configure(app, env);
                        })
                        .Build();

                    await host.RunAsync(_webCancellactionTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Logger.Info($"Task WebService stopped.");
                }
            });
        }
        
        private static async Task AutoNewsLoop()
        {
            while (true)
            {
                await Task.Run(() => AutoNews.HasHashChanged());
                await Task.Delay(TimeSpan.FromSeconds(20));
            }
        }

        private static async Task DrawImageLoop()
        {
            while (true)
            {
                await Task.Run(() => DrawNewsImage.DrawImageAsync());
                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }
    }
}