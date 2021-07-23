using LiteDB;


using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

using TrackTime.Models;

namespace TrackTime.Data
{
    public abstract class ModelServiceBase<T> : IModelServiceBase<T> where T : ModelBase
    {
        public ModelServiceBase(IAppDataContext db)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        protected IAppDataContext Db { get; }

        public IObservable<T> Add(T model) => Observable.Start(() =>
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            model.Id = ModelBase.NewId();
            Collection.Insert(model);
            return model;
        });

        public IObservable<bool> Update(T model) => Observable.Start(() => Collection.Update(model));

        public IObservable<bool> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));
            var objId = new ObjectId(id);
            return Observable.Start(() => Collection.Delete(id));
        }


        public IObservable<T?> GetOne(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));
            var objId = new ObjectId(id);
            return Observable.Start(() => Collection.Query().Where(model => model.Id == objId).SingleOrDefault());
        }

        public abstract ILiteCollection<T> Collection { get; }
    }
}
