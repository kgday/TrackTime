using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public class WorkItemViewModel : EditableViewModel<WorkItem>
    {
        private string _title = string.Empty;
        private string? _description;
        private bool _isBillable;
        private bool _isCompleted;
        private bool _isFixedPrice;
        private DateTime _dateCreated = DateTime.Now;
        private DateTime _dueDate = DateTime.Now + TimeSpan.FromDays(7);
        private bool _beenBilled;
        private string? _body;
        private string? _customerId;

        public WorkItemViewModel(Func<IWorkItemModelService> workItemModelServiceFactory, IDialogService dialogService) : base(workItemModelServiceFactory, dialogService)
        {
        }

        public string Title { get => _title; set => this.RaiseAndSetIfChanged(ref _title, value); }
        public string? Description { get => _description; set => this.RaiseAndSetIfChanged(ref _description, value); }
        public bool IsBillable { get => _isBillable; set => this.RaiseAndSetIfChanged(ref _isBillable, value); }
        public bool IsCompleted { get => _isCompleted; set => this.RaiseAndSetIfChanged(ref _isCompleted, value); }
        public bool IsFixedPrice { get => _isFixedPrice; set => this.RaiseAndSetIfChanged(ref _isFixedPrice, value); }
        public DateTime DateCreated { get => _dateCreated; set => this.RaiseAndSetIfChanged(ref _dateCreated, value); }
        public DateTime DueDate { get => _dueDate; set => this.RaiseAndSetIfChanged(ref _dueDate, value); }
        public bool BeenBilled { get => _beenBilled; set => this.RaiseAndSetIfChanged(ref _beenBilled, value); }
        public string? Body { get => _body; set => this.RaiseAndSetIfChanged(ref _body, value); }
        public string? CustomerId { get => _customerId; set => this.RaiseAndSetIfChanged(ref _customerId, value); }

        protected override string DeleteConfirmationPrompt() => $"Are you sure you wish to delete work item {Title}?";
    }
}
