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
    public class TimeEntryModelService : ModelServiceBase<TimeEntry>, ITimeEntryModelService
    {
        public TimeEntryModelService(IAppDataContext db) : base(db)
        {
        }

        public override ILiteCollection<TimeEntry> Collection => Db.TimeEntries;
        public IObservable<ListRetrievalResponse<TimeEntry>> Get(string? WorkItemId, int pageNo, int itemsPerPage)
        {
            var skip = (pageNo - 1) * itemsPerPage;
            var take = itemsPerPage;
            var WorkItemObjId = ModelBase.IdFromString(WorkItemId);
            return GetCount(WorkItemId)
                .Select(count =>
                {
                    var q = Collection.Query();
                    if (WorkItemId != null)
                        q = q.Where(x => x.WorkItemId != null && x.WorkItemId == WorkItemObjId);

                    var list = q
                        .OrderByDescending(x => x.TimeStart)
                        .Skip(skip)
                        .Limit(take)
                        .ToList();
                    return new ListRetrievalResponse<TimeEntry>(count, list);
                });
        }

        public IObservable<long> GetCount(string? WorkItemId)
        {
            var q = Collection.Query();
            var WorkItemObjId = ModelBase.IdFromString(WorkItemId);
            if (WorkItemId != null)
                q = q.Where(x => x.WorkItemId != null && x.WorkItemId == WorkItemObjId);

            return Observable.Start(() => q.LongCount());
        }
    }
}
