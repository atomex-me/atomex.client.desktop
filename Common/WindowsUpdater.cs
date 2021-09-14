using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.SignatureVerifiers;
using Serilog;

namespace Atomex.Client.Desktop.Common
{

    public class WindowsUpdater : SparkleUpdater, IUpdater
    {
        public WindowsUpdater(string appcastUrl, ISignatureVerifier signatureVerifier) : base(appcastUrl, signatureVerifier)
        {
            RelaunchAfterUpdate = true;
            CloseApplication += () =>
            {
                if (Application.Current.ApplicationLifetime is IControlledApplicationLifetime
                    applicationLifetime)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        applicationLifetime.Shutdown(0);
                    });
                }
            };
        }

        public WindowsUpdater(string appcastUrl, ISignatureVerifier signatureVerifier, string referenceAssembly)
            : base(appcastUrl, signatureVerifier, referenceAssembly)
        {
        }

        public WindowsUpdater(string appcastUrl, ISignatureVerifier signatureVerifier, string referenceAssembly, IUIFactory factory)
            : base(appcastUrl, signatureVerifier, referenceAssembly, factory)
        {
        }
        
        protected override string GetWindowsInstallerCommand(string downloadFilePath)
        {
            return "msiexec /i \"" + downloadFilePath + "\"  /qb  INSTALLDIR=\"" + Directory.GetCurrentDirectory() + "\" REINSTALLMODE=amus";
        }
    }
}
