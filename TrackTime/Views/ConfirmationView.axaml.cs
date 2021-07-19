using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ReactiveUI;

using System;
using System.Reactive.Disposables;

using TrackTime.ViewModels;

namespace TrackTime.Views
{
    //public class ConfirmationViewBase : ReactiveUserControl<ConfirmationViewModel> { }

    public partial class ConfirmationView : ReactiveUserControl<ConfirmationViewModel>
    {
        public ConfirmationView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    this.OneWayBind(viewModel, vm => vm.Prompt, v => v.PromptText.Text).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.Yes, v => v.YesButton).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.No, v => v.NoButton).DisposeWith(d);
                })
                .DisposeWith(d);
            });
        }

        //using XamlNameReferenceGenerator which also generates InitializeComponent
        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        // }
    }
}