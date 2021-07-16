using ReactiveUI;

namespace TrackTime.ViewModels
{
    public class DialogViewModel : ViewModelBase
    {
        private string _caption = string.Empty;

        public string Caption { get => _caption; set => this.RaiseAndSetIfChanged(ref _caption, value); }
    }
}