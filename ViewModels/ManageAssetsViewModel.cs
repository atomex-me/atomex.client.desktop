using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Atomex.Client.Desktop.ViewModels
{
    public class AssetWithSelection : ViewModelBase
    {
        public IAssetViewModel Asset { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        public Action OnChanged { get; set; }

        public AssetWithSelection()
        {
            this.WhenAnyValue(vm => vm.IsSelected)
                .SubscribeInMainThread(_ => OnChanged?.Invoke());
        }
    }

    public class ManageAssetsViewModel : ViewModelBase
    {
        private ObservableCollection<AssetWithSelection> InitialAssets { get; set; }
        private ObservableCollection<AssetWithSelection> BeforeSearchAssets { get; set; }
        [Reactive] public ObservableCollection<AssetWithSelection> AvailableAssets { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public bool HideZeroBalances { get; set; }
        public Action<IEnumerable<string>> OnAssetsChanged { get; set; }

        public ManageAssetsViewModel()
        {
            this.WhenAnyValue(vm => vm.AvailableAssets)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(_ =>
                {
                    AvailableAssets.ForEachDo(asset => asset.OnChanged = () =>
                    {
                        OnAssetsChanged?.Invoke(InitialAssets
                            .Where(a => a.IsSelected)
                            .Select(assetWithSelection => assetWithSelection.Asset.CurrencyCode));
                    });

                    InitialAssets = new ObservableCollection<AssetWithSelection>(AvailableAssets);
                });

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .Where(_ => AvailableAssets != null)
                .SubscribeInMainThread(searchPattern =>
                {
                    if (searchPattern == string.Empty)
                        BeforeSearchAssets = new ObservableCollection<AssetWithSelection>(AvailableAssets);

                    var filteredAssets = InitialAssets
                        .Where(c => c.Asset.CurrencyCode.ToLower().Contains(searchPattern.ToLower())
                                    || c.Asset.CurrencyDescription.ToLower().Contains(searchPattern.ToLower()));

                    AvailableAssets = new ObservableCollection<AssetWithSelection>(filteredAssets);
                });

            SearchPattern = string.Empty;
        }
    }
}