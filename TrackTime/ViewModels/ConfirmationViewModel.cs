using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace TrackTime.ViewModels
{
    public class ConfirmationViewModel : DialogViewModel
    {
        private Subject<bool> _onResult = new();
        private string _prompt = string.Empty;

        public ConfirmationViewModel()
        {
            Yes = ReactiveCommand.Create(() => { _onResult.OnNext(true); _onResult.OnCompleted(); });
            No = ReactiveCommand.Create(() => { _onResult.OnNext(false); _onResult.OnCompleted(); });
        }

        public string Prompt { get => _prompt; set => this.RaiseAndSetIfChanged(ref _prompt, value); }

        public IObservable<bool> OnResult => _onResult.AsObservable();
        public ReactiveCommand<Unit, Unit> Yes { get; }
        public ReactiveCommand<Unit, Unit> No { get; }
    }
}