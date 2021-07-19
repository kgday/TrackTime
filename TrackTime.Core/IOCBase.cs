using ReactiveUI;

using Splat;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TrackTime.Data;
using TrackTime.ViewModels;

namespace TrackTime
{
    public abstract class IOCBase : IServiceResolver
    {
        private readonly IReadonlyDependencyResolver _resolver;

        protected IOCBase()
        {
            _resolver = Locator.Current;
            Container = Locator.CurrentMutable;
        }

        protected IMutableDependencyResolver Container { get; }

        public void RegisterDependencies()
        {
            RegisterDataServices();
            RegisterModelServices();
            RegisterOtherServices();
            RegisterViewModels();
            RegisterViewServices();
            RegisterViews();
        }

        protected abstract void RegisterViewServices();
        protected abstract void RegisterViews();

        protected virtual void RegisterViewModels()
        {
            Container.Register<CustomerViewModel>(() => new CustomerViewModel(
                () => GetService<ICustomerModelService>(),
                GetService<IDialogService>()));
            Container.Register<WorkItemViewModel>(() => new WorkItemViewModel(
                () => GetService<IWorkItemModelService>(),
                GetService<IDialogService>()));
            Container.Register<TimeEntryViewModel>(() => new TimeEntryViewModel(
                () => GetService<ITimeEntryModelService>(),
                GetService<IDialogService>()));

            //Container.Register<Func<CustomerViewModel>>(() => () => GetService<CustomerViewModel>());
            //Container.Register<Func<WorkItemViewModel>>(() => () => GetService<WorkItemViewModel>());
            //Container.Register<Func<TimeEntryViewModel>>(() => () => GetService<TimeEntryViewModel>());

            Container.RegisterLazySingleton(() => new CustomerListViewModel(
                () => GetService<CustomerViewModel>(),
                () => GetService<ICustomerModelService>()
                )
            );

            Container.RegisterLazySingleton(() => new WorkItemListViewModel(
                () => GetService<WorkItemViewModel>(),
                () => GetService<IWorkItemModelService>()
                )
            );

            Container.RegisterLazySingleton(() => new TimeEntryListViewModel(
                () => GetService<TimeEntryViewModel>(),
                () => GetService<ITimeEntryModelService>()
                )
            );


            Container.RegisterLazySingleton<PageViewModel>(() => new HomeViewModel(
                GetService<CustomerListViewModel>(),
                GetService<WorkItemListViewModel>(),
                GetService<TimeEntryListViewModel>()
                )
            );

            Container.RegisterLazySingleton<PageViewModel>(() => new SettingsViewModel());

            Container.RegisterLazySingleton(() => new MainWindowViewModel(_resolver.GetServices<PageViewModel>()));

        }


        protected static void RegisterOtherServices()
        {

        }

        protected void RegisterModelServices()
        {
            Container.Register<ICustomerModelService>(() => new CustomerModelService(GetService<IAppDataContext>()));
            Container.Register<IWorkItemModelService>(() => new WorkItemModelService(GetService<IAppDataContext>()));
            Container.Register<ITimeEntryModelService>(() => new TimeEntryModelService(GetService<IAppDataContext>()));


            //Container.Register<Func<ICustomerModelService>>(() => () => _resolver.GetService<ICustomerModelService>());
            //Container.Register<Func<IWorkItemModelService>>(() => () => _resolver.GetService<IWorkItemModelService>());
            //Container.Register<Func<ITimeEntryModelService>>(() => () => _resolver.GetService<ITimeEntryModelService>());
        }

        protected void RegisterDataServices()
        {
            Container.RegisterLazySingleton<IDbPath>(() => new DbPath());
            Container.RegisterLazySingleton<IAppDataContext>(() => new AppDataContext(GetService<IDbPath>()));
        }

        public T GetService<T>(string? contract = null)
        {
            var service = _resolver.GetService<T>(contract);
            if (service == null)
            {
                var sb = new StringBuilder(typeof(T).Name);
                if (!string.IsNullOrWhiteSpace(contract))
                    sb.Append($" (keyed: {contract})");
                throw new ServiceNotFound($"Unable to locate service {sb}");
            }
            return service;
        }
    }

    public class ServiceNotFound : Exception
    {
        public ServiceNotFound(string? message) : base(message)
        {
        }
    }
}