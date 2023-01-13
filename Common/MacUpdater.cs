using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Common;
using NetSparkleUpdater;
using NetSparkleUpdater.Interfaces;
using Serilog;

namespace Atomex.Client.Desktop.Common
{
    public class MacUpdater : IUpdater
    {
        private static string LaunchdFileName => "com.atomex.osx.plist";

        private static string LaunchdDirFilePath => Path.Combine(
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}",
            "Library/LaunchAgents",
            LaunchdFileName);

        private static string AppName { get; set; } = "Atomex.app";

        public bool CheckServerFileName { get; set; } = true;

        public IUpdateDownloader UpdateDownloader { get; set; }

        public ISignatureVerifier SignatureVerifier { get; set; }


        private string _tmpDownloadFilePath;

        public string TmpDownloadFilePath
        {
            get { return _tmpDownloadFilePath; }
            set { _tmpDownloadFilePath = value?.Trim(); }
        }

        private string _restartExecutableName;

        public string RestartExecutableName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_restartExecutableName))
                {
                    return _restartExecutableName;
                }
#if NETCORE
                try
                {
                    return Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                } 
                catch (Exception e)
                {
                    LogWriter?.PrintMessage("Unable to get executable name: " + e.Message);
                }
#endif
                // we cannot just use Path.GetFileName because on .NET Framework it can fail with
                // invalid chars in the path, so we do some crazy things to get the file name anotehr way
                var cmdLine = Environment.CommandLine.Trim().TrimStart('"').TrimEnd('"');
                return cmdLine.Substring(cmdLine.LastIndexOf(Path.DirectorySeparatorChar) + 1).Trim();
            }
            set { _restartExecutableName = value; }
        }

        private string _restartExecutablePath;

        public string RestartExecutablePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_restartExecutablePath))
                {
                    return _restartExecutablePath;
                }

                return Utilities.GetFullBaseDirectory();
            }
            set { _restartExecutablePath = value; }
        }

        public async Task<string> GetDownloadPathForAppCastItem(AppCastItem item)
        {
            if (item != null && item.DownloadLink != null)
            {
                string filename = string.Empty;

                // default to using the server's file name as the download file name
                if (CheckServerFileName && UpdateDownloader != null)
                {
                    try
                    {
                        filename = await UpdateDownloader.RetrieveDestinationFileNameAsync(item);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }

                if (string.IsNullOrEmpty(filename))
                {
                    // attempt to get download file name based on download link
                    try
                    {
                        filename = Path.GetFileName(new Uri(item.DownloadLink).LocalPath);
                    }
                    catch (UriFormatException)
                    {
                        // ignore
                    }
                }

                if (!string.IsNullOrEmpty(filename))
                {
                    string tmpPath = string.IsNullOrEmpty(TmpDownloadFilePath)
                        ? Path.GetTempPath()
                        : TmpDownloadFilePath;

                    // Creates all directories and subdirectories in the specific path unless they already exist.
                    Directory.CreateDirectory(tmpPath);

                    return Path.Combine(tmpPath, filename);
                }
            }

            return null;
        }


        public async void InstallUpdate(AppCastItem item, string installPath = null)
        {
            var path = installPath != null && File.Exists(installPath)
                ? installPath
                : await GetDownloadPathForAppCastItem(item);
            if (File.Exists(path))
            {
                NetSparkleUpdater.Enums.ValidationResult result;
                try
                {
                    result = SignatureVerifier.VerifySignatureOfFile(item.DownloadSignature, path);
                }
                catch (Exception exc)
                {
                    Log.Error("Error validating signature of file: {0}; {1}", exc.Message,
                        exc.StackTrace);
                    result = NetSparkleUpdater.Enums.ValidationResult.Invalid;
                }

                if (result == NetSparkleUpdater.Enums.ValidationResult.Valid)
                {
                    await RunDownloadedInstaller(path);
                }
            }
        }

        protected virtual async Task RunDownloadedInstaller(string downloadFilePath)
        {
            Log.Information("Running downloaded installer");
            // get the options for restarting the application
            string executableName = RestartExecutableName;
            string workingDir = RestartExecutablePath;

            // generate the batch file path
#if NETFRAMEWORK
            bool isWindows = true;
            bool isMacOS = false;
#else
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
            var extension = isWindows ? ".cmd" : ".sh";
            string batchFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);

            // generate the batch file                
            Log.Information("Generating batch in {0}", Path.GetFullPath(batchFilePath));

            var atomexAppInWorkDir = workingDir
                .Split("/")
                .ToList()
                .FindIndex(s => s.Contains(".app"));

            if (atomexAppInWorkDir != -1)
            {
                AppName = workingDir.Split("/")[atomexAppInWorkDir];
                workingDir = workingDir.Substring(0, workingDir.IndexOf(AppName, StringComparison.Ordinal));
            }

            using (FileStream stream = new FileStream(batchFilePath, FileMode.Create, FileAccess.ReadWrite,
                       FileShare.None, 4096, true))
            using (StreamWriter write = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                if (!isWindows)
                {
                    if (IsZipDownload(downloadFilePath)) // .zip on macOS or .tar.gz on Linux
                    {
                        var tarCommand = isMacOS
                            ? $"tar -x -f {downloadFilePath} -C \"{workingDir}{AppName}\" --strip-components=1"
                            : $"tar -xf {downloadFilePath} -C \"{workingDir}\" --overwrite";

                        var rmCommand = $"rm {downloadFilePath}";

                        var output = $@"{tarCommand}
{rmCommand}";
                        await write.WriteAsync(output);
                    }
                    else
                    {
                        Log.Error("Updater: Invalid update package, required .ZIP archive");
                        return;
                    }

                    write.Close();
                    // if we're on unix, we need to make the script executable!
                    Exec($"chmod +x {batchFilePath}"); // this could probably be made async at some point
                }
            }

            Exec($"rm -f {LaunchdDirFilePath}");

            await using (FileStream stream = new FileStream(LaunchdDirFilePath, FileMode.Create, FileAccess.ReadWrite,
                             FileShare.None, 4096, true))
            await using (StreamWriter write = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                var output = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
    <dict>
        <key>Label</key>
        <string>Atomex launcher</string>
        <key>RunAtLoad</key>
        <false/>
        <key>StartInterval</key>
        <integer>2</integer>
        <key>ProgramArguments</key>
        <array>
            <string>/bin/bash</string>
            <string>-c</string>
            <string>/usr/bin/open ""{workingDir}{AppName}"";launchctl unload -w {LaunchdDirFilePath}</string>
        </array>
   </dict>
</plist>
";
                await write.WriteAsync(output);
                write.Close();
            }

            Exec(batchFilePath);

            var processInfo = new ProcessStartInfo
            {
                Arguments = $"load -w {LaunchdDirFilePath}",
                FileName = "launchctl",
                UseShellExecute = true
            };

            var proc = Process.Start(processInfo);
            await proc!.WaitForExitAsync();

            Process.GetCurrentProcess().Kill();
        }

        private bool IsZipDownload(string downloadFilePath)
        {
            bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if ((isMacOS && IsZipValid(downloadFilePath)) ||
                (isLinux && downloadFilePath.EndsWith(".tar.gz")))
            {
                return true;
            }

            return false;
        }

        public static bool IsZipValid(string path)
        {
            try
            {
                using (var zipFile = ZipFile.OpenRead(path))
                {
                    var entries = zipFile.Entries;
                    return true;
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        // Exec grabbed from https://stackoverflow.com/a/47918132/3938401
        // for easy /bin/bash commands
        public static void Exec(string cmd, bool waitForExit = true)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            using (var process = new Process()
                   {
                       StartInfo = new ProcessStartInfo
                       {
                           RedirectStandardOutput = true,
                           UseShellExecute = false,
                           CreateNoWindow = true,
                           WindowStyle = ProcessWindowStyle.Hidden,
                           FileName = "/bin/bash",
                           Arguments = $"-c \"{escapedArgs}\""
                       }
                   })
            {
                process.Start();
                if (waitForExit)
                {
                    process.WaitForExit();
                }
            }
        }

        public static async void CheckForMacOsDeepLinks()
        {
            if (File.Exists("deeplinks_activated")) return;
            var targetVer = WalletMainViewModel.GetAssemblyFileVersion();
            
            if (PlatformHelper.GetClientType() != ClientType.AvaloniaMac) return;

            var workingDir = Utilities.GetFullBaseDirectory();

            var atomexAppInWorkDir = workingDir
                .Split("/")
                .ToList()
                .FindIndex(s => s.Contains(".app"));

            if (atomexAppInWorkDir == -1) return;

            AppName = workingDir.Split("/")[atomexAppInWorkDir];
            workingDir = workingDir.Substring(0, workingDir.IndexOf(AppName, StringComparison.Ordinal));
            var applicationFolder = $"{workingDir}{AppName}";

            var updateScript = $"! test -f {applicationFolder}/Contents/MacOS/deeplinks_activated && " +
                               $"launchctl unload -w {LaunchdDirFilePath} && " +
                               $"curl https://github.com/atomex-me/atomex.client.desktop/releases/download/{targetVer}/Atomex.{targetVer}.dmg -Lo {Path.Combine(Path.GetTempPath(), $"Atomex.{targetVer}.dmg")} && " +
                               $"hdiutil attach {Path.Combine(Path.GetTempPath(), $"Atomex.{targetVer}.dmg")} && " +
                               $"rm -rf {applicationFolder} && " +
                               $"cp -R /Volumes/Atomex/Atomex.app {workingDir} && " +
                               "hdiutil unmount /Volumes/Atomex && " +
                               $"touch {applicationFolder} && " +
                               $"rm {Path.Combine(Path.GetTempPath(), $"Atomex.{targetVer}.dmg")} && " +
                               $"touch {applicationFolder}/Contents/MacOS/deeplinks_activated";

            Log.Error("Deeplink script\n{Script}", updateScript);
            Exec(updateScript);
            
            var processInfo = new ProcessStartInfo
            {
                Arguments = $"load -w {LaunchdDirFilePath}",
                FileName = "launchctl",
                UseShellExecute = true
            };
            
            var proc = Process.Start(processInfo);
            await proc!.WaitForExitAsync();
            Process.GetCurrentProcess().Kill();
        }
    }
}