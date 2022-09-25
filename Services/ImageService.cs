using System;
using System.Net.Http;
using AsyncImageLoader.Loaders;
using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.Services
{
    public class FileCacheImageLoader : DiskCachedWebImageLoader
    {
        private FileCacheImageLoader() : base(cacheFolder: $"{WalletInfo.BaseUserDataDirectory}Cache/Images/",
            httpClient: new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(200)
            }, disposeHttpClient: true)
        {
        }

        public static FileCacheImageLoader Instance { get; } = new();
    }

    public class ImageLoader : RamCachedWebImageLoader
    {
        private ImageLoader() : base(httpClient: new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(200)
        }, disposeHttpClient: true)
        {
        }

        public static ImageLoader Instance { get; } = new();
    }
}