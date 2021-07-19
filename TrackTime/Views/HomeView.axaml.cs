using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;

using TrackTime.ViewModels;

namespace TrackTime.Views
{
    //public class HomeViewBase : ReactiveUserControl<HomeViewModel> { }
    public partial class HomeView : ReactiveUserControl<HomeViewModel>
    {
        public HomeView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                .Subscribe(viewModel =>
                {
                    this.OneWayBind(viewModel, vm => vm.CustomerList, v => v.CustomerListHost.ViewModel).DisposeWith(d);
                });
            });
        }


        //using XamlNameReferenceGenerator which also generates InitializeComponent
        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        // }
    }
}
