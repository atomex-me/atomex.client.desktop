using System.Runtime.InteropServices;

using Atomex.Common;

namespace Atomex.Client.Desktop.Common
{
    public static class PlatformHelper
    {
        public static ClientType GetClientType()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ClientType.AvaloniaWindows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return ClientType.AvaloniaMac;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ClientType.AvaloniaLinux;
            else
                return ClientType.Unknown;
        }
    }
}