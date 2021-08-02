using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public abstract class ListViewModel<TModel, TViewModel> : ViewModelBase where TViewModel : EditableViewModel<TModel> where TModel : ModelBase, new()
    {
        private readonly SourceCache<TViewModel, string> _viewModelsSource;
        private readonly ObservableCollectionExtended<TViewModel> _itemList = new();
        private readonly IObservable<IChangeSet<TViewModel, string>> _viewModelChangeset;
        private readonly ObservableAsPropertyHelper<int> _totalPages;
        private readonly ObservableAsPropertyHelper<long> _totalItems;
        private readonly ObservableAsPropertyHelper<bool> _hasMultiplePages;
        private int _itemsPerPage = 50;
        private int _currentPage = 1;
        private TViewModel? _selectedItem;
        private string? _justAddedId;
        private BehaviorSubject<IComparer<TViewModel>> _sortOrder = new(SortExpressionComparer<TViewModel>.Ascending(x => x.Id));

        private TViewModel? _newItem;

        public ListViewModel(Func<TViewModel> viewModelFactory)
        {
            _viewModelsSource = new(x => x.Id);
            _viewModelChangeset = _viewModelsSource
                .Connect()
                .Sort(_sortOrder)
                .RefCount();

            _viewModelChangeset
                .AutoRefresh()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(_itemList)
                .Subscribe();

            Load = ReactiveCommand.CreateFromObservable(() => DoLoad());
            _totalItems = Load
                .Select(response => response.TotalRecords)
                .ToProperty(this, x => x.TotalItems);

            _totalPages = this.WhenAnyValue(x => x.TotalItems, x => x.ItemsPerPage, (totalItems, itemsPP) =>
                {
                    var pageSizeFract = Convert.ToDecimal(totalItems) / itemsPP;
                    return Convert.ToInt32(Math.Ceiling(pageSizeFract));
                })
                .ToProperty(this, x => x.TotalPages);

            _hasMultiplePages = this.WhenAnyValue(x => x.TotalPages)
                .Select(pageCount => pageCount > 1)
                .ToProperty(this, x => x.HasMultiplePages);

            Load
                .Subscribe(response =>
                {
                    _viewModelsSource.Edit(list =>
                    {
                        list.Clear();
                        list.AddOrUpdate(response.Results.Select(model =>
                        {
                            var vm = viewModelFactory();
                            vm.FromModel(model);
                            return vm;
                        }));
                    });

                    SelectedItem = null;
                    if (!string.IsNullOrWhiteSpace(_justAddedId))
                        SelectedItem = ItemList.FirstOrDefault(x => x.Id == _justAddedId);
                    if (SelectedItem == null)
                        SelectedItem = ItemList.FirstOrDefault();
                });

            CreateNewItem = ReactiveCommand.CreateFromObservable(() =>
            {
                var vm = viewModelFactory();
                return vm.Edit.Execute().Select(_ => vm);
            });

            CreateNewItem
                .Subscribe(newVm => NewItem = newVm);

            //when canceling or saving remove the new item
            CreateNewItem
                .WhereNotNull()
                .Select(newItem => newItem.CancelEdit.Select(_ => newItem))
                .Switch()
                .Subscribe(_ => NewItem = null);

            //reload the items when a new item is saved. If we don't the sorting will be out of wack.
            CreateNewItem
                .WhereNotNull()
                .Select(newItem => newItem.SaveEdits.Do(_ => _justAddedId = newItem.Id))
                .Switch()
                .Do(_ => NewItem = null)
                .InvokeCommand(Load);

            _viewModelChangeset
                .MergeMany(vm => vm.Delete)
                .Where(deleted => deleted)
                .Select(_ => Unit.Default)
                .InvokeCommand(Load);

            ListNotification =
                _viewModelChangeset
                .MergeMany(vm => vm.SaveEdits.Select(_ => vm))
                .Select(vm =>
                {
                    return vm.IsNew
                        ? $"{vm} was successfully created in the database."
                        : $"{vm} was successfully saved";
                })
                .Merge(
                        _viewModelChangeset
                        .MergeMany(vm => vm.Delete.Select(_ => vm))
                        .Select(vm => $"{vm} deleted")
                       )
                .Select(message => new NotificationSuccessViewModel(message));

            var canGotoFirst = this.WhenAnyValue(x => x.CurrentPage).Select(page => page > 1);
            FirstPage = ReactiveCommand.Create(() => { CurrentPage = 1; }, canGotoFirst);

            var canGotoLast = this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages, (page, totalPages) => page < totalPages);
            LastPage = ReactiveCommand.Create(() => { CurrentPage = TotalPages; }, canGotoLast);

            var canGotoNext = this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages, (page, totalPages) => page < totalPages);
            NextPage = ReactiveCommand.Create(() => { CurrentPage++; }, canGotoNext);

            var canGotoPrevious = this.WhenAnyValue(x => x.CurrentPage).Select(page => page > 1);
            PreviousPage = ReactiveCommand.Create(() => { CurrentPage--; }, canGotoPrevious);

            Observable.Merge(FirstPage, LastPage, NextPage, PreviousPage)
                .InvokeCommand(Load);

            this.WhenAnyValue(x => x.SelectedItem)
                .Subscribe(selected =>
                {
                    foreach (var item in ItemList)
                        item.IsSelected = item == selected;
                });

            //_viewModelChangeset
            //    .ToCollection()
            //    .Select(list => list.Select(vm => vm.Delete).Merge())
            //    .Switch()
            //    .Select(_ => Unit.Default)
            //    .InvokeCommand(Load);
        }

        public ObservableCollectionExtended<TViewModel> ItemList => _itemList;
        public ReactiveCommand<Unit, ListRetrievalResponse<TModel>> Load { get; }
        public ReactiveCommand<Unit, Unit> FirstPage { get; }
        public ReactiveCommand<Unit, Unit> LastPage { get; }
        public ReactiveCommand<Unit, Unit> NextPage { get; }
        public ReactiveCommand<Unit, Unit> PreviousPage { get; }
        public int ItemsPerPage { get => _itemsPerPage; set => this.RaiseAndSetIfChanged(ref _itemsPerPage, value); }
        public int CurrentPage { get => _currentPage; set => this.RaiseAndSetIfChanged(ref _currentPage, value); }
        public int TotalPages => TotalPages1.Value;
        public long TotalItems => _totalItems.Value;
        public bool HasMultiplePages => _hasMultiplePages.Value;
        public TViewModel? SelectedItem { get => _selectedItem; set => this.RaiseAndSetIfChanged(ref _selectedItem, value); }
        public TViewModel? NewItem { get => _newItem; set => this.RaiseAndSetIfChanged(ref _newItem, value); }
        public ReactiveCommand<Unit, TViewModel> CreateNewItem { get; }
        public ObservableAsPropertyHelper<int> TotalPages1 => _totalPages;
        public IObservable<NotificationViewModel> ListNotification { get; }

        protected void ChangeSortOrder(IComparer<TViewModel> comparer)
        {
            _sortOrder.OnNext(comparer);
        }

        protected abstract IObservable<ListRetrievalResponse<TModel>> DoLoad();
    }
}