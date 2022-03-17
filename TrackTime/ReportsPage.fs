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
    open FastReport

    /// sample function to load the initial data

    type State =
        { IsBusy: bool
          SummaryParams: BillingSummaryReport.ReportParams
          DetailParams: BillingDetailsReport.ReportParams }



    type Msg =
        | LaunchBillingSummary
        | LaunchBillingDetails
        | ReportPreviewClosed of Result<unit,exn>
        | ShowErrorMessage of string
        | Nothing

    /// you can dispatch commands in your init if you chose to use `Program.mkProgram`
    /// instead of `Program.mkSimple`
    let init =
        { IsBusy = false
          SummaryParams = BillingSummaryReport.ReportParams.All
          DetailParams = BillingDetailsReport.ReportParams.All },
        Cmd.none

    let saveSummaryReportDirSettings reportDir =
        let settings = Settings.getSettings ()

        let newsettings =
            { settings with
                ReportOutputDirectories =
                    { settings.ReportOutputDirectories with FullBillingSummary = (Some reportDir) } }

        Settings.saveSettings newsettings

    let saveDetailsReportDirSettings reportDir =
        let settings = Settings.getSettings ()

        let newsettings =
            { settings with
                ReportOutputDirectories =
                    { settings.ReportOutputDirectories with FullBillingDetails = (Some reportDir) } }

        Settings.saveSettings newsettings

    let previewReport (genReport:GenReportFunc) =
        let windowService = Globals.GetWindowService()
        windowService.PreviewReportDialog(fun _ -> ReportViewerDialog(genReport))

    let update (msg: Msg) (state: State) : State * Cmd<_> =
        match msg with
        | LaunchBillingSummary ->
            let genReport () = BillingSummaryReport.generateBillingSummary state.SummaryParams
            { state with IsBusy = true },
            Cmd.OfTask.perform previewReport genReport ReportPreviewClosed

        | LaunchBillingDetails ->
            let genReport () = BillingDetailsReport.generateBillingDetails state.DetailParams
            { state with IsBusy = true },
            Cmd.OfTask.perform previewReport genReport ReportPreviewClosed

        | ReportPreviewClosed result ->
            {state with IsBusy = false},
            match result with
            |Ok _ -> Cmd.none
            |Error e -> "Error previewing the report: "+e.Message |> ShowErrorMessage |> Cmd.ofMsg
        | ShowErrorMessage errorMessageString ->
            let windowService = Globals.GetWindowService()
            state, Cmd.OfTask.perform windowService.ShowErrorMsg errorMessageString (fun _ -> Nothing)
        | Nothing -> state, Cmd.none

    let private billingSummRptView state (dispatch: Dispatch<Msg>) =
        Button.create [ Button.classes [ "billingSummRpt" ]
                        Button.content "Full Billing Summary Report"
                        Button.isEnabled (not state.IsBusy)
                        Button.onClick (fun e -> dispatch LaunchBillingSummary) ]

    let private billingDetailsRptView state (dispatch: Dispatch<Msg>) =
        Button.create [ Button.classes [ "billingDetailsRpt" ]
                        Button.content "Full Billing Details Report"
                        Button.isEnabled (not state.IsBusy)
                        Button.onClick (fun e -> dispatch LaunchBillingDetails) ]

    let view (state: State) (dispatch: Dispatch<Msg>) =
        WrapPanel.create [ WrapPanel.classes [ "Reportsgrid" ]
                           WrapPanel.children [ billingSummRptView state dispatch
                                                billingDetailsRptView state dispatch ] ]



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
