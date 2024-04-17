using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class Screenshot
    {
        [STAThread]
        public static async Task<string> CaptureAndSaveAsync()
        {
            Logger.Debug("Capturing screenshot...");
            
            RenderTargetBitmap? bitmap = await CaptureWindowAsync(App.MainWindowInstance);
            
            MemoryStream stream = await SaveBitmapToMemoryStreamAsync(bitmap);
            
            Logger.Debug($"{stream.Length} bytes of screenshot saved to memory stream.");

            // await SaveBitmapToFileAsync(bitmap, "img.png");
        
            stream.Seek(0, SeekOrigin.Begin);
        
            string base64String = Convert.ToBase64String(stream.ToArray());
        
            bitmap = null;
            await stream.DisposeAsync();

            return base64String;
        }
        
        private static async Task SaveBitmapToFileAsync(RenderTargetBitmap bitmap, string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }

        private static Task<RenderTargetBitmap> CaptureWindowAsync(Window window)
        {
            return Task.Run(() =>
            {
                RenderTargetBitmap bitmap = null;

                window.Dispatcher.Invoke(() =>
                {
                    bitmap = new RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            
                    bitmap.Render(window);
                });

                return bitmap;
            });
        }

        private static async Task<MemoryStream> SaveBitmapToMemoryStreamAsync(RenderTargetBitmap bitmap)
        {
            MemoryStream newStream = new MemoryStream();
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    using (MemoryStream originalStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        encoder.Save(originalStream);

                        originalStream.Position = 0;

                        await originalStream.CopyToAsync(newStream);
                        newStream.Position = 0;
                    }
                });

                return newStream;
            }
        }
    }
}