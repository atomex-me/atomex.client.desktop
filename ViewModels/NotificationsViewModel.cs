using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Services;
using Atomex.Common;
using Atomex.Wallet;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class NotificationsViewModel : ViewModelBase
    {
        // private IAtomexApp AtomexApp;
        [Reactive] public bool IsOpened { get; set; }
        [Reactive] public ObservableCollection<AtomexNotification> Notifications { get; set; }
        [Reactive] public int SelectedIndex { get; set; }

        public NotificationsViewModel()
        {
            if (Design.IsDesignMode)
            {
                DesignerMode();
                return;
            }

            this.WhenAnyValue(vm => vm.SelectedIndex)
                .Where(index => Notifications != null && index != null && index >= 0)
                .SubscribeInMainThread(idx =>
                {
                    Log.Fatal($"{idx}");
                    if (Notifications[idx].IsRead) return;
                    App.NotificationsService.ReadById(Notifications[idx].Id);
                });

            App.NotificationsService.NotificationsUpdated += OnNotificationsUpdated;
        }

        // public NotificationsViewModel()
        // {
        //     // AtomexApp = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));
        // }


        private void OnNotificationsUpdated(object sender, AtomexNotificationsEventArgs args)
        {
            // var selectedIdx = SelectedIndex;
            // Notifications = new ObservableCollection<AtomexNotification>();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                
                var notList = args.AtomexNotifications.ToList();
                
                if (notList.Count > 1)
                    Notifications = new ObservableCollection<AtomexNotification>(
                            notList.Remove(el => el.));
                // SelectedIndex = selectedIdx;
            });
        }


        private ReactiveCommand<Unit, Unit> _changeOpenedStateCommand;

        public ReactiveCommand<Unit, Unit> ChangeOpenedStateCommand => _changeOpenedStateCommand ??=
            (_changeOpenedStateCommand = ReactiveCommand.Create(() => { IsOpened = !IsOpened; }));

        private ReactiveCommand<Unit, Unit> _markAllAsReadCommand;

        public ReactiveCommand<Unit, Unit> MarkAllAsReadCommand => _markAllAsReadCommand ??=
            (_markAllAsReadCommand = ReactiveCommand.Create(() => App.NotificationsService.ReadAll()));

#if DEBUG
        private void DesignerMode()
        {
            IsOpened = true;

            Notifications = new ObservableCollection<AtomexNotification>(new List<AtomexNotification>
            {
                new AtomexNotification()
                {
                    AtomexNotificationType = AtomexNotificationType.Swap,
                    Message = "0.035 ETH exchanged for 0.13523 BTC",
                    Time = DateTime.Now,
                    IsRead = false
                },
                new AtomexNotification()
                {
                    AtomexNotificationType = AtomexNotificationType.Outcome,
                    Message = "0.018723 ETH sent to the external address",
                    Time = DateTime.Now,
                    IsRead = true
                },
                new AtomexNotification()
                {
                    AtomexNotificationType = AtomexNotificationType.Income,
                    Message = "0.035 ETH received from the external address",
                    Time = DateTime.Now,
                    IsRead = false
                },
                new AtomexNotification()
                {
                    AtomexNotificationType = AtomexNotificationType.Outcome,
                    Message = "0.018723 ETH sent to the external address",
                    Time = DateTime.Now,
                    IsRead = true
                }
            });
        }
#endif
    }
}