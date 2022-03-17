namespace TrackTime

open System
open DataModels


module BillingDetailsReport =
    open ReportExtensions

    type BillingDetails_TimeEntryDetail =
        { TimeStart: DateTime
          TimeEnd: DateTime
          DurationHrs: double}

    type BillingDetails_TimeEntrySummary =
        { Description: string
          Details: BillingDetails_TimeEntryDetail list
          TotalDurationHrs: double }

    type BillingDetails_WorkItem =
        { WorkItemId: WorkItemId
          Title: string
          DateCreated: DateTime
          IsCompleted: bool
          IsFixedPriced: bool
          TimeEntries: BillingDetails_TimeEntrySummary list
          TotalDurationHrs: double }

    type BillingDetails_Customer =
        { CustomerId: CustomerId
          Name: string
          WorkItems: BillingDetails_WorkItem list
          TotalDurationHrs: double }

    type BillingDetails =
        { RptSubTitle: string
          Customers: BillingDetails_Customer list }

    let convertResultsToReportStructure rptSubTitle (results: BillingData list) =
        let timeEntryDetailFromData (data: BillingData) : BillingDetails_TimeEntryDetail option =
            match data.TimeEntryTimeStart with
            | Some timeStart ->
                let durationHrs =
                    data.TimeEntryDuration
                    |> Option.map (fun d -> d.TotalHours)
                    |> Option.defaultValue 0.0

                { BillingDetails_TimeEntryDetail.TimeStart = timeStart
                  BillingDetails_TimeEntryDetail.TimeEnd =
                    data.TimeEntryTimeEnd
                    |> Option.defaultValue Unchecked.defaultof<DateTime>
                  BillingDetails_TimeEntryDetail.DurationHrs = durationHrs }
                |> Some
            | None -> None

        let assignTimeEntryDetailsListAndTotal
            (
                (timeEntry: BillingDetails_TimeEntrySummary),
                (subList: BillingData list)
            ) : BillingDetails_TimeEntrySummary =
            let timeEntryDetails =
                subList
                |> List.map timeEntryDetailFromData
                |> List.filter (fun detailOption -> detailOption.IsSome)
                |> List.map (fun detailOption -> detailOption.Value)

            let totalDurationHrs =
                let listDurationHrsMinutes =
                    timeEntryDetails
                    |> List.map (fun te ->te.DurationHrs)

                if List.isEmpty listDurationHrsMinutes then
                    0.0
                else
                    listDurationHrsMinutes
                    |> List.sum

            { timeEntry with
                BillingDetails_TimeEntrySummary.Details = timeEntryDetails
                TotalDurationHrs = totalDurationHrs}

        let timeEntryFromData (data: BillingData) : BillingDetails_TimeEntrySummary =
            { BillingDetails_TimeEntrySummary.Description = data.TimeEntryDescription
              BillingDetails_TimeEntrySummary.Details = []
              TotalDurationHrs = 0.0}

        let assignWorkItemTimeEntryListAndTotal ((workItem: BillingDetails_WorkItem), (subList: BillingData list)) =
            let timeEntryList =
                subList
                |> List.groupBy timeEntryFromData
                |> List.map assignTimeEntryDetailsListAndTotal

            let totalDurationHrs =
                let listDurationHrsMinutes =
                    timeEntryList
                    |> List.map (fun te ->te.TotalDurationHrs)

                if List.isEmpty listDurationHrsMinutes then
                    0.0
                else
                    listDurationHrsMinutes
                    |> List.sum

            { workItem with
                BillingDetails_WorkItem.TimeEntries = timeEntryList
                BillingDetails_WorkItem.TotalDurationHrs = totalDurationHrs }

        let workItemFromData (data: BillingData) : BillingDetails_WorkItem =
            { BillingDetails_WorkItem.WorkItemId = data.WorkItemId
              BillingDetails_WorkItem.Title = data.WorkItemTitle
              BillingDetails_WorkItem.DateCreated = data.WorkItemDateCreate
              BillingDetails_WorkItem.IsCompleted = data.WorkItemIsCompleted
              BillingDetails_WorkItem.IsFixedPriced = data.WorkItemIsFixedPriced
              BillingDetails_WorkItem.TimeEntries = []
              BillingDetails_WorkItem.TotalDurationHrs = 0 }

        let assignCustomerWorkItemListAndTotal
            (
                (customer: BillingDetails_Customer),
                (subList: BillingData list)
            ) : BillingDetails_Customer =
            let workItemList =
                subList
                |> List.groupBy workItemFromData
                |> List.map assignWorkItemTimeEntryListAndTotal

            let totalDurationHrs =
                let DurationHrsLists =
                    workItemList
                    |> List.map (fun wi ->wi.TotalDurationHrs)

                if List.isEmpty workItemList then
                    0.0
                else
                    DurationHrsLists
                    |> List.sum

            { customer with
                BillingDetails_Customer.WorkItems = workItemList
                BillingDetails_Customer.TotalDurationHrs = totalDurationHrs }

        let customerFromData (data: BillingData) : BillingDetails_Customer =
            { BillingDetails_Customer.CustomerId = data.CustomerId
              BillingDetails_Customer.Name = data.CustomerName
              BillingDetails_Customer.WorkItems = []
              BillingDetails_Customer.TotalDurationHrs = 0}

        let customers =
            results
            |> List.groupBy customerFromData
            |> List.map assignCustomerWorkItemListAndTotal

        { BillingDetails.RptSubTitle = rptSubTitle
          BillingDetails.Customers = customers }

    let readReportDataAllUnbilled () =
        try
            AppDataService.getAllUnbilledBillingDetails ()
            |> Result.map (fun dataList ->
                dataList
                |> convertResultsToReportStructure "All Unbilled")
        with
        | ex -> Error ex

    //open QuestPDF.Drawing
    //open QuestPDF.Fluent
    //open QuestPDF.Helpers
    //open QuestPDF.Infrastructure


