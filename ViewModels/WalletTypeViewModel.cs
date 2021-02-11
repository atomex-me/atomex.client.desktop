using Atomex.Core;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletTypeViewModel : StepViewModel
    {
        public static Network[] Networks { get; } =
        {
            Network.MainNet,
            Network.TestNet
        };

        private Network _network;

        public Network Network
        {
            get => _network;
            set { this.RaiseAndSetIfChanged(ref _network, value); }
        }

        public WalletTypeViewModel()
        {
            Network = Network.MainNet;
        }

        public override void Next()
        {
            RaiseOnNext(new StepData
            {
                Network = Network
            });
        }
    }
}