using System;
using System.Collections.ObjectModel;
using Atomex.Services;
using Avalonia.Media.Imaging;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DappViewModel : ViewModelBase
    {
        public string Logo { get; set; }
        public IBitmap BitmapLogo => App.ImageService.GetImage(Logo);
        public string Name { get; set; }
        public DateTime ConnectTime { get; set; }
    }

    public class DappsViewModel : ViewModelBase
    {
        private readonly IAtomexApp AtomexApp;
        [Reactive] public ObservableCollection<DappViewModel> Dapps { get; set; }

        public DappsViewModel(IAtomexApp atomexApp)
        {
            AtomexApp = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));

            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }


        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (args.AtomexClient?.Account == null) return;

            
        }
    }
}