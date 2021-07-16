using DynamicData;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using TrackTime.Data;
using TrackTime.Models;

namespace TrackTime.ViewModels
{
    public abstract class ListViewModel<TModel, TViewModel> : ViewModelBase where TViewModel : ViewModelBasedOnModel<TModel> where TModel : ModelBase
    {
        private readonly SourceList<TViewModel> _viewModelsSource;
        private readonly ReadOnlyObservableCollection<TViewModel> _itemList;
        private readonly IObservable<IChangeSet<TViewModel>> _viewModelChangeset;
        private int _itemsPerPage = 50;
        private int _currentPage = 1;

        private ObservableAsPropertyHelper<int> _totalPages;
        private ObservableAsPropertyHelper<long> _totalItems;
        private ObservableAsPropertyHelper<bool> _hasMultiplePages;
        private TViewModel? _selectedItem;

        public ListViewModel(Func<TViewModel> viewModelFactory)
        {
            _viewModelsSource = new();
            _viewModelChangeset = _viewModelsSource
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .RefCount();

            _viewModelChangeset
                .Bind(out _itemList);

            _totalPages = this.WhenAnyValue(x => x.TotalItems, x => x.ItemsPerPage, (totalItems, itemsPP) =>
                {
                    var pageSizeFract = Convert.ToDecimal(totalItems) / itemsPP;
                    return Convert.ToInt32(Math.Ceiling(pageSizeFract));
                })
                .ToProperty(this, x => x.TotalPages);

            _hasMultiplePages = this.WhenAnyValue(x => x.TotalPages)
                .Select(pageCount => pageCount > 1)
                .ToProperty(this, x => x.HasMultiplePages);

            Load = ReactiveCommand.CreateFromObservable(() => DoLoad());
            _totalItems = Load
                .Select(response => response.TotalRecords)
                .ToProperty(this, x => x.TotalItems);
            Load
                .Subscribe(response =>
                {
                    _viewModelsSource.Edit(list =>
                    {
                        list.Clear();
                        list.Add(response.Results.Select(model =>
                        {
                            var vm = viewModelFactory();
                            vm.FromModel(model);
                            return vm;
                        }));
                    });
                    SelectedItem = ItemList.FirstOrDefault();
                });

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
        }

        public ReadOnlyObservableCollection<TViewModel> ItemList => _itemList;
        public ReactiveCommand<Unit, ListRetrievalResponse<TModel>> Load { get; }
        public ReactiveCommand<Unit, Unit> FirstPage { get; }
        public ReactiveCommand<Unit, Unit> LastPage { get; }
        public ReactiveCommand<Unit, Unit> NextPage { get; }
        public ReactiveCommand<Unit, Unit> PreviousPage { get; }
        public int ItemsPerPage { get => _itemsPerPage; set => this.RaiseAndSetIfChanged(ref _itemsPerPage, value); }
        public int CurrentPage { get => _currentPage; set => this.RaiseAndSetIfChanged(ref _currentPage, value); }
        public int TotalPages => _totalPages.Value;
        public long TotalItems => _totalItems.Value;
        public bool HasMultiplePages => _hasMultiplePages.Value;
        public TViewModel? SelectedItem { get => _selectedItem; set => this.RaiseAndSetIfChanged(ref _selectedItem, value); }

        protected abstract IObservable<ListRetrievalResponse<TModel>> DoLoad();
    }
}