using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Atomex.Client.Desktop.Common;
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

        public async Task LoadImageFromUrl(string url, Action? successCallback = null)
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

                    using (var ms = new MemoryStream(previewBytes))
                    {
                        Images[url] = new Bitmap(ms);
                    }

                    successCallback?.Invoke();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error during loading image {Url}", url);
            }
        }

        public bool TryGetImage(string url, [MaybeNullWhen(false)] out IBitmap value)
        {
            return Images.TryGetValue(url, out value);
        }

        public IBitmap GetImage(string? imageSource)
        {
            if (imageSource == null) return Images["default"];

            if (!imageSource.StartsWith("http"))
            {
                try
                {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    return new Bitmap(
                        assets!.Open(new Uri($"avares://Atomex.Client.Desktop/Resources/Images/{imageSource}")));
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

    public class ImageLoader : DiskCachedWebImageLoader
    {
        private ImageLoader() : base(cacheFolder: $"{WalletInfo.BaseUserDataDirectory}Cache/Images/")
        {
        }

        public static ImageLoader Instance { get; } = new();
    }
}