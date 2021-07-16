using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LiteDB;

using TrackTime.Models;

namespace TrackTime.Data
{
    public interface IWorkItemModelService : IModelServiceBase<WorkItem>
    {
        IObservable<ListRetrievalResponse<WorkItem>> Get(string? customerId, bool includeCompleted, int pageNo, int itemsPerPage);
        IObservable<long> GetCount(string? customerId, bool includeCompleted);
    }
}
