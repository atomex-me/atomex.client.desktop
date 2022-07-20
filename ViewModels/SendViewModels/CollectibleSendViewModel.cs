namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class CollectibleSendViewModel : TezosTokensSendViewModel
    {
        public string PreviewUrl { get; set; }

        public CollectibleSendViewModel(
            IAtomexApp app,
            string tokenContract,
            int tokenId,
            string tokenType,
            string previewUrl,
            string? from = null)
            : base(app: app,
                tokenContract: tokenContract,
                tokenId: tokenId,
                tokenType: tokenType,
                tokenPreview: null,
                from: from)
        {
            PreviewUrl = previewUrl;
        }
    }
}