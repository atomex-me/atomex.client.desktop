using System;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using Serilog;

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
        
        
        private ReactiveCommand<string, Unit>? _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>(data =>
        {
            try
            {
                App.Clipboard.SetTextAsync(data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

#if DEBUG
        private void DesignerMode()
        {
            CollectibleName = "ONIMATA - SSR Card";
            TokenId = 547288;
            From = "tz1cwWy1DPNcd5imGficjrMQv184rHeW1Qh5";
            To = "tz1cwWy1DPNcd5imGficjrMQv184rHeW1Qh5";
            CurrencyCode = "OBJKT";
            Fee = 0.034559m;
            Amount = 1;

            ConfirmStage = true;
        }
#endif
    }
}