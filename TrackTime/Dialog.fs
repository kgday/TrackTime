namespace TrackTime

open Avalonia.Threading
open MessageBox.Avalonia
open Serilog
open Avalonia.Controls
open System.Threading.Tasks

module Dialog =
    let showErrorMessageDialog owningWindow errorMessageString : Task<unit> =
        Dispatcher.UIThread.InvokeAsync<unit>
            (fun _ ->
                Log.Error errorMessageString

                let mb =
                    MessageBoxManager.GetMessageBoxStandardWindow(
                        "Error",
                        errorMessageString,
                        Enums.ButtonEnum.Ok,
                        Enums.Icon.Error,
                        WindowStartupLocation.CenterOwner
                    )

                mb.ShowDialog owningWindow |> ignore)


    let showConfirmationMessageDialog owningWindow confirmationMessageString : Task<bool> =
        Dispatcher.UIThread.InvokeAsync<bool>
            (fun _ ->
                task {
                    let mb =
                        MessageBoxManager.GetMessageBoxStandardWindow(
                            "Confirmation",
                            confirmationMessageString,
                            Enums.ButtonEnum.YesNo,
                            Enums.Icon.Warning,
                            WindowStartupLocation.CenterOwner
                        )

                    let! result = mb.ShowDialog owningWindow
                    return (result = Enums.ButtonResult.Yes)
                })
