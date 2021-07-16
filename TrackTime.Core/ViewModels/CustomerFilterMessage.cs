using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackTime.ViewModels
{
    public class CustomerFilterMessage
    {
        public bool IncludingInActive { get; set; } = false;

        public CustomerFilterMessage(bool includingInActive)
        {
            IncludingInActive = includingInActive;
        }
    }
}
