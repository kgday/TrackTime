using LiteDB;

using System;
using System.Collections.Generic;
using System.Text;

namespace TrackTime.Models
{
    public class TimeEntry : ModelBase
    {
        public string Description { get; set; } = string.Empty;
        public DateTime TimeStart { get; set; } = DateTime.Now;
        public DateTime TimeEnd { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public bool BeenBilled { get; set; }

        public ObjectId? WorkItemId { get; set; }
    }
}
