using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Atomex.Core;
using Serilog;

namespace Atomex.Client.Desktop.Common
{
    public class WalletInfo
    {
        private const string DefaultWalletsDirectory = "wallets";
        public const string DefaultWalletFileName = "atomex.wallet";

        public string Name { get; set; }
        public string Path { get; set; }
        public Network Network { get; set; }

        public string Description => Network == Network.MainNet
            ? Name
            : $"[test] {Name}";

        public static IEnumerable<WalletInfo> AvailableWallets()
        {
            var result = new List<WalletInfo>();

            if (!Directory.Exists(CurrentWalletDirectory))
            {
                return result;
            }

            var walletsDirectory = new DirectoryInfo(CurrentWalletDirectory);

            foreach (var directory in walletsDirectory.GetDirectories())
            {
                var walletFile = directory
                    .GetFiles(DefaultWalletFileName)
                    .FirstOrDefault();

                if (walletFile != null)
                {
                    try
                    {
                        Network type;

                        using (var stream = walletFile.OpenRead())
                        {
                            type = stream.ReadByte() == 0
                                ? Network.MainNet
                                : Network.TestNet;
                        }

                        result.Add(new WalletInfo
                        {
                            Name = directory.Name,
                            Path = walletFile.FullName,
                            Network = type
                        });
                    }
                    catch (Exception)
                    {
                        Log.Warning("Wallet file {@fullName} scan error", walletFile.FullName);
                    }
                }
            }

            result.Sort((a, b) => a.Network.CompareTo(b.Network));

            return result;
        }


        public static string CurrentWalletDirectory
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/Library/Application Support/com.atomex.osx/{DefaultWalletsDirectory}";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/.local/share/atomex.client.desktop/{DefaultWalletsDirectory}";

                return $"{AppDomain.CurrentDomain.BaseDirectory}{DefaultWalletsDirectory}";
            }
        }
    }
}