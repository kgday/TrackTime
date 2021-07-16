using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ReactiveUI;

using System;
using System.Reactive.Disposables;

using TrackTime.ViewModels;

namespace TrackTime.Views
{
    //public class MessageBoxViewBase : ReactiveUserControl<MessageBoxViewModel> { }

    public partial class MessageBoxView : ReactiveUserControl<MessageBoxViewModel>
    {
        public MessageBoxView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    this.OneWayBind(viewModel, vm => vm.Message, v => v.MessageText.Text).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.Dismiss, v => v.OKButton).DisposeWith(d);
                })
                .DisposeWith(d);
            });
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}