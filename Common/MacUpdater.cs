using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using NetSparkleUpdater;
using NetSparkleUpdater.Downloaders;
using NetSparkleUpdater.Interfaces;
using Serilog;

namespace Atomex.Client.Desktop.Common
{
    public class MacUpdater
    {
        /// <summary>
        /// Whether or not to check with the online server to verify download
        /// file names.
        /// </summary>
        public bool CheckServerFileName { get; set; } = true;

        /// <summary>
        /// The object responsable for downloading update files for your application
        /// </summary>
        public IUpdateDownloader UpdateDownloader { get; set; }

        /// <summary>
        /// The object that verifies signatures (DSA, Ed25519, or otherwise) of downloaded items
        /// </summary>
        public ISignatureVerifier SignatureVerifier { get; set; }


        private string _tmpDownloadFilePath;

        /// <summary>
        /// If set, downloads files to this path. If the folder doesn't already exist, creates
        /// the folder at download time (and not before). 
        /// Note that this variable is a path, not a full file name.
        /// </summary>
        public string TmpDownloadFilePath
        {
            get { return _tmpDownloadFilePath; }
            set { _tmpDownloadFilePath = value?.Trim(); }
        }

        /// <summary>
        /// Defines if the application needs to be relaunched after executing the downloaded installer
        /// </summary>
        public bool RelaunchAfterUpdate { get; set; }

        private string _restartExecutableName;

        /// <summary>
        /// Executable name to use when restarting the software.
        /// This is the name that will be used/started when the update has been installed.
        /// This defaults to <see cref="Environment.CommandLine"/>.
        /// Used in conjunction with RestartExecutablePath to restart the application --
        /// cd "{RestartExecutablePath}"
        /// "{RestartExecutableName}" is what is called to restart the app.
        /// </summary>
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

        /// <summary>
        /// Path to the working directory for the current application.
        /// This is the directory that the current executable sits in --
        /// e.g. C:/Users/...Foo/. It will be used when restarting the
        /// application on Windows or will be used on macOS/Linux for
        /// overwriting files on an update.
        /// </summary>
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

        /// <summary>
        /// Get the download path for a given app cast item.
        /// If any directories need to be created, this function
        /// will create those directories.
        /// </summary>
        /// <param name="item">The item that you want to generate a download path for</param>
        /// <returns>The download path for an app cast item if item is not null and has valid download link
        /// Otherwise returns null.</returns>
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


        /// <summary>
        /// Run the provided app cast item update regardless of what else is going on.
        /// Note that a more up to date download may be taking place, so if you don't
        /// want to run a potentially out-of-date installer, don't use this. This should
        /// only be used if your user wants to update before another update has been
        /// installed AND the file is already downloaded.
        /// This function will verify that the file exists and that the DSA 
        /// signature is valid before running. It will also utilize the
        /// PreparingToExit event to ensure that the application can close.
        /// </summary>
        /// <param name="item">AppCastItem to install</param>
        /// <param name="installPath">Install path to the executable. If not provided, will ask the server for the download path.</param>
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


        /// <summary>
        /// Updates the application via the file at the given path. Figures out which command needs
        /// to be run, sets up the application so that it will start the downloaded file once the
        /// main application stops, and then waits to start the downloaded update.
        /// </summary>
        /// <param name="downloadFilePath">path to the downloaded installer/updater</param>
        /// <returns>the awaitable <see cref="Task"/> for the application quitting</returns>
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
            

            var atomexAppInWorkDir = workingDir.IndexOf("Atomex.app");
            
            if (atomexAppInWorkDir != -1)
            {
                workingDir = workingDir.Substring(0, atomexAppInWorkDir);
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
                            ? $"tar -x -f {downloadFilePath} -C \"{workingDir}\""
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
            
            Exec(batchFilePath);

            // relaunch app
            var processInfo = new ProcessStartInfo
            {
                Arguments = $"-n {workingDir}Atomex.app",
                FileName = "open",
                UseShellExecute = true
            };

            var process = Process.Start(processInfo);
            process!.WaitForExit();
            
            Process.GetCurrentProcess().Kill();
        }

        private bool IsZipDownload(string downloadFilePath)
        {
            string installerExt = Path.GetExtension(downloadFilePath);
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

        /// <summary>
        /// Checks to see if two extensions match (this is basically just a 
        /// convenient string comparison). Both extensions should include the
        /// initial . (full-stop/period) in the extension.
        /// </summary>
        /// <param name="extension">first extension to check</param>
        /// <param name="otherExtension">other extension to check</param>
        /// <returns>true if the extensions match; false otherwise</returns>
        protected bool DoExtensionsMatch(string extension, string otherExtension)
        {
            return extension.Equals(otherExtension, StringComparison.CurrentCultureIgnoreCase);
        }

        // Exec grabbed from https://stackoverflow.com/a/47918132/3938401
        // for easy /bin/bash commands
        private static void Exec(string cmd, bool waitForExit = true)
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
    }
}