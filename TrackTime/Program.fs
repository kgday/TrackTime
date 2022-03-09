namespace TrackTime

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Serilog
open Serilog.Configuration
open Avalonia.Themes.Fluent


/// This is your application you can ose the initialize method to load styles
/// or handle Life Cycle events of your application
type App() =
    inherit Application()
    override this.Initialize() =
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Light))
        this.Styles.Load "avares://TrackTime/Styles.axaml"
       
    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            let mainWindow = Shell.MainWindow()
            Globals.SetWindowService(mainWindow)
            desktopLifetime.MainWindow <- mainWindow
            mainWindow.Init()       
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main (args: string []) =
        Log.Logger <- 
            (new LoggerConfiguration()) 
                .Destructure.FSharpTypes()
                .WriteTo.Debug()
                .CreateLogger()
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
