using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using Atomex.Common;
using Atomex.Services;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.ConversionViewModels;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Core;
using Atomex.TezosTokens;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;
using OxyPlot;
using OxyPlot.Avalonia;
using OxyPlot.Series;
using ReactiveUI.Fody.Helpers;
using PieSeries = OxyPlot.Series.PieSeries;


namespace Atomex.Client.Desktop.ViewModels
{
    public class PortfolioViewModel : ViewModelBase
    {
        private const string DefaultPortfolioFormat = "0.00";
        private IAtomexApp App { get; }
        public PlotModel PlotModel { get; set; }
        private Color NoTokensColor { get; } = Color.FromArgb(50, 0, 0, 0);
        public SelectCurrencyType SelectCurrencyUseCase { get; set; }
        public Action<CurrencyConfig?> SetDexTab { get; init; }
        public Action<string> SetWalletCurrency { get; init; }
        public Action<string> SetWertCurrency { get; init; }
        [Reactive] public decimal PortfolioValue { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public CurrencyViewModel? SelectedCurrency { get; set; }
        [Reactive] public string? PopupOpenedCurrency { get; set; }
        [Reactive] public IList<CurrencyViewModel> AllCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> ChoosenCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> InitialChoosenCurrencies { get; set; }

        private CurrencyViewModel TezosTokensCurrencyViewModel { get; set; }

        public PortfolioViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public PortfolioViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(selectedCurrency =>
                {
                    switch (SelectCurrencyUseCase)
                    {
                        case SelectCurrencyType.From:
                            var sendViewModel = SendViewModelCreator.CreateViewModel(App, selectedCurrency.Currency);
                            sendViewModel.SelectFromViewModel.BackAction = () => SendCommand.Execute().Subscribe();
                            Desktop.App.DialogService.Show(sendViewModel.SelectFromViewModel);
                            break;
                        case SelectCurrencyType.To:
                            ShowReceiveWindow(selectedCurrency.Currency);
                            break;
                    }
                });

            this.WhenAnyValue(vm => vm.ChoosenCurrencies)
                .WhereNotNull()
                .SubscribeInMainThread(_ => OnAmountUpdatedEventHandler(this, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .SubscribeInMainThread(searchPattern =>
                {
                    var filteredCurrencies = InitialChoosenCurrencies
                        .Where(c => c.Currency.Name.ToLower()
                                        .Contains(searchPattern?.ToLower() ?? string.Empty) ||
                                    c.Currency.Description.ToLower()
                                        .Contains(searchPattern?.ToLower() ?? string.Empty));

                    // todo: remove
                    ChoosenCurrencies = new List<CurrencyViewModel>(filteredCurrencies?
                        .Where(c => c.Header != TezosTokens)
                        .Append(TezosTokensCurrencyViewModel));
                });
            
            SubscribeToServices();
        }
        
        // todo: remove
        public static string TezosTokens => "Tezos tokens";

        private void SubscribeToServices()
        {
            App.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs e)
        {
            if (e.AtomexClient is null) return;

            AllCurrencies = e.AtomexClient?.Account?.Currencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c);
                    vm.AmountUpdated += OnAmountUpdatedEventHandler;
                    return vm;
                })
                .ToList() ?? new List<CurrencyViewModel>();
            
            // todo: remove
            var tezosConfig = App.Account.Currencies.Get<TezosConfig>(TezosConfig.Xtz);
            TezosTokensCurrencyViewModel = new TezosCurrencyViewModel(tezosConfig);
            TezosTokensCurrencyViewModel.Header = TezosTokens;
            AllCurrencies.Add(TezosTokensCurrencyViewModel);

            var savedCurrenciesArr =
                e.AtomexClient?.Account?.UserSettings?.InitializedCurrencies ??
                AllCurrencies.Select(c => c.Currency.Name).ToArray();

            ChoosenCurrencies = new List<CurrencyViewModel>(AllCurrencies)
                .Where(c => savedCurrenciesArr.Contains(c.Currency.Name))
                .ToList();

            InitialChoosenCurrencies = new List<CurrencyViewModel>(ChoosenCurrencies);

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        private void OnAmountUpdatedEventHandler(object sender, EventArgs args)
        {
            // update total portfolio value
            PortfolioValue = ChoosenCurrencies.Sum(c => c.TotalAmountInBase);

            // update currency portfolio percent
            ChoosenCurrencies.ForEachDo(c => c.PortfolioPercent = PortfolioValue != 0
                ? c.TotalAmountInBase / PortfolioValue
                : 0);

            this.RaisePropertyChanged(nameof(ChoosenCurrencies));

            UpdatePlotModel();
        }

