using System;
using System.Collections.Generic;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace STOTool.Core
{
    public class GlobalStaticVariables
    {
        public static readonly string Version = "1.2.9";
        public static FontFamily StFontFamily { get; set; }
        public static Dictionary<string, Image<Rgba32>?> BackgroundImageDictionary { get; set; } = new ();

        public static Dictionary<string, string?> BackgroundImageUriDictionary { get; set; } = new();
        
        public static DateTime InitializedDateTime { get; set; } = new();
    }
}