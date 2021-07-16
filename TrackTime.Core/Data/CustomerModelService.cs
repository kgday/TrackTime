using LiteDB;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

using TrackTime.Models;

namespace TrackTime.Data
{
    public class CustomerModelService : ModelServiceBase<Customer>, ICustomerModelService
    {
        public CustomerModelService(IAppDataContext db) : base(db)
        {
        }

        public override ILiteCollection<Customer> Collection => Db.Customers;
        public IObservable<ListRetrievalResponse<Customer>> Get(bool includeInactive, int pageNo, int itemsPerPage)
        {
            var skip = (pageNo - 1) * itemsPerPage;
            var take = itemsPerPage;
            return GetCount(includeInactive)
                .Select(count =>
                {
                    var q = Collection.Query();
                    if (!includeInactive)
                        q = q.Where(x => x.IsActive);
                    var list = q
                        .OrderBy(x => x.Name)
                        .Skip(skip)
                        .Limit(take)
                        .ToList();

                    return new ListRetrievalResponse<Customer>(count, list);
                });
        }

        public IObservable<long> GetCount(bool includeInactive)
        {
            var q = Collection.Query();
            if (!includeInactive)
                q = q.Where(x => x.IsActive);

            return Observable.Start(() => q.LongCount());
        }
    }
}
