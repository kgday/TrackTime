namespace TrackTime

open System
open DataModels


module BillingDetailsReportGen =
    type private BillingDetails_TimeEntryDetail = { TimeStart: DateTime; TimeEnd: DateTime option; Duration: TimeSpan option }
    type private BillingDetails_TimeEntrySummary = { Description: string; Details: BillingDetails_TimeEntryDetail list; TotalDuration: TimeSpan option }

    type private BillingDetails_WorkItem =
        { WorkItemId: WorkItemId
          Title: string
          DateCreated: DateTime
          IsCompleted: bool
          IsFixedPriced: bool
          TimeEntries: BillingDetails_TimeEntrySummary list
          TotalDuration: TimeSpan option }

    type private BillingDetails_Customer = { CustomerId: CustomerId; Name: string; WorkItems: BillingDetails_WorkItem list; TotalDuration: TimeSpan option }

    type private BillingDetails = { Customers: BillingDetails_Customer list }

    let private convertResultsToReportStructure (results: BillingData list) =
        let timeEntryDetailFromData (data: BillingData) : BillingDetails_TimeEntryDetail option =
            match data.TimeEntryTimeStart with
            | Some timeStart ->
                { BillingDetails_TimeEntryDetail.TimeStart = timeStart
                  BillingDetails_TimeEntryDetail.TimeEnd = data.TimeEntryTimeEnd
                  BillingDetails_TimeEntryDetail.Duration = data.TimeEntryDuration }
                |> Some
            | None -> None

        let assignTimeEntryDetailsListAndTotal ((timeEntry: BillingDetails_TimeEntrySummary), (subList: BillingData list)) : BillingDetails_TimeEntrySummary =
            let timeEntryDetails =
                subList
                |> List.map timeEntryDetailFromData
                |> List.filter (fun detailOption -> detailOption.IsSome)
                |> List.map (fun detailOption -> detailOption.Value)

            let totalDuration =
                let listDurationsMinutes =
                    timeEntryDetails
                    |> List.filter (fun te -> te.Duration.IsSome)
                    |> List.map (fun te -> te.Duration.Value.TotalMinutes)

                if List.isEmpty listDurationsMinutes then
                    None
                else
                    listDurationsMinutes |> List.sum |> TimeSpan.FromMinutes |> Some

            { timeEntry with BillingDetails_TimeEntrySummary.Details = timeEntryDetails; TotalDuration = totalDuration }

        let timeEntryFromData (data: BillingData) : BillingDetails_TimeEntrySummary =
            { BillingDetails_TimeEntrySummary.Description = data.TimeEntryDescription
              BillingDetails_TimeEntrySummary.Details = []
              TotalDuration = None }

        let assignWorkItemTimeEntryListAndTotal ((workItem: BillingDetails_WorkItem), (subList: BillingData list)) =
            let timeEntryList =
                subList
                |> List.groupBy timeEntryFromData
                |> List.map assignTimeEntryDetailsListAndTotal

            let totalDuration =
                let listDurationMinutes =
                    timeEntryList
                    |> List.filter (fun te -> te.TotalDuration.IsSome)
                    |> List.map (fun te -> te.TotalDuration.Value.TotalMinutes)

                if List.isEmpty listDurationMinutes then
                    None
                else
                    listDurationMinutes |> List.sum |> TimeSpan.FromMinutes |> Some

            { workItem with
                  BillingDetails_WorkItem.TimeEntries = timeEntryList
                  BillingDetails_WorkItem.TotalDuration = totalDuration }

        let workItemFromData (data: BillingData) : BillingDetails_WorkItem =
            { BillingDetails_WorkItem.WorkItemId = data.WorkItemId
              BillingDetails_WorkItem.Title = data.WorkItemTitle
              BillingDetails_WorkItem.DateCreated = data.WorkItemDateCreate
              BillingDetails_WorkItem.IsCompleted = data.WorkItemIsCompleted
              BillingDetails_WorkItem.IsFixedPriced = data.WorkItemIsFixedPriced
              BillingDetails_WorkItem.TimeEntries = []
              BillingDetails_WorkItem.TotalDuration = None }

        let assignCustomerWorkItemListAndTotal ((customer: BillingDetails_Customer), (subList: BillingData list)) : BillingDetails_Customer =
            let workItemList =
                subList
                |> List.groupBy workItemFromData
                |> List.map assignWorkItemTimeEntryListAndTotal

            let totalDuration =
                let durationLists =
                    workItemList
                    |> List.filter (fun wi -> wi.TotalDuration.IsSome)
                    |> List.map (fun wi -> wi.TotalDuration.Value.TotalMinutes)

                if List.isEmpty workItemList then
                    None
                else
                    durationLists |> List.sum |> TimeSpan.FromMinutes |> Some

            { customer with
                  BillingDetails_Customer.WorkItems = workItemList
                  BillingDetails_Customer.TotalDuration = totalDuration }

        let customerFromData (data: BillingData) : BillingDetails_Customer =
            { BillingDetails_Customer.CustomerId = data.CustomerId
              BillingDetails_Customer.Name = data.CustomerName
              BillingDetails_Customer.WorkItems = []
              BillingDetails_Customer.TotalDuration = None }

        let customers =
            results
            |> List.groupBy customerFromData
            |> List.map assignCustomerWorkItemListAndTotal

        { BillingDetails.Customers = customers }

    let private readReportDataAllUnbilled () =
        try
            AppDataService.getAllUnbilledBillingDetails ()
            |> Result.map (fun dataList -> dataList |> convertResultsToReportStructure)
        with
        | ex -> Error ex

    open QuestPDF.Drawing
    open QuestPDF.Fluent
    open QuestPDF.Helpers
    open QuestPDF.Infrastructure

    open ReportExtensions



    let private composeReportContentTable (data: BillingDetails) (container: IContainer) =
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

                                                let durationStr =
                                                    detail.Duration
                                                    |> Option.map (fun d -> d.Formated())
                                                    |> Option.defaultValue ""

                                                table.Cell().Row(row).Column(7u).AlignRight().Text(timeStartStr)
                                                table.Cell().Row(row).Column(8u).AlignRight().Text(timeEndStr)
                                                table.Cell().Row(row).Column(9u).AlignRight().Text(durationStr)

                                                row <- row + 1u)

                                        if (te.Details.Length > 1) && te.TotalDuration.IsSome then
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
                                                .Text(te.TotalDuration.Value.Formated(), Report.totalValueStyle)

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

                                if wi.TotalDuration.IsSome then
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
                                        .Text(wi.TotalDuration.Value.Formated(), Report.totalValueStyle)

                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(2u)
                                        .ColumnSpan(8u)
                                        .BorderBottom(0.05f)
                                        .BorderColor(Colors.Grey.Medium)

                                    |> ignore


                                    row <- row + 1u)

                        if cust.TotalDuration.IsSome then
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
                                .Text(cust.TotalDuration.Value.Formated(), Report.totalValueStyle)

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


    let generateBillingDetails (filename: string) =
        async {
            let bsDataResult = readReportDataAllUnbilled ()

            return
                bsDataResult
                |> Result.map
                    (fun billingDetails ->
                        let rptData = Report.ReportData<BillingDetails>.create "Billing Details Report" "All Unbilled" billingDetails
                        let billingDetailsDocument = Report.StandardDocument(Report.PageOrientation.Landscape, rptData, composeReportContentTable)

                        billingDetailsDocument.GeneratePdf(filename)
                        filename)
        }
