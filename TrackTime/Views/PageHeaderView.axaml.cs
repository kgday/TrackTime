using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using System.Reactive.Disposables;
using ReactiveUI;
using TrackTime.ViewModels;
using System.Reactive.Linq;
using System.Reactive;
using System.Linq;
using System;
using Avalonia.Media;

namespace TrackTime.Views
{
    //public class PageHeaderViewBase : ReactiveUserControl<PageViewModel> { }
    public partial class PageHeaderView : ReactiveUserControl<PageViewModel>
    {

        public PageHeaderView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
                {
                    this.WhenAnyValue(x => x.ViewModel)
                    .WhereNotNull()
                    .Subscribe(viewModel =>
                    {
                        this.OneWayBind(viewModel, vm => vm.Title, v => v.TitleText.Text).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IconIdString, v => v.Image.Source, idString => GetImageFromString(idString)).DisposeWith(d);
                    })
                    .DisposeWith(d);
                });
        }

        //using XamlNameReferenceGenerator which also generates InitializeComponent
        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        // }

        private IImage? GetImageFromString(string idString)
        {
            if (this.TryFindResource(idString, out var image))
                return (IImage?)image;
            return default;
        }
    }
}
