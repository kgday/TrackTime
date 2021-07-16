using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using TrackTime.ViewModels;
using ReactiveUI;
using Avalonia.ReactiveUI;

namespace TrackTime.Views
{
    //public class SettingsViewBase : ReactiveUserControl<SettingsViewModel> { }
    public partial class SettingsView : ReactiveUserControl<SettingsViewModel>
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
