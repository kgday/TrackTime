using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;

using TrackTime.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace TrackTime.Views
{
    //public class CustomerViewBase : ReactiveUserControl<CustomerViewModel> { }
    public partial class CustomerView : ReactiveUserControl<CustomerViewModel>
    {
        public CustomerView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(v => v.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    this.OneWayBind(viewModel, vm => vm.Name, v => v.CustomerNameText.Text).DisposeWith(d);
                    this.OneWayBind(viewModel, vm => vm.ShowActiveIndicator, v => v.ActiveIndicator.IsVisible).DisposeWith(d);
                    this.OneWayBind(viewModel, vm => vm.ShowInactiveIndicator, v => v.InActiveIndicator.IsVisible).DisposeWith(d);
                    this.OneWayBind(viewModel, vm => vm.Phone, v => v.PhoneNoText.Text).DisposeWith(d);
                    this.OneWayBind(viewModel, vm => vm.Email, v => v.EmailText.Text).DisposeWith(d);
                    this.OneWayBind(viewModel, vm => vm.Notes, v => v.Notes.Text).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.Edit, v => v.EditButton).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.Delete, v => v.DeleteButton).DisposeWith(d);

                    this.OneWayBind(viewModel, vm => vm.IsEditing, v => v.ViewingGrid.IsVisible, editing => !editing).DisposeWith(d);
                    this.OneWayBind(viewModel, vm => vm.IsEditing, v => v.EditingGrid.IsVisible).DisposeWith(d);

                    this.Bind(viewModel, vm => vm.Name, v => v.CustomerNameEdit.Text).DisposeWith(d);
                    this.Bind(viewModel, vm => vm.IsActive, v => v.ActiveCheckBox.IsChecked).DisposeWith(d);
                    this.Bind(viewModel, vm => vm.Phone, v => v.PhoneNoEdit.Text).DisposeWith(d);
                    this.Bind(viewModel, vm => vm.Email, v => v.EmailEdit.Text).DisposeWith(d);
                    this.Bind(viewModel, vm => vm.Notes, v => v.NotesEdit.Text).DisposeWith(d);
                    this.BindValidation(viewModel, v => v.ErrorText.Text).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.SaveEdits, v => v.SaveButton).DisposeWith(d);
                    this.BindCommand(viewModel, vm => vm.CancelEdit, v => v.CancelButton).DisposeWith(d);
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
