using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ReactiveUI;

using TrackTime.ViewModels;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace TrackTime.Views
{
    public class CustomerListViewBase : ReactiveUserControl<CustomerListViewModel> { }
    public partial class CustomerListView : CustomerListViewBase
    {
        public CustomerListView()
        {
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    this.OneWayBind(viewModel, vm => vm.ItemList, v => v.CustomerList.Items).DisposeWith(d);
                    this.Bind(ViewModel, vm => vm.SelectedItem, v => v.CustomerList.SelectedItem).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.CreateNewItem, v => v.CreateButton).DisposeWith(d);
                    //viewModel.CreateNewItem.BindTo(this, v=>v.NewItemViewHost.ViewModel).DisposeWith(d);
                })
                .DisposeWith(d);
            });
            InitializeComponent();
        }

        //using XamlNameReferenceGenerator which also generates InitializeComponent
        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        // }
    }
}
