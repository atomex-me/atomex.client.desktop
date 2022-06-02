using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Atomex.Blockchain;
using Atomex.Common;
using Atomex.Services;
using Atomex.Wallet;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Serilog;

namespace Atomex.Client.Desktop.Services
{
    public class AtomexNotificationsEventArgs : EventArgs
    {
        public IEnumerable<AtomexNotification> AtomexNotifications { get; }

        public AtomexNotificationsEventArgs(IEnumerable<AtomexNotification> atomexNotifications)
        {
            AtomexNotifications = atomexNotifications;
        }
    }

    public class NotificationsService
    {
        public event EventHandler<AtomexNotificationsEventArgs> NotificationsUpdated;
        private const int DefaultNotificationDurationSeconds = 5;
        private static WindowNotificationManager Manager { get; set; }
        private static IAtomexApp App { get; set; }
        public ObservableCollection<AtomexNotification> AtomexNotifications { get; set; }

        public NotificationsService(IAtomexApp atomexApp, WindowNotificationManager manager)
        {
            Manager = manager;
            App = atomexApp;

            // App.AtomexClientChanged += OnAtomexClientChangedEventHandler!;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var atomexClient = args?.AtomexClient;

            if (atomexClient?.Account == null)
                return;

            // App.Account.UserData.Notifications ??= new List<AtomexNotification>();
            // NotificationsUpdated?.Invoke(this, new AtomexNotificationsEventArgs(App.Account.UserData.Notifications));

            // todo: remove after Notifications layout will done;
            // App.Account.BalanceUpdated += OnBalanceUpdatedEventHandler!;
            // App.Account.UnconfirmedTransactionAdded += OnUnconfirmedTransactionAdded!;
        }

        private void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                Show("Information", $"{args.Currency} balance updated");
            }
            catch (Exception e)
            {
                Log.Error(e, "Notifications Service balance updated event handler error");
            }
        }

        private void OnUnconfirmedTransactionAdded(object sender, TransactionEventArgs args)
        {
            try
            {
                Show("Information", $"New unconfirmed transaction on {args.Transaction.Currency}");
            }
            catch (Exception e)
            {
                Log.Error(e, "Notifications Service unconfirmed transaction added event handler error");
            }
        }

        private static void Show(string title, string message, NotificationType type = NotificationType.Information)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                // var atomexNotification = new AtomexNotification
                // {
                //     Id = Guid.NewGuid().ToString("N"),
                //     Message = message,
                //     IsRead = false,
                //     Time = DateTime.Now,
                //     AtomexNotificationType = AtomexNotificationType.Info
                // };

                // App.Account.UserData.Notifications ??= new List<AtomexNotification>();
                // App.Account.UserData.Notifications.Add(atomexNotification);

                // Save();

                Manager.Show(new Notification(
                    title,
                    message,
                    type,
                    TimeSpan.FromSeconds(DefaultNotificationDurationSeconds)
                ));
            });
        }

        private void Save()
        {
            NotificationsUpdated?.Invoke(this, new AtomexNotificationsEventArgs(App.Account.UserData.Notifications));
            App.Account.UserData.SaveToFile(App.Account.SettingsFilePath);
        }

        public void ReadAll()
        {
            if (App.Account.UserData.Notifications == null) return;

            App.Account.UserData.Notifications = App.Account.UserData.Notifications
                .ForEachDo(notification => notification.IsRead = true)
                .ToList();

            Save();
        }

        public void ReadById(string id)
        {
            if (App.Account.UserData.Notifications == null) return;
            var changedNotificationIndex = App.Account.UserData.Notifications
                .FindIndex(n => n.Id == id);
            
            App.Account.UserData.Notifications[changedNotificationIndex].IsRead = true;

            Save();
        }
    }
}