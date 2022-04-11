using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
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
            Images = new Dictionary<string, IBitmap>();

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var defaultBitmap =
                new Bitmap(assets.Open(new Uri("avares://Atomex.Client.Desktop/Resources/Images/logo_white.png")));
            Images.Add("default", defaultBitmap);
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
                    Images[url] = new Bitmap(memory);
                    
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

        public IBitmap GetImage(string url)
        {
            return url != null && Images.TryGetValue(url, out var image)
                ? image
                : Images["default"];
        }
    }
}