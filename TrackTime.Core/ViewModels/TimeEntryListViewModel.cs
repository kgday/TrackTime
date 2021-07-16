using ReactiveUI;

using System;
using System.Collections.Generic;

using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public class TimeEntryListViewModel : ListViewModel<TimeEntry, TimeEntryViewModel>
    {
        private readonly Func<ITimeEntryModelService> _timeEntryModelServiceFactory;
        private string? _forWorkItemId;

        public TimeEntryListViewModel(Func<TimeEntryViewModel> TViewModelFactory, Func<ITimeEntryModelService> timeEntryModelServiceFactory) : base(TViewModelFactory)
        {
            _timeEntryModelServiceFactory = timeEntryModelServiceFactory;
        }

        public string? ForWorkItemId { get => _forWorkItemId; set => this.RaiseAndSetIfChanged(ref _forWorkItemId, value); }

        protected override IObservable<ListRetrievalResponse<TimeEntry>> DoLoad()
        {
            var modelService = _timeEntryModelServiceFactory();
            return modelService.Get(ForWorkItemId, CurrentPage, ItemsPerPage);
        }
    }
}