using ReactiveUI;

using System;
using System.Collections.Generic;

namespace TrackTime.ViewModels
{
    public class PageViewModel : ViewModelBase
    {
        private string _title;

        private string _iconIdString;

        public PageViewModel(string title, string iconIdString)
        {
            _title = title;
            _iconIdString = iconIdString;
        }

        public string Title { get => _title; set => this.RaiseAndSetIfChanged(ref _title, value); }
        public string IconIdString { get => _iconIdString; set => this.RaiseAndSetIfChanged(ref _iconIdString, value); }
    }
}