using System;
using System.Net.Http;
using AsyncImageLoader.Loaders;
using Atomex.Client.Desktop.Common;
namespace Atomex.Client.Desktop.Services
{
    public class ImageLoader : DiskCachedWebImageLoader
    {
        private ImageLoader() : base(cacheFolder: $"{WalletInfo.BaseUserDataDirectory}Cache/Images/",
            httpClient: new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(180)
            }, disposeHttpClient: true)
        {
        }

        public static ImageLoader Instance { get; } = new();
    }
}