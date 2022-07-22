using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class CollectibleSendViewModel : TezosTokensSendViewModel
    {
        public string PreviewUrl { get; set; }
        public string CollectibleName { get; set; }

        public CollectibleSendViewModel(
            IAtomexApp app,
            string tokenContract,
            int tokenId,
            string tokenType,
            string previewUrl,
            string collectibleName,
            string? from = null)
            : base(app: app,
                tokenContract: tokenContract,
                tokenId: tokenId,
                tokenType: tokenType,
                tokenPreview: null,
                from: from,
                showToSelectDialog: false)
        {
            PreviewUrl = previewUrl;
            CollectibleName = collectibleName;
        }

        public CollectibleSendViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

#if DEBUG
        private void DesignerMode()
        {
            CollectibleName = "ONIMATA - SSR Card";
            TokenId = 547288;
            From = "tz1cwWy1DPNcd5imGficjrMQv184rHeW1Qh5";
            CurrencyCode = "OBJKT";
        }
#endif
    }
}