namespace TrackTime

open System
open DataModels


module BillingSummaryReport =
    open ReportExtensions

    type BillingSummary_TimeEntry =
        { Description: string
          DurationHrs: double }

    type BillingSummary_WorkItem =
        { WorkItemId: WorkItemId
          Title: string
          DateCreated: DateTime
          IsCompleted: bool
          IsFixedPriced: bool
          TimeEntries: BillingSummary_TimeEntry list
          TotalDurationHrs: double }

    type BillingSummary_Customer =
        { CustomerId: CustomerId
          Name: string
          WorkItems: BillingSummary_WorkItem list
          TotalDurationHrs: double }

    type BillingSummary =
        { RptSubTitle: string
          Customers: BillingSummary_Customer list }

    let convertResultsToReportStructure rptSubTitle (results: BillingData list) =
        let timeEntryFromData (data: BillingData) : BillingSummary_TimeEntry =
            let totalHours =
                data.TimeEntryDuration
                |> Option.map (fun d -> d.TotalHours)
                |> Option.defaultValue 0.0

            { BillingSummary_TimeEntry.Description = data.TimeEntryDescription
              BillingSummary_TimeEntry.DurationHrs = totalHours }

        let assignWorkItemTimeEntryListAndTotal ((workItem: BillingSummary_WorkItem), (subList: BillingData list)) =
            let timeEntryList = subList |> List.map timeEntryFromData

            let totalDurationHrs =
                let DurationHrsList =
                    timeEntryList
                    |> List.map (fun te -> te.DurationHrs)

                if List.isEmpty DurationHrsList then
                    0.0
                else
                    DurationHrsList |> List.sum

            { workItem with
                BillingSummary_WorkItem.TimeEntries = timeEntryList
                BillingSummary_WorkItem.TotalDurationHrs = totalDurationHrs }

        let workItemFromData (data: BillingData) : BillingSummary_WorkItem =
            { BillingSummary_WorkItem.WorkItemId = data.WorkItemId
              BillingSummary_WorkItem.Title = data.WorkItemTitle
              BillingSummary_WorkItem.DateCreated = data.WorkItemDateCreate
              BillingSummary_WorkItem.IsCompleted = data.WorkItemIsCompleted
              BillingSummary_WorkItem.IsFixedPriced = data.WorkItemIsFixedPriced
              BillingSummary_WorkItem.TimeEntries = []
              BillingSummary_WorkItem.TotalDurationHrs = 0 }

        let assignCustomerWorkItemListAndTotal
            (
                (customer: BillingSummary_Customer),
                (subList: BillingData list)
            ) : BillingSummary_Customer =
            let workItemList =
                subList
                |> List.groupBy workItemFromData
                |> List.map assignWorkItemTimeEntryListAndTotal

            let totalDurationHrs =
                let DurationHrsList =
                    workItemList
                    |> List.map (fun wi -> wi.TotalDurationHrs)

                if List.isEmpty DurationHrsList then
                    0.0
                else
                    DurationHrsList |> List.sum

            { customer with
                BillingSummary_Customer.WorkItems = workItemList
                BillingSummary_Customer.TotalDurationHrs = totalDurationHrs }

        let customerFromData (data: BillingData) : BillingSummary_Customer =
            { BillingSummary_Customer.CustomerId = data.CustomerId
              BillingSummary_Customer.Name = data.CustomerName
              BillingSummary_Customer.WorkItems = []
              BillingSummary_Customer.TotalDurationHrs = 0 }

        let customers =
            results
            |> List.groupBy customerFromData
            |> List.map assignCustomerWorkItemListAndTotal

        { BillingSummary.RptSubTitle = rptSubTitle
          BillingSummary.Customers = customers }

    let readReportDataAllUnbilled () =
        try
            let dataResult = AppDataService.getAllUnbilledBillingSummary ()

            match dataResult with
            | Ok dataList ->
                dataList
                |> convertResultsToReportStructure "All Unbilled"
                |> Ok
            | Error ex -> Error ex
        with
        | ex -> Error ex

    (*
    open QuestPDF.Drawing
    open QuestPDF.Fluent
    open QuestPDF.Helpers
    open QuestPDF.Infrastructure
    open ReportExtensions

    let  composeReportContentTable (data: BillingSummary) (container: IContainer) =
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

                                        let DurationHrstr =
                                            te.DurationHrs
                                            |> Option.map (fun d -> d.Formated())
                                            |> Option.defaultValue ""

                                        table.Cell().Row(row).Column(7u).AlignRight().Text(DurationHrstr)

                                        row <- row + 1u)

                                if (wi.TimeEntries.Length > 1) && wi.TotalDurationHrs.IsSome then
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
                                        .Text(wi.TotalDurationHrs.Value.Formated(), Report.totalValueStyle)

                                    table
                                        .Cell()
                                        .Row(row)
                                        .Column(2u)
                                        .ColumnSpan(6u)
                                        .BorderBottom(0.05f)
                                        .BorderColor(Colors.Grey.Medium)
                                    |> ignore


                                    row <- row + 1u)

                        if cust.TotalDurationHrs.IsSome then
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
                                .Text(cust.TotalDurationHrs.Value.Formated(), Report.totalValueStyle)

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
        *)

    type ReportParams = | All

    open FastReport
    open FastReport.ReportBuilder
    open FastReport.Export.PdfSimple
    ///Generates the result and saves the report
    let generateBillingSummary (parameters: ReportParams) =
        async {
            let bsDataResult = readReportDataAllUnbilled ()

            return
                "Billing Summary Report",
                bsDataResult
                |> Result.map (fun billingSummary ->
                    use report = new Report()
                    report.Load("BillingSummary.frx")
                    report.RegisterData([ billingSummary ], "BillingSummaryData")
                    report.Prepare() |> ignore
                    ReportUtils.cleanTempReportsDir()
                    let outputFileName = ReportUtils.reportTempOutputFileName "BillingSummary"
                    report.SavePrepared(outputFileName)
                    outputFileName)
        }






//    filename

//readReportDataAllUnbilled ()
//|> Result.map
//    (fun billingSummary -> billingSummary |> buildReport)
