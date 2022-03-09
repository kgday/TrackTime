namespace TrackTime

open System

/// This is the Reports sample
/// you can use the Host to create a view control that has an independent program
/// if you want to be aware of the control's state  you should to refer to  Shell.fs and BlankPage.fs
/// and see how they relate to each other
module Reports =
    open Avalonia.Controls
    open Avalonia.Media.Imaging
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Components
    open Avalonia.FuncUI.Elmish
    open Elmish
    open TrackTime.Core

    /// sample function to load the initial data

    type State = { IsBusy: bool }

    type Msg =
        | LaunchBillingSummary
        | GenerateBillingSummary of outputFileName: string option
        | LaunchBillingDetails
        | GenerateBillingDetails of outputFileName: string option
        | ReportGenDone of Result<string, exn>
        | ShowErrorMessage of string
        | Nothing

    /// you can dispatch commands in your init if you chose to use `Program.mkProgram`
    /// instead of `Program.mkSimple`
    let init = { IsBusy = false }, Cmd.none

    let saveSummaryReportDirSettings reportDir =
        let settings = Settings.getSettings ()

        let newsettings =
            { settings with
                  ReportOutputDirectories = { settings.ReportOutputDirectories with FullBillingSummary = (Some reportDir) } }

        Settings.saveSettings newsettings

    let saveDetailsReportDirSettings reportDir =
        let settings = Settings.getSettings ()

        let newsettings =
            { settings with
                  ReportOutputDirectories = { settings.ReportOutputDirectories with FullBillingDetails = (Some reportDir) } }

        Settings.saveSettings newsettings

    let update (msg: Msg) (state: State) : State * Cmd<_> =
        match msg with
        | LaunchBillingSummary ->
            let windowService = Globals.GetWindowService()
            let settings = Settings.getSettings ()
            let outputDir = settings.ReportOutputDirectories.FullBillingSummary
            let defaultFileNameOnly = sprintf "Billing_Summary_Report_%s.pdf" (DateTime.Now.ToString("yyyyMMdd_HHmmss"))

            let defaultFileName =
                match outputDir with
                | Some dir -> System.IO.Path.Combine(dir, defaultFileNameOnly)
                | None -> defaultFileNameOnly

            { state with IsBusy = true }, Cmd.OfTask.perform (windowService.PromptSavePDFFile(Some "Save PDf File")) (Some defaultFileName) GenerateBillingSummary
        | GenerateBillingSummary fileNameOptional ->
            state,
            match fileNameOptional with
            | Some fileName ->
                Cmd.batch [ Cmd.OfAsync.perform BillingSummaryReportGen.generateBillingSummary fileName ReportGenDone
                            Cmd.OfFunc.perform saveSummaryReportDirSettings (System.IO.Path.GetDirectoryName(fileName)) (fun _ -> Nothing) ]
            | None -> Cmd.none
        | LaunchBillingDetails ->
            let windowService = Globals.GetWindowService()
            let settings = Settings.getSettings ()
            let outputDir = settings.ReportOutputDirectories.FullBillingDetails
            let defaultFileNameOnly = sprintf "Billing_Detail_Report_%s.pdf" (DateTime.Now.ToString("yyyyMMdd_HHmmss"))

            let defaultFileName =
                match outputDir with
                | Some dir -> System.IO.Path.Combine(dir, defaultFileNameOnly)
                | None -> defaultFileNameOnly

            { state with IsBusy = true }, Cmd.OfTask.perform (windowService.PromptSavePDFFile(Some "Save PDf File")) (Some defaultFileName) GenerateBillingDetails
        | GenerateBillingDetails fileNameOptional ->
            state,
            match fileNameOptional with
            | Some fileName ->
                Cmd.batch [ Cmd.OfAsync.perform BillingDetailsReportGen.generateBillingDetails fileName ReportGenDone
                            Cmd.OfFunc.perform saveDetailsReportDirSettings (System.IO.Path.GetDirectoryName(fileName)) (fun _ -> Nothing) ]
            | None -> Cmd.none

        | ReportGenDone genResult ->
            { state with IsBusy = false },
            match genResult with
            | Ok fileName -> Cmd.OfFunc.perform SysUtils.osOpen fileName (fun _ -> Nothing)
            | Error ex ->
                [ "Error generating billing summary report file."; ex.Message ]
                |> String.concat " "
                |> ShowErrorMessage
                |> Cmd.ofMsg
        | ShowErrorMessage errorMessageString ->
            let windowService = Globals.GetWindowService()
            state, Cmd.OfTask.perform windowService.ShowErrorMsg errorMessageString (fun _ -> Nothing)
        | Nothing -> state, Cmd.none

    let private billingSummRptView (dispatch: Dispatch<Msg>) =
        Button.create [ Button.classes [ "billingSummRpt" ]
                        Button.content "Full Billing Summary Report"
                        Button.onClick (fun e -> dispatch LaunchBillingSummary) ]

    let private billingDetailsRptView (dispatch: Dispatch<Msg>) =
        Button.create [ Button.classes [ "billingDetailsRpt" ]
                        Button.content "Full Billing Details Report"
                        Button.onClick (fun e -> dispatch LaunchBillingDetails) ]

    let view (state: State) (dispatch: Dispatch<Msg>) =
        WrapPanel.create [ WrapPanel.classes [ "Reportsgrid" ]
                           WrapPanel.children [ billingSummRptView dispatch; billingDetailsRptView dispatch ] ]



    type Host() as this =
        inherit Hosts.HostControl()

        do
            /// You can use `.mkSimple` yo remove the need of passing a command
            /// if you choose to do so, you  need to remove command from the tuple on your init fn
            /// you can learn more at https://elmish.github.io/elmish/basics.html
            let startFn () = init

            Elmish.Program.mkProgram startFn update view
            |> Program.withHost this
            |> Program.run
