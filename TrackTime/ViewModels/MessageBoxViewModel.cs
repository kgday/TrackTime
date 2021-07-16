using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace TrackTime.ViewModels
{
    public class MessageBoxViewModel : DialogViewModel
    {
        private  string _message = string.Empty;
        private Subject<Unit> _onDismmissed = new();

        public MessageBoxViewModel()
        {
            Dismiss = ReactiveCommand.Create(() => { _onDismmissed.OnNext(Unit.Default); _onDismmissed.OnCompleted(); });
        }

        public string Message { get => _message; set => this.RaiseAndSetIfChanged(ref _message, value); }

        public IObservable<Unit> OnDismissed => _onDismmissed.AsObservable();

        public ReactiveCommand<Unit, Unit> Dismiss { get; }

    }
}
