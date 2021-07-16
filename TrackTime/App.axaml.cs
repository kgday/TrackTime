using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using TrackTime.ViewModels;
using TrackTime.Views;

namespace TrackTime
{
    public class App : Application
    {
        public App()
        {
                IOC.RegisterDependencies();
        }

        public IOC IOC { get; } = new IOC();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = IOC.GetService<MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
