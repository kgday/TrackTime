using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Text;

namespace TrackTime.ViewModels
{
    public class HomeViewModel : PageViewModel
    {
        public HomeViewModel(CustomerListViewModel customerList, WorkItemListViewModel workItemList, TimeEntryListViewModel timeEntryList) : base("Home","Home")
        {
            CustomerList = customerList;
            WorkItemList = workItemList;
            TimeEntryList = timeEntryList;
        }
        public CustomerListViewModel CustomerList { get;  }
        public WorkItemListViewModel WorkItemList { get; }
        public TimeEntryListViewModel TimeEntryList { get; }
    }
}
