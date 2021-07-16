using ReactiveUI;

namespace TrackTime.ViewModels
{
    public class RoutableViewModel : ViewModelBase, IRoutableViewModel
    {
        protected RoutableViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            UrlPathSegment = GetType().Name;
        }

        public string UrlPathSegment { get; }
        public IScreen HostScreen { get; }
    }
}
