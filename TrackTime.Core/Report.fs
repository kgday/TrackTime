namespace TrackTime

open System
//open QuestPDF.Drawing
//open QuestPDF.Fluent
//open QuestPDF.Helpers
//open QuestPDF.Infrastructure

module ReportExtensions =
    type TimeSpan with
        member this.Formated() =
            let hours = floor this.TotalHours |> int
            let hoursMinutes = [ hours.ToString("d2"); this.Minutes.ToString("d2") ]
            hoursMinutes |> String.concat ":"

        member this.ToHoursMinutes() =
            let hours = floor this.TotalHours |> int  
            let mins = this.Minutes
            hours, mins
module TimeSpan =
    open ReportExtensions
    let toHoursMinutes (ts:TimeSpan) = ts.ToHoursMinutes()



    (*
module internal Report =
    type PageOrientation =
        | Portrait
        | Landscape

    let footerStyle = TextStyle.Default.Size(10f)
    let headerStyle = TextStyle.Default.Size(10f)
    let titleStyle = TextStyle.Default.Bold().Size(14f)
    let subTitleStyle = TextStyle.Default.Size(11f)
    let colHeaderStyle = TextStyle.Default.Bold().Size(11f)
    let defaultStyle = TextStyle.Default.Size(11f)
    let totalHeaderStyle = defaultStyle.Size(10f).Bold()
    let totalValueStyle = defaultStyle.Bold()

    type ReportData<'data> =
        { Title: string
          SubTitle: string
          Data: 'data }
        static member create title subtitle data = { Title = title; SubTitle = subtitle; Data = data }


    type StandardDocument<'data>(orientation: PageOrientation, data: ReportData<'data>, composeContentTable: 'data -> IContainer -> unit) =

        let composeHeader (container: IContainer) =
            container
                .SkipOnce()
                .PaddingBottom(10f)
                .DefaultTextStyle(headerStyle)
                .Row(fun (row: RowDescriptor) ->
                    let leftItem = row.RelativeItem().AlignLeft()
                    let rightItem = row.RelativeItem().AlignRight()
                    leftItem.Text(data.Title)
                    rightItem.Text(data.SubTitle))

        let composeFooter (container: IContainer) =
            container
                //.PaddingTop(10f)
                .BorderTop(0.75f)
                .BorderColor(Colors.Black)
                .DefaultTextStyle(footerStyle)
                .Row(fun (row: RowDescriptor) ->
                    let leftItem = row.RelativeItem().AlignLeft()
                    let midItem = row.ConstantItem(100f).AlignCenter()
                    let rightItem = row.RelativeItem().AlignRight()
                    leftItem.Text($"Printed: {DateTime.Now:g}")

                    midItem.Text
                        (fun x ->
                            x.CurrentPageNumber()
                            x.Span(" / ")
                            x.TotalPages())

                    rightItem.Text("Track Your Time"))

        let composeFirstPageTitle (container: IContainer) =
            container
                .PaddingBottom(10f)
                .Column(fun (col: ColumnDescriptor) ->
                    col.Item().AlignCenter().Text(data.Title, titleStyle)
                    col.Item().AlignCenter().Text(data.SubTitle, subTitleStyle))


        let composeContent (container: IContainer) =
            container.Column
                (fun col ->
                    col.Item().Element(composeFirstPageTitle)
                    col.Item().Element(composeContentTable data.Data))

        let composePage (page: PageDescriptor) =
            page.MarginVertical 35f
            page.MarginHorizontal 45f
            page.DefaultTextStyle defaultStyle

            match orientation with
            | Portrait -> PageSizes.A4.Portrait()
            | Landscape -> PageSizes.A4.Landscape()
            |> page.Size

            page.Header() |> composeHeader
            page.Footer() |> composeFooter
            page.Content() |> composeContent

        interface IDocument with
            member this.Compose(container: IDocumentContainer) : unit = container.Page composePage |> ignore

            member this.GetMetadata() : DocumentMetadata =
                let meta = DocumentMetadata.Default
                meta.Author <- "Track Your Time"
                meta.Title <- data.Title
                meta
                *)
