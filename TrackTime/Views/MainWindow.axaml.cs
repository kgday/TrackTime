using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ReactiveUI;

using System.Reactive.Disposables;
using System.Reactive;
using System.Reactive.Linq;

using TrackTime.ViewModels;
using System;
using Avalonia.Input;

namespace TrackTime.Views
{
    //public class MainWindowBase : ReactiveWindow<MainWindowViewModel> { }

    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, IDialogHost
    {

        private CompositeDisposable? _dialogSubscriptions;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    DialogHostBorder.IsVisible = false;
                    this.OneWayBind(viewModel, vm => vm.Pages, v => v.Pages.Items).DisposeWith(d);
                })
                .DisposeWith(d);
            });

        }
        //using XamlNameReferenceGenerator which also generates InitializeComponent
        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        // }

        //Returns an ovservable of when the dialog was closed - it could be closed by the user clicking the dialog background
        public IObservable<UIDialogResult<TResult>> ShowDialog<TResult>(DialogViewModel viewModel, IObservable<TResult> onResult)
        {
            _dialogSubscriptions = new CompositeDisposable();

            DialogCaptionText.Text = viewModel.Caption;
            DialogHost.ViewModel = viewModel;
            DialogHostBorder.IsVisible = true;

            Disposable.Create(() =>
            {
                DialogCaptionText.Text = string.Empty;
                DialogHostBorder.IsVisible = false;
                DialogHost.ViewModel = null;
            })
            .DisposeWith(_dialogSubscriptions);


            var clickedOnBackground = DialogHostBorder.Events().PointerPressed.Take(1).Select(_ => Unit.Default);
            onResult
                .Select(_ => Unit.Default)
                .Merge(clickedOnBackground)
                .Take(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { }, () => _dialogSubscriptions.Dispose())
                .DisposeWith(_dialogSubscriptions);

            return onResult.Select(result => new UIDialogResult<TResult>() { Result = result })
                .Merge(clickedOnBackground.Select(_ => new UIDialogResult<TResult>() { Cancelled = true }))
                .Take(1);
        }

    }
}
