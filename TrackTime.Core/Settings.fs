namespace TrackTime

open System
open System.Linq
open System.Text
open System.IO
open System.Data
open FSharp.Json

module Settings =
    type DBSettings =
        { DbHost: string option
          DbPort: int option
          DbName: string
          DbUser: string
          DbPassword: string }
        static member Empty() = { DbHost = None; DbPort = None; DbName = "TrackTime"; DbUser = "SomeUser"; DbPassword = "SomePassword" }

    type ReportOutputDirectories =
        { FullBillingSummary: string option
          FullBillingDetails: string option}
        static member Empty() = { FullBillingSummary = None; FullBillingDetails = None }

    type Settings =
        { DBSettings: DBSettings
          ReportOutputDirectories: ReportOutputDirectories }
        static member Empty() = { DBSettings = DBSettings.Empty(); ReportOutputDirectories = ReportOutputDirectories.Empty() }

    let private jsonConfig = JsonConfig.create (jsonFieldNaming = Json.lowerCamelCase)

    let configfilePathFactory () =
        let dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
#if !DEBUG
        Path.Combine(dir, "TrackTime", "settings.json")
#else
        Path.Combine(dir, "TrackTimeDev", "settings.json")
#endif

    let CheckDbConfigFileWithPath (configfilePath: string) =
        if not (File.Exists(configfilePath)) then
            configfilePath |> sprintf "Settings file %s not found." |> exn |> raise
        else
            configfilePath

    let getSettingsWithConfigPath (configfilePath: string) =
        configfilePath
        |> File.ReadAllText
        |> Json.deserializeEx<Settings> jsonConfig

    let getDBSettingFromSettings (settings: Settings) = settings.DBSettings

    let getSettingsWithConfigPathWithCheck = CheckDbConfigFileWithPath >> getSettingsWithConfigPath
    let getSettings = configfilePathFactory >> getSettingsWithConfigPathWithCheck


    let saveSettingsWithConfigFilePath (configfilePath: string) (settings: Settings) =
        let json = Json.serializeEx jsonConfig settings
        File.WriteAllText(configfilePath, json)

    let saveSettings = configfilePathFactory () |> saveSettingsWithConfigFilePath


    //do
    //    Settings.Empty()
    //    |> saveSettingsWithConfigFilePath @"c:\temp\defaultSettings.json"
