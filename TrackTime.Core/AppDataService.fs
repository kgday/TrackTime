namespace TrackTime

open DataInterfaces
open DataModels

//AppData is meant to be used within the application - simply supstitution for the app connectDB - for testing use the ..ForDB or ..ToDB .. withDB
module AppDataService =
    open AppDataDonaldSql

    let addCustomer = addCustomerToDB connectDB
    let addWorkItem = addWorkItemToDB connectDB
    let addTimeEntry = addTimeEntryToDB connectDB

    let updateCustomer = updateCustomerToDB connectDB
    let updateWorkItem = updateWorkItemToDB connectDB
    let updateTimeEntry = updateTimeEntryToDB connectDB

    let deleteCustomer = deleteCustomerFromDB connectDB
    let deleteWorkItem = deleteWorkItemFromDB connectDB
    let deleteTimeEntry = deleteTimeEntryFromDB connectDB

    let getOneCustomer = getOneCustomerFromDB connectDB
    let getOneWorkItem = getOneWorkItemFromDB connectDB
    let getOneTimeEntry = getOneTimeEntryFromDB connectDB

    let getCustomers = getCustomersFromDB connectDB
    //let getCustomerCount = getCustomerCountFromDB (connectDB())

    let getWorkItems = getWorkItemsFromDB connectDB
    //let getWorkItemCount = getWorkItemCountFromDB (connectDB())

    let getTimeEntries = getTimeEntriesFromDB connectDB
    //let getTimeEntryCount = getTimeEntryCountFromDB (connectDB())

    let getAllUnbilledBillingSummary() = getAllUnbilledBillingSummaryFromDb connectDB
    let getAllUnbilledBillingDetails() = getAllUnbilledBillingDetailsFromDb connectDB


