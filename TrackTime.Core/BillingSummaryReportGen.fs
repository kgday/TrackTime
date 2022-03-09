namespace TrackTime

open System
open DataModels


module BillingSummaryReportGen =
    type private BillingSummary_TimeEntry = { Description: string; Duration: TimeSpan option }

    type private BillingSummary_WorkItem =
        { WorkItemId: WorkItemId
          Title: string
          DateCreated: DateTime
          IsCompleted: bool
          IsFixedPriced: bool
          TimeEntries: BillingSummary_TimeEntry list
          TotalDuration: TimeSpan option }

    type private BillingSummary_Customer = { CustomerId: CustomerId; Name: string; WorkItems: BillingSummary_WorkItem list; TotalDuration: TimeSpan option }

    type private BillingSummary = { Title: string; Customers: BillingSummary_Customer list }

    let private convertResultsToReportStructure (results: BillingData list) =
        let timeEntryFromData (data: BillingData) : BillingSummary_TimeEntry =
            { BillingSummary_TimeEntry.Description = data.TimeEntryDescription
              BillingSummary_TimeEntry.Duration = data.TimeEntryDuration }

        let assignWorkItemTimeEntryListAndTotal ((workItem: BillingSummary_WorkItem), (subList: BillingData list)) =
            let timeEntryList = subList |> List.map timeEntryFromData

            let totalDuration =
                let durationsList =
                    timeEntryList
                    |> List.filter (fun te -> te.Duration.IsSome)
                    |> List.map (fun te -> te.Duration.Value.TotalMinutes)

                if List.isEmpty durationsList then
                    None
                else
                    durationsList |> List.sum |> TimeSpan.FromMinutes |> Some

            { workItem with
                  BillingSummary_WorkItem.TimeEntries = timeEntryList
                  BillingSummary_WorkItem.TotalDuration = totalDuration }

        let workItemFromData (data: BillingData) : BillingSummary_WorkItem =
            { BillingSummary_WorkItem.WorkItemId = data.WorkItemId
              BillingSummary_WorkItem.Title = data.WorkItemTitle
              BillingSummary_WorkItem.DateCreated = data.WorkItemDateCreate
              BillingSummary_WorkItem.IsCompleted = data.WorkItemIsCompleted
              BillingSummary_WorkItem.IsFixedPriced = data.WorkItemIsFixedPriced
              BillingSummary_WorkItem.TimeEntries = []
              BillingSummary_WorkItem.TotalDuration = None }

        let assignCustomerWorkItemListAndTotal ((customer: BillingSummary_Customer), (subList: BillingData list)) : BillingSummary_Customer =
            let workItemList =
                subList
                |> List.groupBy workItemFromData
                |> List.map assignWorkItemTimeEntryListAndTotal

            let totalDuration =
                let durationsList =
                    workItemList
                    |> List.filter (fun wi -> wi.TotalDuration.IsSome)
                    |> List.map (fun wi -> wi.TotalDuration.Value.TotalMinutes)

                if List.isEmpty durationsList then
                    None
                else
                    durationsList |> List.sum |> TimeSpan.FromMinutes |> Some

            { customer with
                  BillingSummary_Customer.WorkItems = workItemList
                  BillingSummary_Customer.TotalDuration = totalDuration }

        let customerFromData (data: BillingData) : BillingSummary_Customer =
            { BillingSummary_Customer.CustomerId = data.CustomerId
              BillingSummary_Customer.Name = data.CustomerName
              BillingSummary_Customer.WorkItems = []
              BillingSummary_Customer.TotalDuration = None }

        let customers =
            results
            |> List.groupBy customerFromData
            |> List.map assignCustomerWorkItemListAndTotal

        { BillingSummary.Title = "Billing Summary Report"; BillingSummary.Customers = customers }

    let private readReportDataAllUnbilled () =
        try
            let dataResult = AppDataService.getAllUnbilledBillingSummary ()

            match dataResult with
            | Ok dataList -> dataList |> convertResultsToReportStructure |> Ok
            | Error ex -> Error ex
        with
        | ex -> Error ex

    open QuestPDF.Drawing
    open QuestPDF.Fluent
    open QuestPDF.Helpers
    open QuestPDF.Infrastructure
    open ReportExtensions

    let private composeReportContentTable (data: BillingSummary) (container: IContainer) =
        container.Table
            (fun table ->
                table.ColumnsDefinition
                    (fun cols ->
                        cols.RelativeColumn()
                        cols.RelativeColumn()
                        cols.ConstantColumn(70f)
                        cols.ConstantColumn(65f)
                        cols.ConstantColumn(70f)
                        cols.RelativeColumn()
                        cols.ConstantColumn(40f))

                table.Header
                    (fun hdr ->
                        hdr.Cell().Text("Customer", Report.colHeaderStyle)
                        hdr.Cell().Text("Work Item", Report.colHeaderStyle)
                        hdr.Cell().AlignRight().Text("Date Created", Report.colHeaderStyle)
                        hdr.Cell().AlignCenter().Text("Completed", Report.colHeaderStyle)
                        hdr.Cell().AlignCenter().Text("Fixed Price", Report.colHeaderStyle)
                        hdr.Cell().Text("Time Entry Description", Report.colHeaderStyle)
                        hdr.Cell().AlignRight().Text("H:M", Report.colHeaderStyle)

                        hdr.Cell().ColumnSpan(7u).BorderBottom(0.75f).BorderColor(Colors.Black)
                        |> ignore)

                let mutable row = 1u

                data.Customers
                |> List.iter
                    (fun cust ->
                        table.Cell().Column(1u).Row(row).Text(cust.Name)

                        cust.WorkItems
                        |> List.iter
                            (fun wi ->
                                table.Cell().Row(row).Column(2u).Text(wi.Title)

                                table
                                    .Cell()
                                    .Row(row)
                                    .Column(3u)
                                    .AlignRight()
                                    .Text(wi.DateCreated.ToString("d"))

                                table
                                    .Cell()
                                    .Row(row)
                                    .Column(4u)
                                    .AlignCenter()
                                    .Text(if wi.IsCompleted then "Yes" else "No")

                                table
                                    .Cell()
                                    .Row(row)
                                    .Column(5u)
                                    .AlignCenter()
                                    .Text(if wi.IsFixedPriced then "Yes" else "No")

                                wi.TimeEntries
                                |> List.iter
                                    (fun te ->
                                        table.Cell().Row(row).Column(6u).Text(te.Description)

                                        let durationStr =
                                            te.Duration
                                            |> Option.map (fun d -> d.Formated())
                                            |> Option.defaultValue ""

                                        table.Cell().Row(row).Column(7u).AlignRight().Text(durationStr)

                                        row <- row + 1u)

                                if (wi.TimeEntries.Length > 1) && wi.TotalDuration.IsSome then
                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(2u)
                                        .ColumnSpan(5u)
                                        .AlignRight()
                                        //.BorderTop(0.05f)
                                        //.BorderColor(Colors.Black)
                                        .Text(
                                            $"Total for Work Item \'{wi.Title}\':",
                                            Report.totalHeaderStyle
                                        )

                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(7u)
                                        .AlignRight()
                                        .BorderTop(0.05f)
                                        .BorderColor(Colors.Grey.Medium)
                                        .Text(wi.TotalDuration.Value.Formated(), Report.totalValueStyle)

                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(2u)
                                        .ColumnSpan(6u)
                                        .BorderBottom(0.05f)
                                        .BorderColor(Colors.Grey.Medium)
                                    |> ignore


                                    row <- row + 1u)

                        if cust.TotalDuration.IsSome then
                            table
                                .Cell()
                                .Row(row)
                                .Column(1u)
                                .ColumnSpan(6u)
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
                                .Column(7u)
                                .AlignRight()
                                .BorderTop(0.05f)
                                .BorderColor(Colors.Grey.Medium)
                                .Text(cust.TotalDuration.Value.Formated(), Report.totalValueStyle)

                            table
                                .Cell()
                                .Row(row)
                                .Column(1u)
                                .ColumnSpan(7u)
                                .BorderBottom(0.05f)
                                .BorderColor(Colors.Grey.Medium)
                            |> ignore

                            row <- row + 1u
                        //table
                        //    .Cell()
                        //    .Row(row)
                        //    .Column(1u)
                        //    .ColumnSpan(7u)
                        //    .BorderTop(0.05f)
                        //    .BorderColor(Colors.Grey.Medium)
                        //    |> ignore
                        //row <- row + 1u
                        ))

    let generateBillingSummary (filename: string) =
        async {
            let bsDataResult = readReportDataAllUnbilled ()

            return
                bsDataResult
                |> Result.map
                    (fun billingSummary ->
                        let rptData = Report.ReportData<BillingSummary>.create "Billing Summary Report" "All Unbilled" billingSummary
                        let billingSummaryDocument = Report.StandardDocument(Report.PageOrientation.Landscape, rptData, composeReportContentTable)
                        billingSummaryDocument.GeneratePdf(filename)
                        filename)
        }
