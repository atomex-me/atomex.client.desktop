using System;
using System.Threading.Tasks;

namespace Atomex.Client.Desktop.Controls
{
    public interface IDialogViewer
    {
        void ShowDialog(
            int dialogId,
            object dataContext,
            Action loaded = null,
            Action canceled = null,
            int defaultPageId = 0);
        void HideDialog(int dialogId);
        void HideAllDialogs();

        void PushPage(int dialogId, int pageId, object dataContext = null, Action closeAction = null);
        void PopPage(int dialogId);
        void Back(int dialogId);

        void ShowMessage(string title, string message);
        
        Task<object> ShowMessageAsync(string title, string message);
    }
}