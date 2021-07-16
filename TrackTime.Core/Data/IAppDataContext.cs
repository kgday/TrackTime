using LiteDB;

using System;
using System.Collections.Generic;
using System.Text;

using TrackTime.Models;

namespace TrackTime.Data
{
    public interface IAppDataContext
    {
        ILiteCollection<Customer> Customers { get; }
        ILiteCollection<WorkItem> WorkItems { get; }
        ILiteCollection<TimeEntry> TimeEntries { get; }
    }
}
