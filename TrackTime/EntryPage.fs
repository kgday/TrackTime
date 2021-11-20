namespace TrackTime

open Avalonia.FuncUI.Components.Hosts
open Avalonia
open Avalonia.Threading
open System.Threading.Tasks
open Elmish
open Serilog

module EntryPage =
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Components
    open Avalonia.FuncUI.Elmish
    open Elmish

    open DataModels
    open DataInterfaces

    open MessageBox.Avalonia
    open Avalonia.FuncUI.DSL.MaterialIcon
    open Material.Icons.Avalonia
    open Material.Icons

    type Paging =
        { PageNo: int
          ItemsPerPage: int
          TotalPages: int
          TotalCount: int64 }
        static member Empty() =
            { PageNo = 1
              ItemsPerPage = 30
              TotalPages = 1
              TotalCount = 0L }

    type State =
        { Customers: Customer seq
          CustomerPaging: Paging
          IncludeInactiveCustomers: bool
          SelectedCustomerId: CustomerId option
          WorkItems: WorkItem seq
          WorkItemPaging: Paging
          IncludeCompletedWorkItems: bool
          SelectedWorkItemId: WorkItemId option
          TimeEntries: TimeEntry seq
          TimeEntryPaging: Paging
          SelectedTimeEntryId: TimeEntryId option }

    type Msg =
        | RequestLoadCustomers of int (*page number*)
        | LoadCustomersDone of Result<ListResults<Customer>, exn>
        | SelectCustomer of CustomerId option
        | RequestLoadWorkItemsForSelectedCustomerId of int (*page number*)
        | LoadWorkItemsDone of Result<ListResults<WorkItem>, exn>
        | SelectWorkItem of WorkItemId option
        | RequestLoadTimeEntriesForSelectedWorkItemId of int (*page number*)
        | LoadTimeEntriesDone of Result<ListResults<TimeEntry>, exn>
        | SelectTimeEntry of TimeEntryId option
        | ShowErrorMessage of string
        | AddCustomer
        | ReviewCustomer of customerId: CustomerId
        | CustomerDialogClosed of Result<Dialog.Result,string>
        | NothingMsg of unit

    let init =
        { Customers = Seq.empty
          CustomerPaging = Paging.Empty()
          IncludeInactiveCustomers = false
          SelectedCustomerId = None
          WorkItems = Seq.empty
          WorkItemPaging = Paging.Empty()
          IncludeCompletedWorkItems = false
          SelectedWorkItemId = None
          TimeEntries = Seq.empty
          TimeEntryPaging = Paging.Empty()
          SelectedTimeEntryId = None },
        Cmd.ofMsg (RequestLoadCustomers 1)

    let pagedCustomerRequest (state: State) (pageNo: int) =
        { CustomersRequest = { IncludeInactive = state.IncludeInactiveCustomers }
          PageRequest =
              { PageNo = pageNo
                ItemsPerPage = state.CustomerPaging.ItemsPerPage } }

    let loadCustomers (state: State) (pageNo: int) =
        let request = pagedCustomerRequest state pageNo
        AppDataService.getCustomers request

    let pagedWorkItemRequest (state: State) (pageNo: int) =
        { WorkItemsRequest =
              { CustomerId = state.SelectedCustomerId
                IncludeCompleted = state.IncludeCompletedWorkItems }
          PageRequest =
              { PageNo = pageNo
                ItemsPerPage = state.WorkItemPaging.ItemsPerPage } }

    let pagedTimeEntryRequest (state: State) (pageNo: int) =
        { TimeEntriesRequest = { WorkItemId = state.SelectedWorkItemId }
          PageRequest =
              { PageNo = pageNo
                ItemsPerPage = state.TimeEntryPaging.ItemsPerPage } }

    let loadWorkItems (state: State) (pageNo: int) =
        let request = pagedWorkItemRequest state pageNo
        AppDataService.getWorkItems request

    let loadTimeEntries (state: State) (pageNo: int) =
        let request = pagedTimeEntryRequest state pageNo
        AppDataService.getTimeEntries request

    let updatePagingFromTotal (paging: Paging) (totalRecords: int64) =
        let recs = uint totalRecords
        let recsPP = uint paging.ItemsPerPage
        let pageCount = recs / recsPP

        let pc =
            if recs % recsPP > 0u then
                pageCount + 1u
            else
                pageCount

        { paging with
              TotalPages = int pc
              TotalCount = totalRecords }

    let checkCustomerIdselection (results: Customer seq) (customerId: CustomerId option) =
        match customerId with
        | Some id ->
            let idExistsInNewResults =
                results
                |> Seq.exists (fun (customer: Customer) -> customer.CustomerId = id)

            if idExistsInNewResults then
                customerId
            else
                None
        | None -> None

    let processCustomersResults state (customerResults: Result<ListResults<Customer>, exn>) : State * Cmd<_> =
        match customerResults with
        | Ok listResults ->
            let paging =
                updatePagingFromTotal state.CustomerPaging listResults.TotalRecords

            //Is the existing selection still valid
            let selectedCustomerId =
                checkCustomerIdselection listResults.Results state.SelectedCustomerId

            { state with
                  Customers = listResults.Results
                  CustomerPaging = paging
                  SelectedCustomerId = selectedCustomerId },
            Cmd.none
        | Error (e: exn) ->
            let errorMsgString =
                sprintf "Loading customers failed with exception %s: %s" (e.GetType().Name) e.Message

            state, Cmd.ofMsg (ShowErrorMessage errorMsgString)

    let processWorkItemsResults state workItemResults : State * Cmd<_> =
        match workItemResults with
        | Ok listResults ->
            let paging =
                updatePagingFromTotal state.WorkItemPaging listResults.TotalRecords

            { state with
                  WorkItems = listResults.Results
                  WorkItemPaging = paging },
            Cmd.none
        | Error (e: exn) ->
            let errorMsgString =
                sprintf "Loading work items failed with exception %s: %s" (e.GetType().Name) e.Message

            state, Cmd.ofMsg (ShowErrorMessage errorMsgString)

    let processTimeEntriesResults state timeEntryResults : State * Cmd<_> =
        match timeEntryResults with
        | Ok listResults ->
            let paging =
                updatePagingFromTotal state.TimeEntryPaging listResults.TotalRecords

            { state with
                  TimeEntries = listResults.Results
                  TimeEntryPaging = paging },
            Cmd.none
        | Error (e: exn) ->
            let errorMsgString =
                sprintf "Loading time entries failed with exception %s: %s" (e.GetType().Name) e.Message

            state, Cmd.ofMsg (ShowErrorMessage errorMsgString)



    let customerDialog mainWindow id =
        async {
            let showDialogTask =
                Dispatcher.UIThread.InvokeAsync<Result<Dialog.Result,string>>
                    (fun _ ->
                        let dialog = CustomerDialog id
                        dialog.ShowDialog<Result<Dialog.Result,string>> mainWindow)

            let! returnResult = showDialogTask |> Async.AwaitTask

            return
                if obj.ReferenceEquals(returnResult, null) then  (*Should never happen*)
                    Ok Dialog.Cancelled
                else
                    returnResult
        }


    let update mainWindow msg state : State * Cmd<_> =
        match msg with
        | RequestLoadCustomers pageNo -> state, Cmd.OfFunc.perform (loadCustomers state) pageNo LoadCustomersDone
        | LoadCustomersDone customerResults -> processCustomersResults state customerResults
        | SelectCustomer selectedCustId ->
            { state with
                  SelectedCustomerId = selectedCustId },
            match selectedCustId with
            |Some _ -> Cmd.ofMsg (RequestLoadWorkItemsForSelectedCustomerId 1)
            |None -> Cmd.none
        | RequestLoadWorkItemsForSelectedCustomerId pageNo ->
            state, Cmd.OfFunc.perform (loadWorkItems state) pageNo LoadWorkItemsDone
        | LoadWorkItemsDone workItemsResults -> processWorkItemsResults state workItemsResults
        | SelectWorkItem selectedWorkItemId ->
            { state with
                  SelectedWorkItemId = selectedWorkItemId },
            match selectedWorkItemId with
            |Some _ -> Cmd.ofMsg (RequestLoadTimeEntriesForSelectedWorkItemId 1)
            |None -> Cmd.none
        | RequestLoadTimeEntriesForSelectedWorkItemId pageNo ->
            state, Cmd.OfFunc.perform (loadTimeEntries state) pageNo LoadTimeEntriesDone
        | LoadTimeEntriesDone timeEntriesResults -> processTimeEntriesResults state timeEntriesResults
        | SelectTimeEntry selectedTimeEntryId ->
            { state with
                  SelectedTimeEntryId = selectedTimeEntryId },
            match selectedTimeEntryId with
            |Some _ ->  Cmd.none
            |None ->  Cmd.none
        | ShowErrorMessage errorMessageString ->
            state, Cmd.OfTask.perform (Dialog.showErrorMessageDialog mainWindow) errorMessageString NothingMsg
        | AddCustomer -> state, Cmd.OfAsync.perform (customerDialog mainWindow) None CustomerDialogClosed
        | ReviewCustomer customerId ->
            state, Cmd.OfAsync.perform (customerDialog mainWindow) (Some customerId) CustomerDialogClosed
        | CustomerDialogClosed dialogResult ->
            match dialogResult with
            |Ok result ->
                match result with
                | Dialog.Cancelled -> state, Cmd.none
                | _ -> state, Cmd.ofMsg (RequestLoadCustomers state.CustomerPaging.PageNo) //reload
            |Error eMsg -> state, Cmd.ofMsg (ShowErrorMessage $"Problem occured while adding/reviewing the customer. {eMsg}") //hopefully shouldn't occur
        | NothingMsg _ -> state, Cmd.none

    let customerListItemView (state: State) (customer: Customer) =
        Grid.create
            [ Grid.columnDefinitions "*,auto"
              Grid.children
                  [ TextBlock.create
                      [ TextBlock.text (customer.Name.Value)
                        TextBlock.classes [ "CustomerName" ] ]
                    if state.IncludeInactiveCustomers then
                        match customer.CustomerState with
                        | CustomerState.Active ->
                            MaterialIcon.create
                                [ MaterialIcon.kind MaterialIconKind.PersonCheck
                                  MaterialIcon.classes [ "customerIconActive" ] ]
                        |_ ->
                            MaterialIcon.create
                                [ MaterialIcon.kind MaterialIconKind.PersonCancel
                                  MaterialIcon.classes [ "customerIconInActive" ] ]
                        
                    else
                        MaterialIcon.create
                            [ MaterialIcon.kind MaterialIconKind.Person
                              MaterialIcon.classes [ "customerIcon" ] ] ] ]

    let customerListView (state: State) (dispatch: Msg -> unit) =
        Grid.create
            [ Grid.rowDefinitions "auto,auto,*"
              Grid.column 0
              Grid.children
                  [ TextBlock.create
                      [ Grid.row 0
                        TextBlock.text "Customers" ]
                    Grid.create
                        [ Grid.row 1
                          Grid.columnDefinitions "*,auto,auto"
                          Grid.children
                              [ Button.create
                                  [ Button.content "Add"
                                    Grid.column 1
                                    Button.onClick (fun _ -> AddCustomer |> dispatch) ]
                                Button.create
                                    [ Button.content "Review"
                                      Grid.column 2
                                      Button.onClick
                                          (fun _ ->
                                              match state.SelectedCustomerId with
                                              | Some selectedCustomerId ->
                                                  (ReviewCustomer selectedCustomerId) |> dispatch
                                              | None -> NothingMsg() |> dispatch) ] ] ]
                    ListBox.create
                        [ Grid.row 2
                          ListBox.dataItems state.Customers
                          ListBox.itemTemplate (
                              DataTemplateView<Customer>.create ((fun customer -> customerListItemView state customer))
                          )
                          ListBox.onSelectedItemChanged ((fun item -> 
                            let selectedCustomer = if obj.ReferenceEquals(item,null) then None else Some (item :?> Customer).CustomerId
                            (SelectCustomer selectedCustomer) |> dispatch))] ] ]

    let workItemListView (state: State) (dispatch: Msg -> unit) =
        Grid.create
            [ Grid.rowDefinitions "auto,auto,*"
              Grid.column 2
              Grid.children
                  [ TextBlock.create
                      [ Grid.row 0
                        TextBlock.text "Work Items" ]
                    Grid.create
                        [ Grid.row 1
                          Grid.columnDefinitions "*,auto,auto"
                          Grid.children
                              [ Button.create
                                  [ Button.content "Add"
                                    Grid.column 1 ]
                                Button.create
                                    [ Button.content "Review"
                                      Grid.column 2 ] ] ]
                    ListBox.create [ Grid.row 2 ] ] ]

    let timeEntryListView (state: State) (dispatch: Msg -> unit) =
        Grid.create
            [ Grid.rowDefinitions "auto,auto,*"
              Grid.column 4
              Grid.children
                  [ TextBlock.create
                      [ Grid.row 0
                        TextBlock.text "Time Entries" ]
                    Grid.create
                        [ Grid.row 1
                          Grid.columnDefinitions "*,auto,auto"
                          Grid.children
                              [ Button.create
                                  [ Button.content "Add"
                                    Grid.column 1 ]
                                Button.create
                                    [ Button.content "Review"
                                      Grid.column 2 ] ] ]
                    ListBox.create [ Grid.row 2 ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        Grid.create
            [ Grid.margin 10.0
              Grid.columnDefinitions "*,8,*,8,*"
              Grid.children
                  [ customerListView state dispatch
                    GridSplitter.create [ Grid.column 1 ]
                    workItemListView state dispatch
                    GridSplitter.create [ Grid.column 3 ]
                    timeEntryListView state dispatch ] ]

//type Host() as this =
//    inherit Hosts.HostControl()
//    let ownerWindow = Application.Current.ApplicationLifetime.

//    do
//        /// You can use `.mkProgram` to pass Commands around
//        /// if you decide to use it, you have to also return a Command in the initFn
//        /// (init, Cmd.none)
//        /// you can learn more at https://elmish.github.io/elmish/basics.html

//        Elmish.Program.mkProgram init (update ownerWindow) view
//        |> Program.withHost this
//        |> Program.run
