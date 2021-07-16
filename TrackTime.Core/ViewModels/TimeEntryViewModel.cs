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
    public class TimeEntryViewModel : EditableViewModel<TimeEntry>
    {
        private string _description = string.Empty;
        private DateTime _timeStart = DateTime.Now;
        private DateTime _timeEnd;
        private string? _notes;
        private bool _beenBilled;
        private string? _workItemId;

        public TimeEntryViewModel(Func<ITimeEntryModelService> timeEntryModelServiceFactory, IDialogService dialogService) : base(timeEntryModelServiceFactory, dialogService)
        {
        }

        public string Description { get => _description; set => this.RaiseAndSetIfChanged(ref _description, value); }
        public DateTime TimeStart { get => _timeStart; set => this.RaiseAndSetIfChanged(ref _timeStart, value); }
        public DateTime TimeEnd { get => _timeEnd; set => this.RaiseAndSetIfChanged(ref _timeEnd, value); }
        public string? Notes { get => _notes; set => this.RaiseAndSetIfChanged(ref _notes, value); }
        public bool BeenBilled { get => _beenBilled; set => this.RaiseAndSetIfChanged(ref _beenBilled, value); }

        public string? WorkItemId { get => _workItemId; set => this.RaiseAndSetIfChanged(ref _workItemId, value); }

        public override void FromModel(TimeEntry model)
        {
            base.FromModel(model);
            WorkItemId = model.WorkItemId?.ToString();
        }

        public override void ToModel(TimeEntry model)
        {
            base.ToModel(model);
            model.WorkItemId = ModelBase.IdFromString(WorkItemId);
        }

        protected override string DeleteConfirmationPrompt() => $"Are you sure you want to delete time entry {Description} started at {TimeStart}?";
    }
}
