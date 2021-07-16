using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

using TrackTime.ViewModels;

namespace TrackTime.Views
{
    public interface IDialogHost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <param name="onHide"></param>
        /// <returns>An observable of when the dialog was closed - true if closed by the user clicking the dialog background</returns>
        IObservable<UIDialogResult<TResult>> ShowDialog<TResult>(DialogViewModel viewModel, IObservable<TResult> onResult);
    }

    public class UIDialogResult<TResult>
    {
        public bool Cancelled { get; set; } = false;
        public TResult? Result { get; set; } = default;
    }
}
