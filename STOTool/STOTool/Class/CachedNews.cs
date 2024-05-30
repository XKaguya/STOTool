using System.Collections.Generic;

namespace STOTool.Class
{
    public class CachedNews
    {
        public List<string>? NewsUrls { get; set; }
        public Dictionary<string, byte[]>? ScreenshotData { get; set; }
    }
}