using System;
using System.Collections;
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
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;
using OxyPlot;
using OxyPlot.Avalonia;
using OxyPlot.Series;
using ReactiveUI.Fody.Helpers;
using Serilog;
using PieSeries = OxyPlot.Series.PieSeries;


namespace Atomex.Client.Desktop.ViewModels
{
    public class PortfolioViewModel : ViewModelBase
    {
        private const string DefaultPortfolioFormat = "0.00";
        private IAtomexApp App { get; }
        public PlotModel PlotModel { get; set; }
        [Reactive] public IList<CurrencyViewModel> AllCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> ChoosenCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> InitialChoosenCurrencies { get; set; }
        private Color NoTokensColor { get; } = Color.FromArgb(50, 0, 0, 0);
        public SelectCurrencyType SelectCurrencyUseCase { get; set; }
        public Action<CurrencyConfig?> SetDexTab { get; set; }
        [Reactive] public decimal PortfolioValue { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public CurrencyViewModel? SelectedCurrency { get; set; }


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
                            var receiveViewModel = new ReceiveViewModel(App, selectedCurrency.Currency)
                            {
                                OnBack = () => ReceiveCommand.Execute().Subscribe()
                            };
                            Desktop.App.DialogService.Show(receiveViewModel);
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

                    ChoosenCurrencies = new List<CurrencyViewModel>(filteredCurrencies);
                });

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.AtomexClientChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs e)
        {
            AllCurrencies = e.AtomexClient?.Account?.Currencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c);
                    vm.AmountUpdated += OnAmountUpdatedEventHandler;
                    return vm;
                })
                .ToList() ?? new List<CurrencyViewModel>();

            // todo: select from settings
            ChoosenCurrencies = new List<CurrencyViewModel>(AllCurrencies);
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

        private ReactiveCommand<Unit, Unit> _exchangeCommand;

        public ReactiveCommand<Unit, Unit> ExchangeCommand => _exchangeCommand ??= _exchangeCommand =
            ReactiveCommand.Create(() => { SetDexTab?.Invoke(null); });

        private ReactiveCommand<Unit, Unit> _manageAssetsCommand;

        public ReactiveCommand<Unit, Unit> ManageAssetsCommand => _manageAssetsCommand ??= _manageAssetsCommand =
            ReactiveCommand.Create(() =>
            {
                var vm = new ManageAssetsViewModel
                {
                    AvailableCurrencies = new ObservableCollection<CurrencyWithSelection>(
                        AllCurrencies.Select(currency => new CurrencyWithSelection
                        {
                            Currency = currency,
                            IsSelected = InitialChoosenCurrencies.Contains(currency)
                        })
                    ),
                    OnAssetsChanged = currencies =>
                    {
                        ChoosenCurrencies = new List<CurrencyViewModel>(currencies);
                        InitialChoosenCurrencies = new List<CurrencyViewModel>(ChoosenCurrencies);
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

            ChoosenCurrencies = DesignTime.Currencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false);
                    vm.TotalAmountInBase = random.Next(1000000, 10000000);
                    return vm;
                })
                .ToList();

            SearchPattern = "BTC";

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }
    }
}