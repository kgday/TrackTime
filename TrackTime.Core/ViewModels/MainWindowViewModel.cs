using ReactiveUI;

using Splat;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace TrackTime.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IEnableLogger/*,  IActivatableViewModel*/
    {

        public MainWindowViewModel(IEnumerable<PageViewModel> pages)
        {
            Pages = new(pages);
        }

        public ObservableCollection<PageViewModel> Pages { get; }


        //public ViewModelActivator Activator { get; } = new ViewModelActivator();
    }
}
