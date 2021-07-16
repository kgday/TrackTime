using System;
using System.Collections.Generic;
using System.Text;

using TrackTime.Models;

namespace TrackTime.Data
{
    public interface ICustomerModelService : IModelServiceBase<Customer>
    {
        IObservable<ListRetrievalResponse<Customer>> Get(bool includeInactive, int pageNo, int itemsPerPage);
        IObservable<long> GetCount(bool includeInactive);
    }
}
