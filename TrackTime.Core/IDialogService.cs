using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace TrackTime
{

    public interface IDialogService
    {
        IObservable<bool> ConfirmationYesNo(string caption, string prompt);
        IObservable<Unit> MessageBox(string caption, string message);
    }
}
