using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ReactiveUI;

using TrackTime.ViewModels;
using System.Reactive.Disposables;
using System;

namespace TrackTime.Views
{
    public partial class NotificationSuccessView : ReactiveUserControl<NotificationSuccessViewModel>
    {
        public NotificationSuccessView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    this.OneWayBind(viewModel, vm => vm.Message, v => v.MessageText.Text).DisposeWith(d);
                })
                .DisposeWith(d);
            });
        }

        //private void InitializeComponent()
        //{
        //    AvaloniaXamlLoader.Load(this);
        //}
    }
}
