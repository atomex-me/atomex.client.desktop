using System;
using System.Windows.Input;

using ReactiveUI;
using Serilog;

using Atomex.Client.Desktop.Properties;

namespace Atomex.Client.Desktop.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        private readonly Action _backAction;
        private readonly Action _nextAction;

        public string Title { get; }
        public string Text { get; }
        public string BaseUrl { get; }
        public string Id { get; }
        public string NextText { get; }
        public bool IsBackVisible { get; }
        public bool IsLinkVisible { get; }
        public bool IsNextVisible { get; }


        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= (_backCommand = ReactiveCommand.Create(() => { _backAction(); }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= (_nextCommand = ReactiveCommand.Create(() => { _nextAction(); }));

        public MessageViewModel()
        {
        }

        public MessageViewModel(
            string title,
            string text,
            bool isBackVisible,
            string nextTitle,
            Action backAction,
            Action nextAction)
        {
            Title = title;
            Text = text;

            NextText = nextTitle;

            IsBackVisible = isBackVisible;
            IsNextVisible = !string.IsNullOrEmpty(NextText);

            _backAction = backAction;
            _nextAction = nextAction;
        }

        public MessageViewModel(
            string title,
            string text,
            string baseUrl,
            string id,
            bool isBackVisible,
            string nextTitle,
            Action backAction,
            Action nextAction)
        {
            Title = title;
            Text = text;
            
            NextText = nextTitle;
            BaseUrl = baseUrl;
            Id = id;

            IsLinkVisible = !string.IsNullOrEmpty(BaseUrl) && !string.IsNullOrEmpty(Id);
            IsBackVisible = isBackVisible;
            IsNextVisible = !string.IsNullOrEmpty(NextText);

            _backAction = backAction;
            _nextAction = nextAction;
        }

        public static MessageViewModel Error(string text, Action backAction) =>
            new MessageViewModel(
                title: Resources.SvError,
                text: text,
                isBackVisible: true,
                nextTitle: null,
                backAction: backAction,
                nextAction: null);

        public static MessageViewModel Success(string text, Action nextAction) =>
            new MessageViewModel(
                title: Resources.SvSuccess,
                text: text,
                isBackVisible: false,
                nextTitle: Resources.SvOk,
                backAction: null,
                nextAction: nextAction);
        
        public static MessageViewModel Success(string title, string text, Action nextAction) =>
            new MessageViewModel(
                title: title,
                text: text,
                isBackVisible: false,
                nextTitle: Resources.SvOk,
                backAction: null,
                nextAction: nextAction);

        public static MessageViewModel Message(string title, string text, Action backAction) =>
            new MessageViewModel(
                title: title,
                text: text,
                isBackVisible: true,
                nextTitle: null,
                backAction: backAction,
                nextAction: null);
        
        public static MessageViewModel Message(string title, string text, string nextTitle, Action backAction, Action nextAction) =>
            new MessageViewModel(
                title: title,
                text: text,
                isBackVisible: true,
                nextTitle: nextTitle,
                backAction: backAction,
                nextAction: nextAction);

        public static MessageViewModel Success(string text, string baseUrl, string id, Action nextAction) =>
            new MessageViewModel(
                title: Resources.SvSuccess,
                text: text,
                baseUrl: baseUrl,
                id: id,
                isBackVisible: false,
                nextTitle: Resources.SvOk,
                backAction: null,
                nextAction: nextAction);

        private ICommand _openTxInExplorerCommand;

        public ICommand OpenTxInExplorerCommand => _openTxInExplorerCommand ??= (_openTxInExplorerCommand =
            ReactiveCommand.Create<string>((id) =>
            {
                if (Uri.TryCreate($"{BaseUrl}{Id}", UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
                else
                    Log.Error("Invalid uri for transaction explorer");
            }));

        private ICommand _copyCommand;

        public ICommand CopyCommand => _copyCommand ??= (_copyCommand = ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                App.Clipboard.SetTextAsync(s);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        }));
    }
}