using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ReactiveUI;

using System.Reactive.Disposables;

using TrackTime.ViewModels;
using System;
using ReactiveUI.Validation.Extensions;

namespace TrackTime.Views
{
    //public class WorkItemViewBase : ReactiveUserControl<WorkItemViewModel> { }
    public partial class WorkItemView : ReactiveUserControl<WorkItemViewModel>
    {
        public WorkItemView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
                {
                    this.WhenAnyValue(x=>x.ViewModel)
                    .WhereNotNull()
                    .Subscribe(viewModel =>
                    {
                        this.OneWayBind(viewModel, vm => vm.IsEditing, v => v.ViewingGrid.IsVisible, editing => !editing).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IsEditing, v => v.EditingGrid.IsVisible).DisposeWith(d);

                        this.OneWayBind(viewModel, vm => vm.Title, v => v.WorkItemTitleText.Text).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.Description, v => v.DescriptionText.Text).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IsBillable, v => v.NotBillableIndicator.IsVisible, billable => !billable).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IsCompleted, v => v.CompletedIndicator.IsVisible).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IsCompleted, v => v.InProgressIndicator.IsVisible, completed => !completed).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IsFixedPrice, v => v.FixedPriceIndicator.IsVisible).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.BeenBilled, v => v.BilledIndicator.IsVisible).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.BeenBilled, v => v.NotBillableIndicator.IsVisible, billed => !billed).DisposeWith(d);
                        //TODO ITEMS FOR DATE CREATED AND DATE DUE AS WELL AS THE REST OF THE BINDING

                        this.Bind(viewModel, vm => vm.Title, v => v.WorkItemTitleEdit.Text).DisposeWith(d);
                        this.Bind(viewModel, vm => vm.Description, v => v.WorkItemDescriptionEdit.Text).DisposeWith(d);

                        this.BindValidation(viewModel, v => v.ErrorText.Text).DisposeWith(d);
                        this.BindCommand(viewModel, vm => vm.SaveEdits, v => v.SaveButton).DisposeWith(d);
                        this.BindCommand(viewModel, vm => vm.CancelEdit, v => v.CancelButton).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IsNew, v => v.OuterBorder.BorderThickness, isNew => isNew ? new Thickness(1) : new Thickness(0)).DisposeWith(d);
                        this.OneWayBind(viewModel, vm => vm.IsSelected, v => v.ButtonPanel.IsVisible).DisposeWith(d);

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
