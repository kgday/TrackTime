using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;

using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public class CustomerListViewModel : ListViewModel<Customer, CustomerViewModel>, IActivatableViewModel
    {
        private readonly Func<ICustomerModelService> _customerModelServiceFactory;

        public CustomerListViewModel(Func<CustomerViewModel> viewModelFactory, Func<ICustomerModelService> customerModelServiceFactory) : base(viewModelFactory)
        {
            _customerModelServiceFactory = customerModelServiceFactory;
            this.WhenActivated(d =>
            {
                MessageBus.Current.RegisterMessageSource(
                    this.WhenAnyValue(x => x.IncludeInactive)
                    .Select(includeInActive => new CustomerFilterMessage(includeInActive))
                    )
                .DisposeWith(d);

                this.WhenAnyValue(x => x.IncludeInactive)
                    .Skip(1)
                    .InvokeCommand(Load)
                    .DisposeWith(d);
            });

        }

        protected override IObservable<ListRetrievalResponse<Customer>> DoLoad()
        {
            var modelService = _customerModelServiceFactory();
            return modelService.Get(IncludeInactive, CurrentPage, ItemsPerPage);
        }

        private bool _includeInactive;
        public bool IncludeInactive { get => _includeInactive; set => this.RaiseAndSetIfChanged(ref _includeInactive, value); }
        public ViewModelActivator Activator { get; } = new();
    }
}
