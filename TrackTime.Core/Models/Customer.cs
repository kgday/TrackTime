using System;
using System.Collections.Generic;
using System.Text;

namespace TrackTime.Models
{
    public class Customer : ModelBase
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
    }

}
