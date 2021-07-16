using LiteDB;

using System;
using System.Collections.Generic;
using System.Text;

namespace TrackTime.Models
{
    public class WorkItem : ModelBase
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsBillable { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsFixedPrice { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now + TimeSpan.FromDays(7);
        public bool BeenBilled { get; set; }
        public string? Body { get; set; }

        public ObjectId? CustomerId { get; set; }
    }
}