        private void UpdatePlotModel()
        {
            var maxCurrencyBalance = ChoosenCurrencies.OrderByDescending(cvm => cvm.TotalAmountInBase);
            var currencyFormat = GetAmountFormat(maxCurrencyBalance.ElementAtOrDefault(0)?.TotalAmountInBase);

            var series = new PieSeries
            {
                StrokeThickness = 0,
                StartAngle = 0,
                AngleSpan = 360,
                TickHorizontalLength = 0,
                TickRadialLength = 0,
                OutsideLabelFormat = string.Empty,
                InsideLabelFormat = string.Empty,
                TrackerFormatString = "{1}: ${2:" + currencyFormat + "} ({3:P2})"
            };

            foreach (var currency in ChoosenCurrencies)
            {
                series.Slices.Add(
                    new PieSlice(currency.Currency.Name, (double)currency.TotalAmountInBase)
                    {
                        Fill = currency.AccentColor.ToOxyColor(),
                    });
            }

            if (PortfolioValue == 0)
            {
                series.Slices.Add(
                    new PieSlice(Properties.Resources.PwNoTokens, 1) { Fill = NoTokensColor.ToOxyColor() });
                series.TrackerFormatString = "{1}";
            }

            PlotModel = new PlotModel { Culture = CultureInfo.InvariantCulture };
            PlotModel.Series.Add(series);

            this.RaisePropertyChanged(nameof(PlotModel));
        }

        private void ShowReceiveWindow(CurrencyConfig currencyConfig)
        {
            var receiveViewModel = currencyConfig switch
            {
                Fa12Config fa12Config => new ReceiveViewModel(
                    app: App,
                    currency: App.Account.Currencies.GetByName(TezosConfig.Xtz),
                    tokenContract: fa12Config.TokenContractAddress,
                    tokenType: "FA12"),

                _ => new ReceiveViewModel(App, currencyConfig)
            };
            receiveViewModel.OnBack = () => ReceiveCommand.Execute().Subscribe();

            Desktop.App.DialogService.Show(receiveViewModel);
        }

        private ReactiveCommand<CurrencyViewModel, Unit> _setWalletCurrencyCommand;

        public ReactiveCommand<CurrencyViewModel, Unit> SetWalletCurrencyCommand => _setWalletCurrencyCommand ??=
            (_setWalletCurrencyCommand = ReactiveCommand.Create<CurrencyViewModel>(currencyViewModel =>
            {
                // todo: remove
                if (currencyViewModel.Header == TezosTokens)
                {
                    SetWalletCurrency?.Invoke(TezosTokens);
                    return;
                }

                SetWalletCurrency?.Invoke(currencyViewModel.Currency.Description);
            }));

        private ReactiveCommand<CurrencyViewModel, Unit> _setWertCurrencyCommand;

        public ReactiveCommand<CurrencyViewModel, Unit> SetWertCurrencyCommand => _setWertCurrencyCommand ??=
            (_setWertCurrencyCommand = ReactiveCommand.Create<CurrencyViewModel>(currencyViewModel =>
            {
                PopupOpenedCurrency = null;
                SetWertCurrency?.Invoke(currencyViewModel.Currency.Description);
            }));

        private ReactiveCommand<CurrencyViewModel, Unit> _openCurrencyPopupCommand;

        public ReactiveCommand<CurrencyViewModel, Unit> OpenCurrencyPopupCommand => _openCurrencyPopupCommand ??=
            (_openCurrencyPopupCommand = ReactiveCommand.Create<CurrencyViewModel>(currencyViewModel =>
            {
                PopupOpenedCurrency = currencyViewModel.Header;
            }));

        private ReactiveCommand<Unit, Unit> _sendCommand;

        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= (_sendCommand = ReactiveCommand.Create(() =>
        {
            var selectFromCurrencyViewModel =
                new SelectCurrencyWithoutAddressesViewModel(SelectCurrencyType.From, ChoosenCurrencies)
                {
                    OnSelected = currencyViewModel =>
                    {
                        SelectCurrencyUseCase = SelectCurrencyType.From;
                        SelectedCurrency = currencyViewModel;
                    }
                };

            SelectedCurrency = null;
            Desktop.App.DialogService.Show(selectFromCurrencyViewModel);
        }));

