using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using STOTool.Class;
using STOTool.Enum;
using STOTool.Generic;
using System.IO;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using EventInfo = STOTool.Class.EventInfo;
using Point = SixLabors.ImageSharp.Point;

namespace STOTool.Feature
{
    public class DrawNewsImage
    {
        private static async Task<Image<Rgba32>> LoadImageAsync(byte[] imageBytes)
        {
            try
            {
                using var stream = new MemoryStream(imageBytes);

                var image = await Task.Run(() => Image.Load<Rgba32>(stream));
                
                return image;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception occurred while loading image: {ex.Message}");

                return null;
            }
        }

        private static async Task<string> DrawOnBackgroundAsync(Image<Rgba32> backgroundImage, List<Image<Rgba32>> newsImages, List<string> newsTitles, MaintenanceInfo maintenanceInfo, List<EventInfo> eventInfos, int startX, int startY)
        {

            int x = startX;
            int y = startY;

            int count = 0;

            string maintenanceMessage = Api.MaintenanceInfoToString(maintenanceInfo);

#if DEBUG
            maintenanceMessage = "TEST TEST TEST TEST TEST";
            
            EventInfo testInfo = new EventInfo()
            {
                EndDate = "testtesttest",
                StartDate = "testtesttest",
                Summary = "testtesttest",
            };

            List<EventInfo> eventInfoTest = new List<EventInfo>();
            eventInfoTest.Add(testInfo);
            eventInfoTest.Add(testInfo);
            eventInfoTest.Add(testInfo);
#endif
            
            // Draw events messages.
            int xE = x;
            int yE = y;
            
            foreach (var eventInfo in eventInfos)
            {
                backgroundImage.Mutate(ctx => ctx.DrawText(eventInfo.Summary!, MainWindow.StFontFamily.CreateFont(50), Color.Gray, new PointF(xE - 50, yE - 400)));
                backgroundImage.Mutate(ctx => ctx.DrawText(eventInfo.StartDate!, MainWindow.StFontFamily.CreateFont(50), Color.White, new PointF(xE - 50, yE - 350)));
                backgroundImage.Mutate(ctx => ctx.DrawText(eventInfo.EndDate!, MainWindow.StFontFamily.CreateFont(50), Color.White, new PointF(xE - 50, yE - 300)));

                xE += 250;
            }

            // Draw maintenance messages.
            backgroundImage.Mutate(ctx => ctx.DrawText(maintenanceMessage, MainWindow.StFontFamily.CreateFont(60), Color.White, new PointF(x + 20, y + 870)));

            foreach (var newsImage in newsImages)
            {
                // Draw news images and titles.
                backgroundImage.Mutate(ctx => ctx.DrawImage(newsImage, new Point(x, y), 1f));
                backgroundImage.Mutate(ctx => ctx.DrawText(newsTitles[count], MainWindow.StFontFamily.CreateFont(50), Color.White, new PointF(x, y + 230)));

                x += 432;

                if (x + 432 > backgroundImage.Width)
                {
                    x = startX;
                    y += 293;
                }

                count++;

                if (count == 9)
                {
                    break;
                }
            }
            
            // await backgroundImage.SaveAsync("test.png");

            string result = backgroundImage.ToBase64String(PngFormat.Instance);

            return result;
        }

        public static async Task<string>? DrawImageAsync()
        {
            CachedInfo cachedInfo = await Cache.GetCachedInfoAsync();
            MaintenanceInfo maintenanceInfo = await Cache.GetFastCachedMaintenanceInfoAsync();

            if (Helper.NullCheck(cachedInfo))
            {
                Logger.Error($"Something is null.");
                return null;
            }
            
            int startX = 311;
            int startY = 447;

            List<Image<Rgba32>> newsImages = new List<Image<Rgba32>>();
            List<string> newsTitles = new List<string>();

            var downloadTasks = new List<Task<byte[]>>();

            foreach (var newsInfo in cachedInfo.NewsInfos!)
            {
                downloadTasks.Add(Helper.DownloadImageAsync(newsInfo.ImageUrl!)!);
                newsTitles.Add(newsInfo.Title!);
            }
            
            var downloadedImages = await Task.WhenAll(downloadTasks);
            
            foreach (var imageBytes in downloadedImages)
            {
                if (imageBytes != null!)
                {
                    var image = await LoadImageAsync(imageBytes);
                    image.Mutate(x => x.Resize(407, 232));
                    newsImages.Add(image);
                }
            }

            Image<Rgba32>? backgroundImage;

            switch (maintenanceInfo.ShardStatus)
            {
                case MaintenanceTimeType.Maintenance:
                case MaintenanceTimeType.WaitingForMaintenance:
                case MaintenanceTimeType.None:
                case MaintenanceTimeType.MaintenanceEnded:
                    backgroundImage = MainWindow.BackgroundImageUp;
                    break;

                default:
                    backgroundImage = MainWindow.BackgroundImageDown;
                    break;
            }

            string? result = null;

            if (backgroundImage != null)
            {
                result = await DrawOnBackgroundAsync(backgroundImage, newsImages, newsTitles, maintenanceInfo, cachedInfo.EventInfos!, startX, startY);
            }
            else
            {
                Logger.Error("Background image is null.");
            }
            
            return result;
        }
    }
}