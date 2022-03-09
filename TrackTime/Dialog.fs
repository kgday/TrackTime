namespace TrackTime

open Avalonia.Threading
open MessageBox.Avalonia
open Serilog
open Avalonia.Controls
open System.Threading.Tasks
open System
open System.IO

module Dialog =
    let showErrorMessageDialog owningWindow errorMessageString : Task<unit> =
        Dispatcher.UIThread.InvokeAsync<unit>
            (fun _ ->
                Log.Error errorMessageString

                let mb =
                    MessageBoxManager.GetMessageBoxStandardWindow("Error", errorMessageString, Enums.ButtonEnum.Ok, Enums.Icon.Error, WindowStartupLocation.CenterOwner)

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

    //copied and adapted from https://github.com/fsprojects/Avalonia.FuncUI/blob/master/src/Examples/Examples.MusicPlayer/Dialogs.fs

    let saveFileDialog owningWindow (dialogTitle: string option) (filters: FileDialogFilter seq option) (defaultFileName: string option) =
        Dispatcher.UIThread.InvokeAsync<string option>
            (fun _ ->
                task {
                    let dialog = SaveFileDialog()

                    let filters =
                        match filters with
                        | Some filter -> filter
                        | None ->

                            let allFilesFilter = FileDialogFilter()
                            allFilesFilter.Extensions <- Collections.Generic.List(seq { "*" })
                            allFilesFilter.Name <- "All Files"

                            seq { allFilesFilter }

                    let title =
                        match dialogTitle with
                        | Some t -> t
                        | None -> "Save File As.."



                    dialog.Directory <- 
                        match defaultFileName with
                        |Some fileName -> Path.GetDirectoryName(fileName)
                        |None -> Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    dialog.Title <- title
                    dialog.Filters <- System.Collections.Generic.List(filters)

                    dialog.InitialFileName <-
                        match defaultFileName with
                        | Some fileName -> fileName
                        | None -> ""

                    let! fileName = dialog.ShowAsync(owningWindow)
                    let fnameOption = if String.IsNullOrWhiteSpace(fileName) then None else Some fileName
                    return fnameOption
                })

    let savePDFFileDialog owningWindow (dialogTitle: string option) (defaultFileName: string option) =
        let filters =
            let pdfFilter = FileDialogFilter()
            pdfFilter.Extensions <- Collections.Generic.List(seq { "pdf" })
            pdfFilter.Name <- "Pdf Files"
            let allFilesFilter = FileDialogFilter()
            allFilesFilter.Extensions <- Collections.Generic.List(seq { "*" })
            allFilesFilter.Name <- "All Files"

            seq {
                pdfFilter
                allFilesFilter
            }

        saveFileDialog owningWindow dialogTitle (Some filters) defaultFileName