(*

    let  composeReportContentTable (data: BillingDetails) (container: IContainer) =
        container.Table
            (fun table ->
                table.ColumnsDefinition
                    (fun cols ->
                        cols.ConstantColumn(25f)
                        cols.RelativeColumn()
                        cols.ConstantColumn(70f)
                        cols.ConstantColumn(65f)
                        cols.ConstantColumn(40f)
                        cols.RelativeColumn()
                        cols.ConstantColumn(100f)
                        cols.ConstantColumn(100f)
                        cols.ConstantColumn(40f))

                table.Header
                    (fun hdr ->
                        hdr
                            .Cell()
                            .Column(1u)
                            .Row(1u)
                            .ColumnSpan(2u)
                            .Text("Customer", Report.colHeaderStyle)

                        hdr.Cell().Column(2u).Row(2u).Text("Work Item", Report.colHeaderStyle)

                        hdr
                            .Cell()
                            .Column(3u)
                            .Row(2u)
                            .AlignRight()
                            .Text("Date Created", Report.colHeaderStyle)

                        hdr
                            .Cell()
                            .Column(4u)
                            .Row(2u)
                            .AlignCenter()
                            .Text("Completed", Report.colHeaderStyle)

                        hdr
                            .Cell()
                            .Column(5u)
                            .Row(1u)
                            .RowSpan(2u)
                            .AlignCenter()
                            .Text("Fixed Price", Report.colHeaderStyle)

                        hdr.Cell().Column(6u).Row(2u).Text("Time Entry", Report.colHeaderStyle)

                        hdr
                            .Cell()
                            .Column(7u)
                            .Row(2u)
                            .AlignRight()
                            .Text("Time Start", Report.colHeaderStyle)

                        hdr
                            .Cell()
                            .Column(8u)
                            .Row(2u)
                            .AlignRight()
                            .Text("Time End", Report.colHeaderStyle)

                        hdr
                            .Cell()
                            .Column(9u)
                            .Row(2u)
                            .AlignRight()
                            .Text("H:M", Report.colHeaderStyle)

                        hdr.Cell().ColumnSpan(9u).BorderBottom(0.75f).BorderColor(Colors.Black)
                        |> ignore)

                let mutable row = 1u

                let countTimeEntryDetailsPerWorkItem wi = wi.TimeEntries |> List.map (fun te -> te.Details.Length) |> List.sum

                data.Customers
                |> List.iter
                    (fun cust ->
                        table.Cell().Row(row).Column(1u).ColumnSpan(2u).Text(cust.Name)
                        row <- row + 1u

                        cust.WorkItems
                        |> List.iter
                            (fun wi ->
                                let isCompletedStr = if wi.IsCompleted then "Yes" else "No"
                                let isFixedPrice = if wi.IsFixedPriced then "Yes" else "No"

                                table.Cell().Row(row).Column(2u).Text(wi.Title)

                                table
                                    .Cell()
                                    .Row(row)
                                    .Column(3u)
                                    .AlignRight()
                                    .Text(wi.DateCreated.ToString("d"))

                                table.Cell().Row(row).Column(4u).AlignCenter().Text(isCompletedStr)
                                table.Cell().Row(row).Column(5u).AlignCenter().Text(isFixedPrice)

                                wi.TimeEntries
                                |> List.iter
                                    (fun te ->
                                        table.Cell().Row(row).Column(6u).RowSpan(uint32 2).Text(te.Description)

                                        te.Details
                                        |> List.iter
                                            (fun detail ->
                                                let timeStartStr = detail.TimeStart.ToString("g")

                                                let timeEndStr =
                                                    detail.TimeEnd
                                                    |> Option.map (fun d -> d.ToString("g"))
                                                    |> Option.defaultValue ""

                                                let DurationHrstr =
                                                    detail.DurationHrs
                                                    |> Option.map (fun d -> d.Formated())
                                                    |> Option.defaultValue ""

                                                table.Cell().Row(row).Column(7u).AlignRight().Text(timeStartStr)
                                                table.Cell().Row(row).Column(8u).AlignRight().Text(timeEndStr)
                                                table.Cell().Row(row).Column(9u).AlignRight().Text(DurationHrstr)

                                                row <- row + 1u)

                                        if (te.Details.Length > 1) && te.TotalDurationHrs.IsSome then
                                            table
                                                .Cell()
                                                .Row(row)
                                                .Column(6u)
                                                .ColumnSpan(3u)
                                                .AlignRight()
                                                //.BorderTop(0.05f)
                                                //.BorderColor(Colors.Black)
                                                .Text(
                                                    $"Total for Time Entry \'{te.Description}\':",
                                                    Report.totalHeaderStyle
                                                )

                                            table
                                                .Cell()
                                                .Row(row)
                                                .Column(9u)
                                                .AlignRight()
                                                .BorderTop(0.05f)
                                                .BorderColor(Colors.Grey.Medium)
                                                .Text(te.TotalDurationHrs.Value.Formated(), Report.totalValueStyle)

                                            table
                                                .Cell()
                                                .Row(row)
                                                .Column(6u)
                                                .ColumnSpan(4u)
                                                .BorderBottom(0.05f)
                                                .BorderColor(Colors.Grey.Medium)
                                            |> ignore


                                            row <- row + 1u

                                        )

                                if wi.TotalDurationHrs.IsSome then
                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(2u)
                                        .ColumnSpan(7u)
                                        .AlignRight()
                                        //.BorderTop(0.05f)
                                        //.BorderColor(Colors.Grey.Medium)
                                        .Text(
                                            $"Total for Work Item \'{wi.Title}\':",
                                            Report.totalHeaderStyle
                                        )

                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(9u)
                                        .AlignRight()
                                        .BorderTop(0.05f)
                                        .BorderColor(Colors.Grey.Medium)
                                        .Text(wi.TotalDurationHrs.Value.Formated(), Report.totalValueStyle)

                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(2u)
                                        .ColumnSpan(8u)
                                        .BorderBottom(0.05f)
                                        .BorderColor(Colors.Grey.Medium)

                                    |> ignore


                                    row <- row + 1u)

                        if cust.TotalDurationHrs.IsSome then
                            table
                                .Cell()
                                .Row(row)
                                .Column(1u)
                                .ColumnSpan(8u)
                                .AlignRight()
                                //.BorderTop(0.05f)
                                //.BorderColor(Colors.Black)
                                .Text(
                                    $"Total for Customer \'{cust.Name}\':",
                                    Report.totalHeaderStyle
                                )

                            table
                                .Cell()
                                .Row(row)
                                .Column(9u)
                                .AlignRight()
                                .BorderTop(0.05f)
                                .BorderColor(Colors.Grey.Medium)
                                .Text(cust.TotalDurationHrs.Value.Formated(), Report.totalValueStyle)

                            table
                                .Cell()
                                .Row(row)
                                .Column(1u)
                                .ColumnSpan(9u)
                                .BorderBottom(0.05f)
                                .BorderColor(Colors.Grey.Medium)
                            |> ignore

                            row <- row + 1u
                        //table
                        //    .Cell()
                        //    .Row(row)
                        //    .Column(1u)
                        //    .ColumnSpan(9u)
                        //    .BorderTop(0.05f)
                        //    .BorderColor(Colors.Grey.Medium)
                        //    |> ignore
                        //row <- row + 1u
                        ))

*)
    type ReportParams =
        |All
    open FastReport
    open FastReport.Export
    open FastReport.Export.PdfSimple

    let tempDir =
        let d = IO.Path.GetTempPath()
        IO.Path.Combine(d, "TrackTimeReports")

    let generateBillingDetails (parameters : ReportParams)  =
        async {
            let bsDataResult = readReportDataAllUnbilled ()

            return
                "Billing Details Report",
                bsDataResult
                |> Result.map
                    (fun billingDetails ->
                        use report = new Report()
                        report.Load("BillingDetails.frx")
                        report.RegisterData([billingDetails],"BillingDetailsData")                       
                        ReportUtils.cleanTempReportsDir()
                        let outputFileName = ReportUtils.reportTempOutputFileName "BillingDetails"
                        report.SavePrepared(outputFileName)
                        outputFileName)
        }

