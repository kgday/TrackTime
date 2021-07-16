using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TrackTime.Models;

namespace TrackTime.Data
{
    public interface ITimeEntryModelService : IModelServiceBase<TimeEntry>
    {
        IObservable<ListRetrievalResponse<TimeEntry>> Get(string? WorkItemId, int pageNo, int itemsPerPage);
        IObservable<long> GetCount(string? WorkItemId);
    }
}
