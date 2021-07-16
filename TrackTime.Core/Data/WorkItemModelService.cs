using LiteDB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using TrackTime.Models;

namespace TrackTime.Data
{
    public class WorkItemModelService : ModelServiceBase<WorkItem>, IWorkItemModelService
    {
        public WorkItemModelService(IAppDataContext db) : base(db)
        {
        }

        public override ILiteCollection<WorkItem> Collection => Db.WorkItems;

        public IObservable<ListRetrievalResponse<WorkItem>> Get(string? customerId, bool includeCompleted, int pageNo, int itemsPerPage)
        {
            var skip = (pageNo - 1) * itemsPerPage;
            var take = itemsPerPage;
            var customerObjId = ModelBase.IdFromString(customerId);
            return GetCount(customerId, includeCompleted)
                .Select(count =>
                {
                    var q = Collection.Query();
                    if (customerId != null)
                        q = q.Where(x => x.CustomerId != null && x.CustomerId == customerObjId);
                    if (!includeCompleted)
                        q = q.Where(x => x.IsCompleted);

                    var list = q
                        .OrderBy(x => x.DueDate)
                        .Skip(skip)
                        .Limit(take)
                        .ToList();
                    return new ListRetrievalResponse<WorkItem>(count, list);
                });
        }

        public IObservable<long> GetCount(string? customerId, bool includeCompleted)
        {
            var q = Collection.Query();
            var customerObjId = ModelBase.IdFromString(customerId);
            if (customerId != null)
                q = q.Where(x => x.CustomerId != null && x.CustomerId == customerObjId);

            if (!includeCompleted)
                q = q.Where(x => x.IsCompleted);

            return Observable.Start(() => q.LongCount());
        }
    }
}
