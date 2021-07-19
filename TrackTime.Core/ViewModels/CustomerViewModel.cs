using ReactiveUI;
using ReactiveUI.Validation.Extensions;

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public class CustomerViewModel : EditableViewModel<Customer>, IActivatableViewModel
    {
        //private readonly ObservableAsPropertyHelper<bool> _listShowingInactive;
        private readonly BehaviorSubject<bool> _listShowingInactiveSubject = new(false);
        private string _name = string.Empty;
        private string _phone = string.Empty;
        private string _email = string.Empty;
        private bool _isActive = true;
        private string _comments = string.Empty;

        private readonly ObservableAsPropertyHelper<bool> _showActiveIndicator;
        private readonly ObservableAsPropertyHelper<bool> _showInactiveIndicator;

        public CustomerViewModel(Func<ICustomerModelService> customerModelServiceFactory, IDialogService dialogService) : base(customerModelServiceFactory, dialogService)
        {
            //we only want to show the active and inactive indicators if we are not filtering out inactive items
            //_listShowingInactive = _listShowingInactiveSubject.ToProperty(this, x => x.ListShowingInactive);
            _showActiveIndicator = Observable.CombineLatest(
                _listShowingInactiveSubject,
                this.WhenAnyValue(x => x.IsActive),
                (includingInactive, isActive) => includingInactive && isActive
                )
            .ToProperty(this, x => x.ShowActiveIndicator);

            _showInactiveIndicator = Observable.CombineLatest(
                _listShowingInactiveSubject,
                this.WhenAnyValue(x => x.IsActive),
                (includingInactive, isActive) => includingInactive && !isActive
                )
            .ToProperty(this, x => x.ShowInactiveIndicator);

            this.ValidationRule(x => x.Name, name => !string.IsNullOrWhiteSpace(name), "Customer must have a name!");

            this.WhenActivated(d =>
            {
                MessageBus.Current.ListenIncludeLatest<CustomerFilterMessage>()
                    .WhereNotNull()
                    .Subscribe(filter => _listShowingInactiveSubject.OnNext(filter.IncludingInActive))
                    .DisposeWith(d);
            });
        }

        public string Name { get => _name; set => this.RaiseAndSetIfChanged(ref _name, value); }
        public string Phone { get => _phone; set => this.RaiseAndSetIfChanged(ref _phone, value); }
        public string Email { get => _email; set => this.RaiseAndSetIfChanged(ref _email, value); }
        public bool IsActive { get => _isActive; set => this.RaiseAndSetIfChanged(ref _isActive, value); }
        public string Notes { get => _comments; set => this.RaiseAndSetIfChanged(ref _comments, value); }
        //public bool ListShowingInactive => _listShowingInactive.Value;
        public bool ShowActiveIndicator => _showActiveIndicator.Value;
        public bool ShowInactiveIndicator => _showInactiveIndicator.Value;

        public ViewModelActivator Activator { get; } = new();

        protected override string DeleteConfirmationPrompt() => $"Are you sure you wish to delete customer {Name}?";
    }
}