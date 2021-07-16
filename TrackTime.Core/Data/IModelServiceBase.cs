using LiteDB;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using TrackTime.Models;

namespace TrackTime.Data
{
    public class ListRetrievalResponse<T>
    {
        public ListRetrievalResponse(long totalRecords, IEnumerable<T> list)
        {
            TotalRecords = totalRecords;
            Results = list;
        }

        public long TotalRecords { get; set; }
        public IEnumerable<T> Results { get; set; }
    }

    public interface IModelServiceBase<T> where T : ModelBase
    {
        IObservable<T> Add(T model);
        IObservable<bool> Delete(string id);
        IObservable<bool> Update(T model);
        IObservable<T?> GetOne(string id);
    }
}
