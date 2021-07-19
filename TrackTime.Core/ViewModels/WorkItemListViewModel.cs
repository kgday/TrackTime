using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Reactive;
using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public class WorkItemListViewModel : ListViewModel<WorkItem, WorkItemViewModel>
    {
        private readonly Func<IWorkItemModelService> _workItemModelServiceFactory;
        private bool _includeCompleted;
        private string? _forCustomerId;

        public WorkItemListViewModel(Func<WorkItemViewModel> TViewModelFactory, Func<IWorkItemModelService> workItemModelServiceFactory
                            ) : base(TViewModelFactory)
        {
            _workItemModelServiceFactory = workItemModelServiceFactory;
 
            
        }

        public bool IncludeCompleted { get => _includeCompleted; set => this.RaiseAndSetIfChanged(ref _includeCompleted, value); }
        public string? ForCustomerId { get => _forCustomerId; set => this.RaiseAndSetIfChanged(ref _forCustomerId, value); }

        protected override IObservable<ListRetrievalResponse<WorkItem>> DoLoad()
        {
            var modelService = _workItemModelServiceFactory();
            return modelService.Get(ForCustomerId, IncludeCompleted, CurrentPage, ItemsPerPage);
        }


    }
}