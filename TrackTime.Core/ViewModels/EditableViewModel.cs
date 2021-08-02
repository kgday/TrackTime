using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;

using System;
using System.Reactive;
using System.Reactive.Linq;

using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public abstract class EditableViewModel<TModel> : ViewModelBasedOnModel<TModel>, IValidatableViewModel where TModel : ModelBase, new()
    {
        private readonly Func<IModelServiceBase<TModel>> _modelServiceFactory;
        private bool _isEditing;

        private bool _isNew = true;

        private bool _isSelected;

        public EditableViewModel(Func<IModelServiceBase<TModel>> modelServiceFactory, IDialogService dialogService)
        {
            _modelServiceFactory = modelServiceFactory;

            var canEdit = this.WhenAnyValue(x => x.IsEditing).Select(editing => !editing);
            Edit = ReactiveCommand.Create(() => { IsEditing = true; }, canEdit);

            var canCancel = this.WhenAnyValue(x => x.IsEditing);
            CancelEdit = ReactiveCommand.CreateFromObservable(() =>
            {
                if (!string.IsNullOrWhiteSpace(Id))
                {
                    var modelService = _modelServiceFactory();
                    return modelService.GetOne(Id);
                }
                return Observable.Return<TModel?>(default);
            }, canCancel);
            CancelEdit
                .Subscribe(model =>
                {
                    if (!IsNew && model != null)
                        FromModel(model);
                    IsEditing = false;
                });

            var canSave = Observable.CombineLatest(
                this.WhenAnyValue(x => x.IsEditing),
                this.IsValid(),
                (editing, isValid) => editing && isValid);
            SaveEdits = ReactiveCommand.CreateFromObservable(() =>
            {
                var model = new TModel();
                ToModel(model);
                var modelService = _modelServiceFactory();

                if (string.IsNullOrWhiteSpace(Id)) //is adding a new one
                    return modelService.Add(model).Do(model => FromModel(model)).Select(_ => Unit.Default);
                else
                    return modelService.Update(model).Select(_ => Unit.Default);
            }, canSave);
            SaveEdits
                .Subscribe(_ => IsEditing = false);

            var canDelete = this.WhenAnyValue(x => x.IsEditing).Select(editing => !editing);
            Delete = ReactiveCommand.CreateFromObservable(() =>
            {
                return dialogService.ConfirmationYesNo("Delete Confirmation", DeleteConfirmationPrompt())
                    .Select(yes =>
                    {
                        if (yes && !string.IsNullOrWhiteSpace(Id))
                        {
                            var modelService = _modelServiceFactory();
                            return modelService.Delete(Id);
                        }
                        return Observable.Return(false);
                    })
                    .Switch();
            }, canDelete);
        }

        public bool IsEditing { get => _isEditing; set => this.RaiseAndSetIfChanged(ref _isEditing, value); }
        public bool IsNew { get => _isNew; set => this.RaiseAndSetIfChanged(ref _isNew, value); }
        public bool IsSelected { get => _isSelected; set => this.RaiseAndSetIfChanged(ref _isSelected, value); }

        public ReactiveCommand<Unit, Unit> Edit { get; }
        public ReactiveCommand<Unit, TModel?> CancelEdit { get; } //return a fresh model from the model service
        public ReactiveCommand<Unit, Unit> SaveEdits { get; }
        public ReactiveCommand<Unit, bool> Delete { get; }
        public ValidationContext ValidationContext { get; } = new();

        public override void FromModel(TModel model)
        {
            base.FromModel(model);
            IsNew = false;
        }

        protected abstract string DeleteConfirmationPrompt();
    }
}