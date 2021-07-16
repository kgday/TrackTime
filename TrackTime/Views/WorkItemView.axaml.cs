using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using TrackTime.ViewModels;

namespace TrackTime.Views
{
    //public class WorkItemViewBase : ReactiveUserControl<WorkItemViewModel> { }
    public partial class WorkItemView : ReactiveUserControl<WorkItemViewModel>
    {
        public WorkItemView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
