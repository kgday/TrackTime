using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackTime.ViewModels
{
    public class NotificationViewModel
    {
        public NotificationViewModel(string message)
        {
            Message = message;
        }

        public string Message { get;}
    }
}
