
open FastReport
open FastReport.Data
open TrackTime
open System.Data

let initBillingSummary() = 
    printfn "\nBilling Summary"
    printfn "---------------"
    printfn ".. Loading some data."
    let bsDataResult = BillingSummaryReport.readReportDataAllUnbilled ()
    
    match bsDataResult with
    |Ok data -> 
        let rpt = new Report()
        rpt.Dictionary.RegisterBusinessObject([data],"BillingSummaryData", 4, true)
        rpt.Dictionary.Save("BillingSummary.frd")
        rpt.RegisterData([data],"BillingSummaryData", 4)
        rpt.Prepare() |> ignore
        rpt.Save("BillingSummary.frx")
        printfn "Success!!"
    |Error e -> printfn "** Error get billing summary data. %s" e.Message


let initBillingDetails() =
    printfn "\nBilling Details"
    printfn "---------------"
    printfn ".. Loading some data."
    let bsDataResult = BillingDetailsReport.readReportDataAllUnbilled()
    match bsDataResult with
    |Ok data ->
        let rpt = new Report()
        rpt.Dictionary.RegisterBusinessObject([data],"BillingDetailsData", 5, true)
        rpt.Dictionary.Save("BillingDetails.frd")
        rpt.RegisterData([data],"BillingDetailsData", 5)
        rpt.Prepare() |> ignore
        rpt.Save("BillingDetails.frx")
        printfn "Success!!"
    |Error e -> printfn "** Error get billing details data. %s" e.Message

// For more information see https://aka.ms/fsharp-console-apps
printfn "Initializing reports"
initBillingSummary()
initBillingDetails()
printfn "\n -- Done --"
    