        private ReactiveCommand<CurrencyViewModel, Unit> _sendFromPopupCommand;

        public ReactiveCommand<CurrencyViewModel, Unit> SendFromPopupCommand => _sendFromPopupCommand ??=
            (_sendFromPopupCommand = ReactiveCommand.Create<CurrencyViewModel>(currencyViewModel =>
            {
                var sendViewModel = SendViewModelCreator.CreateViewModel(App, currencyViewModel.Currency);
                PopupOpenedCurrency = null;
                Desktop.App.DialogService.Show(sendViewModel.SelectFromViewModel);
            }));


        private ReactiveCommand<Unit, Unit> _receiveCommand;

        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= _receiveCommand =
            ReactiveCommand.Create(() =>
            {
                var selectReceiveCurrencyViewModel =
                    new SelectCurrencyWithoutAddressesViewModel(SelectCurrencyType.To, ChoosenCurrencies)
                    {
                        OnSelected = currencyViewModel =>
                        {
                            SelectCurrencyUseCase = SelectCurrencyType.To;
                            SelectedCurrency = currencyViewModel;
                        }
                    };
                
                SelectedCurrency = null;
                Desktop.App.DialogService.Show(selectReceiveCurrencyViewModel);
            });

        private ReactiveCommand<CurrencyViewModel, Unit> _receiveFromPopupCommand;

        public ReactiveCommand<CurrencyViewModel, Unit> ReceiveFromPopupCommand => _receiveFromPopupCommand ??=
            _receiveFromPopupCommand = ReactiveCommand.Create<CurrencyViewModel>(
                currencyViewModel =>
                {
                    PopupOpenedCurrency = null;
                    ShowReceiveWindow(currencyViewModel.Currency);
                });

        private ReactiveCommand<CurrencyViewModel, Unit> _exchangeCommand;

        public ReactiveCommand<CurrencyViewModel, Unit> ExchangeCommand => _exchangeCommand ??= _exchangeCommand =
            ReactiveCommand.Create<CurrencyViewModel>(currencyViewModel =>
            {
                PopupOpenedCurrency = null;
                SetDexTab?.Invoke(currencyViewModel?.Currency);
            });

        private ReactiveCommand<Unit, Unit> _manageAssetsCommand;

        public ReactiveCommand<Unit, Unit> ManageAssetsCommand => _manageAssetsCommand ??= _manageAssetsCommand =
            ReactiveCommand.Create(() =>
            {
                var vm = new ManageAssetsViewModel
                {
                    AvailableCurrencies = new ObservableCollection<CurrencyWithSelection>(
                        AllCurrencies
                            .Where(c => c.Header != TezosTokens)
                            .Select(currency => new CurrencyWithSelection
                        {
                            Currency = currency,
                            IsSelected = InitialChoosenCurrencies.Contains(currency)
                        })
                    ),
                    OnAssetsChanged = currencies =>
                    {
                        // todo: remove
                        ChoosenCurrencies = new List<CurrencyViewModel>(currencies.Append(TezosTokensCurrencyViewModel));

                        InitialChoosenCurrencies = new List<CurrencyViewModel>(ChoosenCurrencies);
                        
                        App.Account.UserSettings.InitializedCurrencies = ChoosenCurrencies
                            .Select(currency => currency.Currency.Name)
                            .ToArray();

                        App.Account.UserSettings.SaveToFile(App.Account.SettingsFilePath);
                    }
                };

                Desktop.App.DialogService.Show(vm);
            });

        public IController ActualController { get; set; }

        public static string GetAmountFormat(decimal? amount)
        {
            if (amount == null) return DefaultPortfolioFormat;

            return amount switch
            {
                > 999999999 => "0,,,.###B",
                > 999999 => "0,,.###M",
                _ => DefaultPortfolioFormat
            };
        }

        private void DesignerMode()
        {
            var random = new Random();

            PortfolioValue = 423394932.23m;

            ChoosenCurrencies = DesignTime.TestNetCurrencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false);
                    vm.TotalAmountInBase = random.Next(1000000, 10000000);
                    vm.TotalAmount = random.Next(1000000, 10000000);
                    vm.AvailableAmount = random.Next(1000000, 10000000);
                    return vm;
                })
                .ToList();

            SearchPattern = "BTC";

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }
    }
}