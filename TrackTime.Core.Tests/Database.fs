namespace TrackTime.Core.Tests

open System
open System.IO
open System.Data
open System.Reflection
open FirebirdSql.Data.Isql
open TrackTime.DataInterfaces
open TrackTime


module Database =
    open Expecto
    open TrackTime.Core
    open TrackTime.DataModels
    open FirebirdSql.Data.FirebirdClient
    open Donald
    open TrackTime.AppDataDonaldSql

    let configfileTestingPathFactory () =
        let dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))

        Path.Combine(dir, "TrackTimeTest", "settings.json")

    let getConnStr = configfileTestingPathFactory >> Settings.getSettingsWithConfigPath >>  GetDbConnStrFromSettings
    let private connStr = getConnStr ()
    let connectDB () = getDbConnectionWithConnStr connStr

    do
        FbConnection.CreateDatabase(connStr, 8192, false, true)

        let scriptDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)

        let scriptFile = Path.Combine(scriptDir, "tracktimedb.sql")
        use streamReader = File.OpenText(scriptFile)
        let fs = new FbScript(streamReader.ReadToEnd())
        fs.Parse() |> ignore
        use conn = connectDB ()
        let batch = new FbBatchExecution(conn)
        batch.AppendSqlStatements(fs)
        batch.Execute()

    [<Tests>]
    let customerTests =
        testList
            "database"
            [ test "customers" {
                let customer =
                    { CustomerId = 0L
                      Name = CustomerName.Create "Test Customer"
                      Phone = PhoneNoOptional.None
                      Email = EmailAddressOptional.None
                      CustomerState = CustomerState.Active
                      Notes = None }

                let customer2 = { Customer.Empty with Name = CustomerName.Create "Test Customer 2" }

                //add customer and get one customer to test both adding and getting by id
                let addCustResult = addCustomerToDB connectDB customer

                let custId1 =
                    match addCustResult with
                    | Ok custId ->
                        let justAddedCustomer = getOneCustomerFromDB connectDB custId

                        match justAddedCustomer with
                        | Ok cust -> Expect.isTrue (cust.CustomerId = custId) "return customer id didn't match what was added"
                        | Error e -> failtestf "getOneCustomer shouldn't have returned an error %s %s" (e.GetType().Name) e.Message

                        custId
                    | Error e -> failtestf "error adding customer: %s %s\n" (e.GetType().Name) e.Message

                let addCustResult2 = addCustomerToDB connectDB customer2

                let custId2 =
                    match addCustResult2 with
                    | Ok c -> c
                    | Error e -> failtestf "error adding second customer: %s %s\n" (e.GetType().Name) e.Message


                //get list of customers
                let custListRequest: PagedCustomersRequest = { PageRequest = { PageNo = 1; ItemsPerPage = 10 }; CustomersRequest = { IncludeInactive = false } }

                let customerListJustActive = getCustomersFromDB connectDB custListRequest

                match customerListJustActive with
                | Ok custListResult ->
                    Expect.isGreaterThanOrEqual custListResult.TotalRecords 2 "returned customer list should have at least 2 records"
                    let custList = custListResult.Results
                    Expect.isFalse (Seq.isEmpty custList) "returned customer list should not be empty"
                    Expect.isSome (Seq.tryFind (fun (cust: Customer) -> cust.CustomerId = custId1) custList) "First customer added should have been in the returned list"
                    Expect.isSome (Seq.tryFind (fun (cust: Customer) -> cust.CustomerId = custId2) custList) "Second customer added should have been in the returned list"
                | Error e -> failtestf "Error returned from getCustomersFromDB %s %s\n" (e.GetType().Name) e.Message

                let changedCustomer1 = { customer with CustomerId = custId1; Name = CustomerName.Create "Changed Customer Name" }

                let updateCustomerResult = updateCustomerToDB connectDB changedCustomer1

                match updateCustomerResult with
                | Ok updated -> Expect.isTrue updated "Customer was not updated as expected."
                | Error e -> failtestf "Updating the customer record failed with an error: %s %s\n" (e.GetType().Name) e.Message

                //delete customers
                let customerToDelete = { customer2 with CustomerId = custId2 }
                let deleteCustomer2Result = deleteCustomerFromDB connectDB customerToDelete

                match deleteCustomer2Result with
                | Ok deleted -> Expect.isTrue deleted "Customer was not deleted as expected."
                | Error e -> failtestf "Deleting the customer record failed with an error: %s %s\n" (e.GetType().Name) e.Message
              }
              test "work items" {
                  //add another customer for the work items to be attached to
                  let customerForWorkItems = { Customer.Empty with Name = CustomerName.Create "Customer For WI's" }

                  let newCustomerIdResult = addCustomerToDB connectDB customerForWorkItems

                  let newCustId =
                      match newCustomerIdResult with
                      | Ok custId -> custId
                      | Error e -> failtest "Error occured adding another customer %s %s" (e.GetType().Name) e.Message

                  //lets add a work item and then retrieve it - test both adding and geting by id
                  let WI1 = { WorkItem.Empty with CustomerId = newCustId; Title = WorkItemTitle.Create "A Work item" }

                  let addWIResult = addWorkItemToDB connectDB WI1

                  let addedWorkItem =
                      match addWIResult with
                      | Ok workItemId ->
                          let justAddedWorkItemResult = getOneWorkItemFromDB connectDB workItemId

                          match justAddedWorkItemResult with
                          | Ok justAddedWorkItem ->
                              Expect.equal justAddedWorkItem.WorkItemId workItemId "returned workitem didn't have and Id that matched that returned by 'addWorkItemToDB'"
                              justAddedWorkItem
                          | Error e -> failtestf "getOneWorkItemFromDB failed wiith %s %s" (e.GetType().Name) e.Message
                      | Error e -> failtestf "addWorkItemToDB failed with %s %s" (e.GetType().Name) e.Message

                  let wiListRequest: PagedWorkItemsRequest =
                      { PageRequest = { PageNo = 1; ItemsPerPage = 10 }
                        WorkItemsRequest = { CustomerId = Some newCustId; IncludeCompleted = false } }

                  let workItemListIncomplete = getWorkItemsFromDB connectDB wiListRequest

                  match workItemListIncomplete with
                  | Ok wiListResult ->
                      let wiList = wiListResult.Results
                      Expect.isFalse (Seq.isEmpty wiList) "returned workItem list should not be empty"

                      Expect.isSome
                          (Seq.tryFind (fun (wi: WorkItem) -> wi.WorkItemId = addedWorkItem.WorkItemId) wiList)
                          "Previously added work item should have been in returned list"
                  | Error e -> failtestf "Error returned from getWorkItemsFromDB %s %s\n" (e.GetType().Name) e.Message

                  //check updating a work item
                  let changedWorkItem = { addedWorkItem with Title = WorkItemTitle.Create "Changed WorkItem Title" }

                  let updateWorkItemResult = updateWorkItemToDB connectDB changedWorkItem

                  match updateWorkItemResult with
                  | Ok updated -> Expect.isTrue updated "WorkItem was not updated as expected."
                  | Error e -> failtestf "Updating the workItem record failed with an error: %s %s\n" (e.GetType().Name) e.Message

                  //check deleting a work item
                  let deleteWorkItemResult = deleteWorkItemFromDB connectDB addedWorkItem

                  match deleteWorkItemResult with
                  | Ok deleted -> Expect.isTrue deleted "WorkItem was not deleted as expected."
                  | Error e -> failtestf "Updating the workItem record failed with an error: %s %s\n" (e.GetType().Name) e.Message
              }
              test "timeEntries" {
                  //add a customer first
                  let customerForWorkItems = { Customer.Empty with Name = CustomerName.Create "Customer For Time Entries" }

                  let newCustomerIdResult = addCustomerToDB connectDB customerForWorkItems

                  let newCustId =
                      match newCustomerIdResult with
                      | Ok custId -> custId
                      | Error e -> failtest "Error occured adding another customer %s %s" (e.GetType().Name) e.Message

                  //add a new work item for the  the time entry tests
                  let newWorkItem = { WorkItem.Empty with Title = WorkItemTitle.Create "New Title"; CustomerId = newCustId }

                  let newWorkItemIdResult = addWorkItemToDB connectDB newWorkItem

                  let newWorkItemId =
                      match newWorkItemIdResult with
                      | Ok id -> id
                      | Error e -> failtestf "Error occured adding a new work item. %s %s" (e.GetType().Name) e.Message

                  //test adding a time entry and getting by id
                  let timeEntry = { TimeEntry.Empty with WorkItemId = newWorkItemId; Description = TimeEntryDescription.Create "A Work item" }

                  let addTimeEntryResult = addTimeEntryToDB connectDB timeEntry

                  let addedTimeEntry =
                      match addTimeEntryResult with
                      | Ok timeEntryId ->
                          let justAddedTimeEntryResult = getOneTimeEntryFromDB connectDB timeEntryId

                          match justAddedTimeEntryResult with
                          | Ok justAddedTimeEntry ->
                              Expect.equal justAddedTimeEntry.TimeEntryId timeEntryId "returned workitem didn't have and Id that matched that returned by 'addTimeEntryToDB'"
                              justAddedTimeEntry
                          | Error e -> failtestf "getOneTimeEntryFromDB failed wiith %s %s" (e.GetType().Name) e.Message
                      | Error e -> failtestf "addTimeEntryToDB failed with %s %s" (e.GetType().Name) e.Message

                  //list the time entries
                  let timeEntryRequest: PagedTimeEntriesRequest = { PageRequest = { PageNo = 1; ItemsPerPage = 10 }; TimeEntriesRequest = { WorkItemId = Some newWorkItemId } }

                  let timeEntriesGetListResult = getTimeEntriesFromDB connectDB timeEntryRequest

                  match timeEntriesGetListResult with
                  | Ok timeEntriesListResult -> Expect.isFalse (Seq.isEmpty timeEntriesListResult.Results) "returned timeEntry list should not be empty"
                  | Error e -> failtestf "getTimeEntriesFromDB returned error %s %s" (e.GetType().Name) e.Message

                  //test the update of our added time entry
                  let changedTimeEntry = { addedTimeEntry with Description = TimeEntryDescription.Create "Changed TimeEntry Description" }

                  let updateTimeEntryResult = updateTimeEntryToDB connectDB changedTimeEntry

                  match updateTimeEntryResult with
                  | Ok updated -> Expect.isTrue updated "TimeEntry was not updated as expected."
                  | Error e -> failtestf "Updating the timeEntry record failed with an error: %s %s\n" (e.GetType().Name) e.Message

                  //test deletiig our time entry
                  let deleteTimeEntryResult = deleteTimeEntryFromDB connectDB addedTimeEntry

                  match deleteTimeEntryResult with
                  | Ok deleted -> Expect.isTrue deleted "TimeEntry was not deleted as expected."
                  | Error e -> failtestf "Updating the timeEntry record failed with an error: %s %s\n" (e.GetType().Name) e.Message
              } ]
