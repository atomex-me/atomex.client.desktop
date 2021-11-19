using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NetSparkleUpdater;
using NetSparkleUpdater.Interfaces;
using Serilog;

namespace Atomex.Client.Desktop.Common
{
    public class LinuxUpdater : IUpdater
    {
        private bool CheckServerFileName { get; set; } = true;

        public IUpdateDownloader UpdateDownloader { get; set; }

        public ISignatureVerifier SignatureVerifier { get; set; }


        private string _tmpDownloadFilePath;
        
        private string TmpDownloadFilePath
        {
            get { return _tmpDownloadFilePath; }
            set { _tmpDownloadFilePath = value?.Trim(); }
        }
        
        private async Task<string> GetDownloadPathForAppCastItem(AppCastItem item)
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

            var extension = ".sh";
            string batchFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);

            // generate the batch file                
            Log.Information("Generating batch in {0}", Path.GetFullPath(batchFilePath));

            var xAuthorutyProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"echo $XAUTHORITY\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            xAuthorutyProc.Start();

            var xAuthority = string.Empty;
            while (!xAuthorutyProc.StandardOutput.EndOfStream)
            {
                xAuthority = xAuthorutyProc.StandardOutput.ReadLine();
            }

            var displayProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"echo $DISPLAY\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            displayProc.Start();

            var display = string.Empty;
            while (!displayProc.StandardOutput.EndOfStream)
            {
                display = displayProc.StandardOutput.ReadLine();
            }

            await using (FileStream stream = new FileStream(batchFilePath, FileMode.Create, FileAccess.ReadWrite,
                FileShare.None, 4096, true))
            await using (StreamWriter write = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                string userName = Environment.UserName;

                var installCommand = $"sudo dpkg -i {downloadFilePath}";
                var rmCommand = $"rm {downloadFilePath}";
                var sysCtlCommand = $@"sudo rm -f /usr/lib/systemd/system/atomex.service
echo -n ""[Unit]
Description=Start Atomex

[Service]
Environment=\""DISPLAY={display}\""
Environment=\""XAUTHORITY={xAuthority}\""
ExecStart=/opt/Atomex.Client.Desktop/Atomex.Client.Desktop
User={userName}
Group={userName}

[Install]
WantedBy=graphical.target"" >> /usr/lib/systemd/system/atomex.service

sudo systemctl daemon-reload";

                var restartCommand = "sudo systemctl start atomex.service";

                var output = $@"#!/bin/bash
{installCommand}
{rmCommand}
{sysCtlCommand}
{restartCommand}";

                await write.WriteAsync(output);

                write.Close();
                Exec($"chmod +x {batchFilePath}");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"pkexec {batchFilePath}\""
            };

            using (var p = Process.Start(processStartInfo))
            {
                if (p != null)
                {
                    await p.WaitForExitAsync();
                }
            }

            Environment.Exit(0);
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