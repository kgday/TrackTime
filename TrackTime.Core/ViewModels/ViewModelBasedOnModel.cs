using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Reactive;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public class ViewModelBasedOnModel<TModel> : ViewModelBase where TModel : ModelBase
    {
        private string? _id;

        public string? Id { get => _id; set => this.RaiseAndSetIfChanged(ref _id, value); }

        //Assigns same name properties
        public virtual void FromModel(TModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var srcProps = model.GetType().GetProperties();
            foreach (var srcProp in srcProps)
            {
                var destProp = GetType().GetProperty(srcProp.Name);
                if (destProp != null && srcProp.PropertyType == destProp.PropertyType)
                    destProp.SetValue(model, srcProp.GetValue(this));
            }

            Id = model.Id?.ToString();
        }

        public virtual void ToModel(TModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var destProps = model.GetType().GetProperties();
            foreach (var destProp in destProps)
            {
                var srcProp = GetType().GetProperty(destProp.Name);
                if (srcProp != null && srcProp.PropertyType == destProp.PropertyType)
                    destProp.SetValue(model, srcProp.GetValue(this));
            }

            model.Id = ModelBase.IdFromString(Id);
        }
    }
}