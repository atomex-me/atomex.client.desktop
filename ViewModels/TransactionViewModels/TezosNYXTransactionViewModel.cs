using Atomex.Blockchain.Tezos;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TezosNYXTransactionViewModel : TezosFA12TransactionViewModel
    {
        public TezosNYXTransactionViewModel()
            : base()
        {
        }

        public TezosNYXTransactionViewModel(TezosTransaction tx)
            : base(tx)
        {
        }
    }
}
