namespace TrackTime

open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.FuncUI
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open System.Data
open DataModels
open Avalonia.FuncUI.DSL
open System
open Avalonia.Media.Imaging
open FastReport
open FastReport.Export
open FastReport.Export.Image
open System.Threading
open System.Reactive.Linq
open Avalonia.FuncUI.Types
open System.Reactive.Subjects
open Microsoft.CodeAnalysis.VisualBasic


type GenReportFunc = unit -> Async<string * Result<string, exn>>

module ReportViewer =
    module Zoom =
        [<Literal>]
        let In = 10

        [<Literal>]
        let Out = -10

    type Zoom =
        private
        | Zoom of int
        static member init() = Zoom 100

        member this.Value =
            match this with
            | Zoom zoom -> zoom

        ///Give the next zoom value with the amount to change
        /// but returns none if the next zoom value is not valid
        member private this.Next by =
            let proposed = this.Value + by

            if (proposed <= 200) && (proposed >= 40) then
                Some proposed
            else
                None

        member this.Can by = this.Next by |> Option.isSome

        member this.Do by =
            this.Next by
            |> Option.defaultValue this.Value
            |> Zoom

        member this.reset() = Zoom.init ()

    type State =
        { CurrentPageNo: int
          CurrentPageImage: Bitmap option
          PageImages: Bitmap list
          Report: Report option
          RptExport: ImageExport
          CurrentZoom: Zoom
          //NormalImageSize: Size
          Busy: bool }

        member this.Dispose() =
            match this.Report with
            | Some report -> report.Dispose()
            | None -> ()

            this.RptExport.Dispose()

        member this.hasPages = List.isEmpty this.PageImages |> not
        member this.isOnFirstPage = this.CurrentPageNo = 0
        member this.canFirstPage = this.hasPages && (not this.isOnFirstPage)
        member this.canPreviousPage = this.hasPages && (not this.isOnFirstPage)
        member this.isNotOnLastPage = this.CurrentPageNo < (List.length this.PageImages)
        member this.canLastPage = this.hasPages && this.isNotOnLastPage
        member this.canNextPage = this.hasPages && this.isNotOnLastPage

        member this.isValidPage p =
            this.hasPages
            && (p >= 0)
            && (p < this.PageImages.Length)

    type Msg =
        | Noop
        | GenerateReport of GenReportFunc
        | ReportGenerated of title: string * genResult: Result<string, exn>
        | StartLoadReportContent
        | FinishedLoadReportContent of Result<Bitmap list, string>
        | FirstPage
        | IncrementPageNo
        | DecrementPageNo
        | LastPage
        | RequestPageNo of int
        | PageNoText of string
        | ZoomIn
        | ZoomOut
        | ResetZoom
        | ShowErrorMessage of string
    //| NormalImageSizeChange of Size

    [<Literal>]
    let private normalResolution = 96


    let init () =
        let exp = new ImageExport()
        exp.HasMultipleFiles <- true
        exp.Resolution <- normalResolution

        { CurrentPageNo = 0
          PageImages = []
          CurrentPageImage = None
          Report = None
          RptExport = exp
          CurrentZoom = Zoom.init ()
          //NormalImageSize = Size(0, 0)
          Busy = false },
        Cmd.none

    let getReportImages (state: State) =
        match state.Report with
        | Some report ->
            try

                let zoom = state.CurrentZoom
                let exp = state.RptExport
                let imagePath = ReportUtils.createRptPagesImagesDir ()
                exp.ImageFormat <- ImageExportFormat.Png

                let res =
                    (float normalResolution) * (float zoom.Value)
                    / 100.0

                exp.Resolution <- int res
                let f = IO.Path.Combine(imagePath, "exportimage.png")
                exp.Export(report, f)

                exp.GeneratedFiles
                |> Seq.map (fun f -> new Bitmap(f))
                |> Seq.toList
                |> Ok
            with
            | e ->
                e.Message
                |> sprintf "Error loading report. %s"
                |> Error
        | None -> Error "Report failed to generate."

    open FSharp.Control.Reactive

    module Int32 =
        let tryParse (str: string) : int option =
            let mutable result = 0

            if Int32.TryParse(str, &result) then
                Some result
            else
                None

    module internal PageNumberTextEvent =
        let private event = Event<string>()
        let Trigger s = event.Trigger s

        let OnThrottled _ =
            let sub dispatch =
                event.Publish
                |> Observable.throttle (TimeSpan.FromSeconds(2))
                |> Observable.map (fun s -> s |> Int32.tryParse)
                |> Observable.filter (fun a -> a.IsSome)
                |> Observable.subscribe (fun pn ->
                    match pn with
                    | Some p -> p |> RequestPageNo |> dispatch
                    | None -> Noop |> dispatch (*just for completeness*) )
                |> ignore

            Cmd.ofSub sub

    module internal TitleEvent =
        let private event = Event<string>()
        let Trigger s = event.Trigger s
        let OnChanged = event.Publish

    let update msg state : State * Cmd<_> =
        match msg with
        | Noop -> state, Cmd.none
        | GenerateReport genReport -> { state with Busy = true }, Cmd.OfAsync.perform genReport () ReportGenerated
        | ReportGenerated (title, result) ->
            TitleEvent.Trigger title

            match result with
            | Ok reportFileName ->
                let report = new Report()

                try
                    report.LoadPrepared(reportFileName)
                    { state with Report = Some report }, Cmd.ofMsg StartLoadReportContent
                with
                | e ->
                    state,
                    e.Message
                    |> sprintf "Report %s was not generated. %s" title
                    |> ShowErrorMessage
                    |> Cmd.ofMsg

            | Error e ->
                { state with Report = None },
                e.Message
                |> sprintf "Error occured generating '%s'. %s" title
                |> ShowErrorMessage
                |> Cmd.ofMsg
        | StartLoadReportContent ->
            match state.Report with
            | Some report ->
                { state with Busy = true }, Cmd.OfFunc.perform getReportImages state FinishedLoadReportContent
            | None -> state, Cmd.none
        | FinishedLoadReportContent pagesResult ->
            match pagesResult with
            | Ok pages ->
                { state with
                    Busy = false
                    PageImages = pages },
                Cmd.ofMsg (RequestPageNo 0)
            | Error msg -> { state with Busy = false }, msg |> ShowErrorMessage |> Cmd.ofMsg
        | FirstPage ->
            state,
            if state.canFirstPage then
                0 |> RequestPageNo |> Cmd.ofMsg
            else
                Cmd.none
        | IncrementPageNo ->
            state,
            state.CurrentPageNo + 1
            |> RequestPageNo
            |> Cmd.ofMsg
        | DecrementPageNo ->
            state,
            state.CurrentPageNo - 1
            |> RequestPageNo
            |> Cmd.ofMsg
        | LastPage ->
            state,
            state.PageImages
            |> List.length
            |> (+) (-1)
            |> RequestPageNo
            |> Cmd.ofMsg
        | RequestPageNo pageNo ->
            (if state.isValidPage pageNo then
                 { state with
                     CurrentPageNo = pageNo
                     CurrentPageImage = Some state.PageImages[pageNo] }
             else
                 state),
            Cmd.none
        | PageNoText s ->
            PageNumberTextEvent.Trigger s
            state, Cmd.none
        | ZoomIn ->
            if state.CurrentZoom.Can Zoom.In then
                { state with
                    Busy = true
                    CurrentZoom = state.CurrentZoom.Do Zoom.In },
                Cmd.ofMsg StartLoadReportContent
            else
                state, Cmd.none
        | ZoomOut ->
            if state.CurrentZoom.Can Zoom.Out then
                { state with
                    Busy = true
                    CurrentZoom = state.CurrentZoom.Do Zoom.Out },
                Cmd.ofMsg StartLoadReportContent
            else
                state, Cmd.none
        | ResetZoom ->
            { state with
                Busy = true
                CurrentZoom = state.CurrentZoom.reset () },
            Cmd.ofMsg StartLoadReportContent
        | ShowErrorMessage msgStr ->
            let windowService = Globals.GetWindowService()
            state, Cmd.OfTask.perform windowService.ShowErrorMsg msgStr (fun _ -> Noop)


    let view (state: State) (dispatch: Dispatch<Msg>) =
        Grid.create [ Grid.rowDefinitions "Auto,*"
                      Grid.children [ StackPanel.create [ Grid.row 0
                                                          StackPanel.orientation Layout.Orientation.Horizontal
                                                          StackPanel.children [ Button.create [ Button.classes [ "pagingControl"
                                                                                                                 "first" ]
                                                                                                Button.isEnabled
                                                                                                    state.canFirstPage
                                                                                                Button.onClick
                                                                                                    (fun _ ->
                                                                                                        FirstPage
                                                                                                        |> dispatch) ]
                                                                                Button.create [ Button.classes["pagingControl"
                                                                                                               "previous"]

                                                                                                Button.isEnabled
                                                                                                    state.canPreviousPage

                                                                                                Button.onClick
                                                                                                    (fun _ ->
                                                                                                        DecrementPageNo
                                                                                                        |> dispatch) ]
                                                                                TextBox.create [ TextBox.classes [ "pagingControl"
                                                                                                                   "currentPageNo" ]
                                                                                                 TextBox.onTextChanged
                                                                                                     (fun txt ->
                                                                                                         txt
                                                                                                         |> PageNoText
                                                                                                         |> dispatch)
                                                                                                 TextBox.text (
                                                                                                     (state.CurrentPageNo
                                                                                                      + 1)
                                                                                                         .ToString()
                                                                                                 ) ]
                                                                                Button.create [ Button.classes [ "pagingControl"
                                                                                                                 "next" ]
                                                                                                Button.isEnabled
                                                                                                    state.canNextPage
                                                                                                Button.onClick
                                                                                                    (fun _ ->
                                                                                                        IncrementPageNo
                                                                                                        |> dispatch) ]
                                                                                Button.create [ Button.classes["pagingControl"
                                                                                                               "last"]
                                                                                                Button.isEnabled
                                                                                                    state.canLastPage
                                                                                                Button.onClick
                                                                                                    (fun _ ->
                                                                                                        LastPage
                                                                                                        |> dispatch) ]
                                                                                Button.create [ Button.classes [ "zoomControl"
                                                                                                                 "zoomIn" ]
                                                                                                Button.isEnabled (
                                                                                                    state.CurrentZoom.Can
                                                                                                        Zoom.In
                                                                                                )
                                                                                                Button.onClick
                                                                                                    (fun _ ->
                                                                                                        ZoomIn
                                                                                                        |> dispatch) ]
                                                                                Button.create [ Button.classes [ "zoomControl"
                                                                                                                 "zoomIn" ]
                                                                                                Button.isEnabled (
                                                                                                    state.CurrentZoom.Can
                                                                                                        Zoom.Out
                                                                                                )
                                                                                                Button.onClick
                                                                                                    (fun _ ->
                                                                                                        ZoomOut
                                                                                                        |> dispatch) ] ]


                                                           ]
                                      if state.Busy then
                                          ProgressBar.create [ ProgressBar.classes [ "busy" ]
                                                               Grid.row 1 ]
                                      else
                                          ScrollViewer.create [ ScrollViewer.classes [ "reportPage" ]
                                                                Grid.row 1
                                                                ScrollViewer.content (
                                                                    Border.create [ Border.classes [ "reportPage" ]
                                                                                    Border.child (
                                                                                        Image.create [ Image.classes [ "reportPage" ]
                                                                                                       match state.CurrentPageImage
                                                                                                           with
                                                                                                       | Some image ->
                                                                                                           image
                                                                                                       | None -> null
                                                                                                       |> Image.source ]
                                                                                    ) ]
                                                                ) ]

                                       ] ]

type ReportViewerDialog(genReport) as this =
    inherit HostWindow()

    do
        this.Title <- "Report Preview"
        this.Width <- 500.0
        this.Height <- 400.0
        this.WindowStartupLocation <- WindowStartupLocation.CenterOwner
        this.MinWidth <- 400.0
        this.MinHeight <- 300.0

        //let init () = Customer.init customerId

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        let update = ReportViewer.update
        let init = ReportViewer.init
        let view = ReportViewer.view

        let subscribeActivated _ =
            let sub dispatch =
                this
                    .Activated
                    .Take(1)
                    .Subscribe(fun _ ->
                        genReport
                        |> ReportViewer.GenerateReport
                        |> dispatch)
                |> ignore

            Cmd.ofSub sub

        let setTitle title = this.Title <- title

        ReportViewer.TitleEvent.OnChanged.Subscribe(fun title -> title |> sprintf "Report Preview: %s" |> setTitle)
        |> ignore

        Elmish.Program.mkProgram init update view
        |> Program.withHost this
        |> Program.withSubscription subscribeActivated
        |> Program.withSubscription ReportViewer.PageNumberTextEvent.OnThrottled
        //|> Program.withConsoleTrace
        |> Program.run
