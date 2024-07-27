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
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using EventInfo = STOTool.Class.EventInfo;
using Point = SixLabors.ImageSharp.Point;

namespace STOTool.Feature
{
    public class DrawNewsImage
    {
        public static string Tips { get; set; } = "";
        
        private static Dictionary<int, Font> Fonts { get; set; } = new();
        
        private const int EventInfoOffsetX = 280;
        private const int EventInfoStartYOffset = -400;
        private const int EventInfoStartDateYOffset = -350;
        private const int EventInfoEndDateYOffset = -300;
        private const int MaintenanceMessageOffsetX = 20;
        private const int MaintenanceMessageOffsetY = 870;
        private const int NewsImageWidth = 432;
        private const int NewsImageHeightOffset = 230;
        private const int NewsImageRowHeight = 293;
        private const int MaxNewsCount = 9;
        private const int TipsStartX = 321;
        private const int TipsStartY = 367;
        private const int TipsEndX = 1573;
        private const int TipsEndY = 439;

        public static void InitFonts()
        {
            Fonts.Add(50, MainWindow.StFontFamily.CreateFont(50));
            Fonts.Add(60, MainWindow.StFontFamily.CreateFont(60));
        }
        
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
        
        private static void DrawTips(Image<Rgba32> backgroundImage, string tips)
        {
            var textSize = TextMeasurer.MeasureSize(tips, new TextOptions(Fonts[50]));
            
            var rect = new RectangleF(TipsStartX, TipsStartY, TipsEndX - TipsStartX, TipsEndY - TipsStartY);
            
            var textPosition = new PointF(rect.X + (rect.Width - textSize.Width) / 2, rect.Y + (rect.Height - textSize.Height) / 2);
            
            backgroundImage.Mutate(ctx => ctx.DrawText(new DrawingOptions(), tips, Fonts[50], Color.White, textPosition));

            Tips = "";
        }

        private static void DrawEventInfos(Image<Rgba32> backgroundImage, List<EventInfo> eventInfos, int startX, int startY)
        {
            int xE = startX;
            int yE = startY;

            foreach (var eventInfo in eventInfos)
            {
                backgroundImage.Mutate(ctx => ctx.DrawText(Helper.StringTrim(eventInfo.Summary!, 18), Fonts[50], Color.Gray, new PointF(xE - 50, yE + EventInfoStartYOffset)));
                backgroundImage.Mutate(ctx => ctx.DrawText(Helper.StringTrim(eventInfo.StartDate!, 15), Fonts[50], Color.White, new PointF(xE - 50, yE + EventInfoStartDateYOffset)));
                backgroundImage.Mutate(ctx => ctx.DrawText(Helper.StringTrim(eventInfo.EndDate!, 15), Fonts[50], Color.White, new PointF(xE - 50, yE + EventInfoEndDateYOffset)));

                xE += EventInfoOffsetX;
            }
        }

        private static void DrawMaintenanceInfo(Image<Rgba32> backgroundImage, MaintenanceInfo maintenanceInfo, int startX, int startY)
        {
            string maintenanceMessage = Api.MaintenanceInfoToString(maintenanceInfo);
            backgroundImage.Mutate(ctx => ctx.DrawText(maintenanceMessage, Fonts[60], Color.White, new PointF(startX + MaintenanceMessageOffsetX, startY + MaintenanceMessageOffsetY)));
        }

        private static void DrawNewsImagesAndTitles(Image<Rgba32> backgroundImage, List<Image<Rgba32>> newsImages, List<string> newsTitles, int startX, int startY)
        {
            int x = startX;
            int y = startY;
            int count = 0;

            foreach (var newsImage in newsImages)
            {
                backgroundImage.Mutate(ctx => ctx.DrawImage(newsImage, new Point(x, y), 1f));
                backgroundImage.Mutate(ctx => ctx.DrawText(Helper.StringTrim(newsTitles[count], 27), Fonts[50], Color.White, new PointF(x, y + NewsImageHeightOffset)));

                x += NewsImageWidth;

                if (x + NewsImageWidth > backgroundImage.Width)
                {
                    x = startX;
                    y += NewsImageRowHeight;
                }

                count++;

                if (count == MaxNewsCount)
                {
                    break;
                }
            }
        }

        private static string DrawOnBackground(Image<Rgba32> backgroundImage, List<Image<Rgba32>> newsImages, List<string> newsTitles, MaintenanceInfo maintenanceInfo, List<EventInfo> eventInfos, int startX, int startY)
        {
            eventInfos = eventInfos.Skip(eventInfos.Count - 3).Take(3).ToList();

            if (string.IsNullOrEmpty(Tips))
            {
                string formattedTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                if (Cache.CacheSetTimes.TryGetValue(Cache.CacheKey, out DateTime setTime))
                {
                    formattedTime += $" Cache last refresh at {setTime.ToString("HH:mm:ss")}";
                }
                
                Tips = formattedTime;
            }
            
            DrawTips(backgroundImage, Tips);
            
            DrawEventInfos(backgroundImage, eventInfos, startX, startY);
            
            DrawMaintenanceInfo(backgroundImage, maintenanceInfo, startX, startY);
            
            DrawNewsImagesAndTitles(backgroundImage, newsImages, newsTitles, startX, startY);
            
            string result = backgroundImage.ToBase64String(PngFormat.Instance);

            return result;
        }

        public static async Task<string>? DrawImageAsync()
        {
            CachedInfo? cachedInfo = await Cache.GetCachedInfoAsync();
            MaintenanceInfo? maintenanceInfo = await Cache.GetFastCachedMaintenanceInfoAsync();

            if (Helper.NullCheck(cachedInfo) && Helper.NullCheck(maintenanceInfo))
            {
                Logger.Error($"Something is null.");
                return "null";
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
                case MaintenanceTimeType.WaitingForMaintenance:
                case MaintenanceTimeType.None:
                case MaintenanceTimeType.Null:
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
                backgroundImage = backgroundImage.Clone();
                result = DrawOnBackground(backgroundImage, newsImages, newsTitles, maintenanceInfo, cachedInfo.EventInfos!, startX, startY);
            }
            else
            {
                Logger.Error("Background image is null.");
            }

            if (result == null)
            {
                return "null";
            }
            
            return result;
        }
    }
}