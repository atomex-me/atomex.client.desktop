using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Atomex.Client.Desktop.Services
{
    public class ImageService
    {
        public IDictionary<string, IBitmap> Images;


        public ImageService()
        {
            Images = new Dictionary<string, IBitmap>();

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var defaultBitmap =
                new Bitmap(assets.Open(new Uri("avares://Atomex.Client.Desktop/Resources/Images/logo_white.png")));
            Images.Add("default", defaultBitmap);
        }

        public async void LoadImage(string url)
        {
            using WebClient wc = new();
            await using Stream s = wc.OpenRead(url);
            using System.Drawing.Bitmap bmp = new(s);
            await using MemoryStream memory = new();
            bmp.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            Images[url] = new Bitmap(memory);
        }


        public IBitmap GetImage(string url)
        {
            return Images.TryGetValue(url, out var image)
                ? image
                : Images["default"];
        }
    }
}