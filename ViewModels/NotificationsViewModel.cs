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
        [Reactive] public bool IsOpened { get; set; }
        [Reactive] private ObservableCollection<NotificationViewModel> Notifications { get; set; }
        [ObservableAsProperty] public bool HasUnread { get; }
        [Reactive] public int SelectedIndex { get; set; }

        public NotificationsViewModel()
        {
            if (Design.IsDesignMode)
            {
                DesignerMode();
                return;
            }

            this.WhenAnyValue(vm => vm.SelectedIndex)
                .WhereNotNull()
                .Where(_ => Notifications != null)
                .Where(index => index >= 0)
                .Where(index => !Notifications![index].IsRead)
                .Select(index => Notifications![index].Id)
                .SubscribeInMainThread(notificationId => { App.NotificationsService.ReadById(notificationId); });

            this.WhenAnyValue(vm => vm.Notifications)
                .WhereNotNull()
                .Select(notifications => notifications.Any(n => !n.IsRead))
                .ToPropertyExInMainThread(this, vm => vm.HasUnread);

            SelectedIndex = -1;
            App.NotificationsService.NotificationsUpdated += OnNotificationsUpdated;
        }


        private void OnNotificationsUpdated(object sender, AtomexNotificationsEventArgs args)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var selectedNotificationIdx = SelectedIndex;
                var incomeNotifications = args.AtomexNotifications
                    .Select(n => new NotificationViewModel()
                    {
                        Id = n.Id,
                        Message = n.Message,
                        Time = n.Time,
                        IsRead = n.IsRead,
                        Type = n.AtomexNotificationType
                    })
                    .OrderByDescending(n => n.Time);

                Notifications = new ObservableCollection<NotificationViewModel>(incomeNotifications);
                SelectedIndex = selectedNotificationIdx;
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

            Notifications = new ObservableCollection<NotificationViewModel>(new List<NotificationViewModel>
            {
                new()
                {
                    Type = AtomexNotificationType.Swap,
                    Message = "0.035 ETH exchanged for 0.13523 BTC",
                    Time = DateTime.Now,
                    IsRead = false
                },
                new()
                {
                    Type = AtomexNotificationType.Outcome,
                    Message = "0.018723 ETH sent to the external address",
                    Time = DateTime.Now,
                    IsRead = true
                },
                new()
                {
                    Type = AtomexNotificationType.Income,
                    Message = "0.035 ETH received from the external address",
                    Time = DateTime.Now,
                    IsRead = false
                },
                new()
                {
                    Type = AtomexNotificationType.Outcome,
                    Message = "0.018723 ETH sent to the external address",
                    Time = DateTime.Now,
                    IsRead = true
                }
            });
        }
#endif
    }

    public class NotificationViewModel : ViewModelBase
    {
        [Reactive] public string Id { get; set; }
        [Reactive] public string Message { get; set; }
        [Reactive] public DateTime Time { get; set; }
        [Reactive] public bool IsRead { get; set; }
        [Reactive] public AtomexNotificationType Type { get; set; }
    }
}