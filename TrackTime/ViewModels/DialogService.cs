using Splat;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using TrackTime.Views;

namespace TrackTime.ViewModels
{
    public class DialogService : IDialogService
    {
        private readonly IDialogHost _dialogHost;

        public DialogService(IDialogHost dialogHost)
        {
            _dialogHost = dialogHost;
        }

        public IObservable<bool> ConfirmationYesNo(string caption, string prompt)
        {
            var vm = Locator.Current.GetService<ConfirmationViewModel>() ?? throw new InvalidOperationException($"{nameof(ConfirmationViewModel)} is not a registered service");
            vm.Caption = caption;
            vm.Prompt = prompt;
            return _dialogHost.ShowDialog(vm, vm.OnResult).Select(dialogResult => !dialogResult.Cancelled && dialogResult.Result);
        }

        public IObservable<Unit> MessageBox(string caption, string message)
        {
            var vm = Locator.Current.GetService<MessageBoxViewModel>() ?? throw new InvalidOperationException($"{nameof(MessageBoxViewModel)} is not a registered service");
            vm.Caption = caption;
            vm.Message = message;
            return _dialogHost.ShowDialog(vm, vm.OnDismissed).Select(_ => Unit.Default);
        }
    }


}
