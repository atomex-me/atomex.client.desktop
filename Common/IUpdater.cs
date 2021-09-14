using NetSparkleUpdater;
using NetSparkleUpdater.Interfaces;

namespace Atomex.Client.Desktop.Common
{
    public interface IUpdater
    {
        IUpdateDownloader UpdateDownloader { get; set; }
        ISignatureVerifier SignatureVerifier { get; set; }
        
        void InstallUpdate(AppCastItem item, string installPath = null);
    }
}