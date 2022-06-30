using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Atomex.Common;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Serilog;

namespace Atomex.Client.Desktop.Services
{
    public class ImageService
    {
        public IDictionary<string, IBitmap> Images;

        public ImageService()
        {
            Images = new ConcurrentDictionary<string, IBitmap>();

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var defaultBitmap =
                new Bitmap(assets.Open(new Uri("avares://Atomex.Client.Desktop/Resources/Images/logo_white.png")));
            Images["default"] = defaultBitmap;
        }

        public async Task LoadImageFromUrl(string url, Action successCallback = null)
        {
            try
            {
                var response = await HttpHelper.HttpClient
                    .GetAsync(url)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var previewBytes = await response.Content
                        .ReadAsByteArrayAsync()
                        .ConfigureAwait(false);

                    Stream imgStream = new MemoryStream(previewBytes);
                    using System.Drawing.Bitmap bmp = new(imgStream);
                    await using MemoryStream memory = new();
                    bmp.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    Images
                        [url] = new Bitmap(memory);

                    successCallback?.Invoke();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error during loading image {url} \n {e}");
            }
        }

        public bool GetImageLoaded(string url)
        {
            return url != null && Images.TryGetValue(url, out _);
        }

        public IBitmap GetImage(string? imageSource)
        {
            if (imageSource == null) return Images["default"];
            
            if (!imageSource.StartsWith("http"))
            {
                try
                {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    return new Bitmap(assets!.Open(new Uri($"avares://Atomex.Client.Desktop/Resources/Images/{imageSource}")));
                }
                catch (Exception e)
                {
                    Log.Error(e, "Can't find image {Source} in resources", imageSource);
                }
            }

            return Images.TryGetValue(imageSource, out var image)
                ? image
                : Images["default"];
        }
    }
}