using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ReactiveUI;

using Splat;

using TrackTime.ViewModels;
using TrackTime.Views;

namespace TrackTime
{
    public class IOC : IOCBase
    {
        protected override void RegisterViewModels()
        {
            base.RegisterViewModels();
            Container.Register(() => new ConfirmationViewModel());
            Container.Register(() => new MessageBoxViewModel());
        }

        protected override void RegisterViews()
        {
            Container.RegisterLazySingleton(() =>
            {
                var mainWindow = new MainWindow
                {
                    DataContext = GetService<MainWindowViewModel>(),
                };
                return mainWindow;
            });
            Container.Register<IDialogHost>(() => GetService<MainWindow>());
            Container.Register<IViewFor<HomeViewModel>>(() => new HomeView());
            Container.Register<IViewFor<SettingsViewModel>>(() => new SettingsView());

            Container.Register<IViewFor<CustomerListViewModel>>(() => new CustomerListView());
            Container.Register<IViewFor<CustomerViewModel>>(() => new CustomerView());
            Container.Register<IViewFor<WorkItemViewModel>>(() => new WorkItemView());

            Container.Register<IViewFor<ConfirmationViewModel>>(() => new ConfirmationView());
            Container.Register<IViewFor<MessageBoxViewModel>>(() => new MessageBoxView());
        }

        protected override void RegisterViewServices()
        {
            Container.RegisterLazySingleton<IDialogService>(() => new DialogService(GetService<IDialogHost>()));
        }
    }
}
