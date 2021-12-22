namespace TrackTime

open Avalonia.FuncUI.Components.Hosts
open Avalonia
open Avalonia.Threading
open System.Threading.Tasks
open Elmish
open Serilog
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Types
open System

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
          TotalCount: int64
          EnableGoButtons: bool }
        static member Empty() =
            { PageNo = 1
              ItemsPerPage = 30
              TotalPages = 1
              TotalCount = 0L
              EnableGoButtons = false }

        member this.CanGoFirst = this.EnableGoButtons && this.PageNo > 1
        member this.CanGoPrevious = this.EnableGoButtons && this.PageNo > 1
        member this.CanGoLast = this.EnableGoButtons && this.PageNo < this.TotalPages
        member this.CanGoNext = this.EnableGoButtons && this.PageNo < this.TotalPages

    type State =
        { Customers: Customer list
          CustomerPaging: Paging
          IncludeInactiveCustomers: bool
          SelectedCustomerId: CustomerId option
          WorkItems: WorkItem list
          WorkItemPaging: Paging
          IncludeCompletedWorkItems: bool
          SelectedWorkItemId: WorkItemId option
          TimeEntries: TimeEntry list
          TimeEntryPaging: Paging
          SelectedTimeEntryId: TimeEntryId option }

    type PagingButton =
        | First
        | Previous
        | Next
        | Last

    type Msg =
        | RequestLoadCustomers
        | LoadCustomersDone of Result<ListResults<Customer>, exn>
        | SelectCustomer of CustomerId option
        | RequestLoadWorkItemsForSelectedCustomerId
        | LoadWorkItemsDone of Result<ListResults<WorkItem>, exn>
        | SelectWorkItem of WorkItemId option
        | RequestLoadTimeEntriesForSelectedWorkItemId
        | LoadTimeEntriesDone of Result<ListResults<TimeEntry>, exn>
        | SelectTimeEntry of TimeEntryId option
        | ShowErrorMessage of string
        | AddCustomer
        | ReviewSelectedCustomer
        | ReviewCustomer of customerId: CustomerId
        | CustomerDialogClosed of Result<DialogResult, string>
        | AddWorkItem
        | ReviewSelectedWorkItem
        | ReviewWorkItem of workItemId: WorkItemId
        | WorkItemDialogClosed of Result<DialogResult, string>
        | AddTimeEntry
        | ReviewSelectedTimeEntry
        | ReviewTimeEntry of timeEntryId: TimeEntryId
        | TimeEntryDialogClosed of Result<DialogResult, string>
        | NothingMsg
        | IncludeInactiveCustomers of bool
        | IncludeCompletedWorkitems of bool
        | CustomerItemsPerPageChange of int
        | CustomerListCurrentPageChange of int
        | WorkItemItemsPerPageChange of int
        | WorkItemListCurrentPageChange of int
        | TimeEntryItemsPerPageChange of int
        | TimeEntryListCurrentPageChange of int
        | CustomerPagingButtonClicked of PagingButton
        | WorkItemPagingButtonClicked of PagingButton
        | TimeEntryPagingButtonClicked of PagingButton
        | EnableCustomerPaging of bool
        | EnableWorkItemPaging of bool
        | EnableTimeEntryPaging of bool

    let init () =
        let state =
            { Customers = List.empty
              CustomerPaging = Paging.Empty()
              IncludeInactiveCustomers = false
              SelectedCustomerId = None
              WorkItems = List.empty
              WorkItemPaging = Paging.Empty()
              IncludeCompletedWorkItems = false
              SelectedWorkItemId = None
              TimeEntries = List.empty
              TimeEntryPaging = Paging.Empty()
              SelectedTimeEntryId = None }

        state, Cmd.ofMsg RequestLoadCustomers

    let pagedCustomerRequest state =
        { CustomersRequest = { IncludeInactive = state.IncludeInactiveCustomers }
          PageRequest =
              { PageNo = state.CustomerPaging.PageNo
                ItemsPerPage = state.CustomerPaging.ItemsPerPage } }

    let loadCustomers state =
        let request = pagedCustomerRequest state
        AppDataService.getCustomers request

    let pagedWorkItemRequest (state: State) =
        { WorkItemsRequest =
              { CustomerId = state.SelectedCustomerId
                IncludeCompleted = state.IncludeCompletedWorkItems }
          PageRequest =
              { PageNo = state.WorkItemPaging.PageNo
                ItemsPerPage = state.WorkItemPaging.ItemsPerPage } }

    let pagedTimeEntryRequest (state: State) =
        { TimeEntriesRequest = { WorkItemId = state.SelectedWorkItemId }
          PageRequest =
              { PageNo = state.TimeEntryPaging.PageNo
                ItemsPerPage = state.TimeEntryPaging.ItemsPerPage } }

    let loadWorkItems (state: State) =
        let request = pagedWorkItemRequest state
        AppDataService.getWorkItems request

    let loadTimeEntries (state: State) =
        let request = pagedTimeEntryRequest state
        AppDataService.getTimeEntries request

    let updatePagingFromListResults paging listResults =
        let recs = uint listResults.TotalRecords
        let recsPP = uint paging.ItemsPerPage
        let pageCount = recs / recsPP

        let pc = if recs % recsPP > 0u then pageCount + 1u else pageCount

        { paging with
              TotalPages = int pc
              TotalCount = listResults.TotalRecords
              EnableGoButtons = true (*processing complete*)  }


    let checkCustomerIdselection (results: Customer seq) (customerId: CustomerId option) =
        match customerId with
        | Some id ->
            let idExistsInNewResults =
                results
                |> Seq.exists (fun (customer: Customer) -> customer.CustomerId = id)

            if idExistsInNewResults then customerId else None
        | None -> None

    let processCustomersResults state (customerResults: Result<ListResults<Customer>, exn>) : State * Cmd<_> =
        match customerResults with
        | Ok listResults ->
            let paging = updatePagingFromListResults state.CustomerPaging listResults

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
            let paging = updatePagingFromListResults state.WorkItemPaging listResults

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
                updatePagingFromListResults state.TimeEntryPaging listResults

            { state with
                  TimeEntries = listResults.Results
                  TimeEntryPaging = paging },
            Cmd.none
        | Error (e: exn) ->
            let errorMsgString =
                sprintf "Loading time entries failed with exception %s: %s" (e.GetType().Name) e.Message

            state, Cmd.ofMsg (ShowErrorMessage errorMsgString)

    let customerDialog id =
        let windowService = Globals.GetWindowService()
        windowService.OpenModelDialog(fun _ -> CustomerDialog(id))

    let workItemDialog customerId workItemId =
        let windowService = Globals.GetWindowService()
        windowService.OpenModelDialog(fun _ -> WorkItemDialog(customerId, workItemId))

    let timeEntryDialog workItemId timeEntryId =
        let windowService = Globals.GetWindowService()
        windowService.OpenModelDialog(fun _ -> TimeEntryDialog(workItemId, timeEntryId))

    let calculateNewPageNumberFromNewItemsPerPage currentPaging itemsPerPage (getItemIndex: unit -> int option) =
        let itemsPerPageWas = currentPaging.ItemsPerPage
        let pageNoWas = currentPaging.PageNo
        let recordsUpToAndIncludePageWas = (pageNoWas * itemsPerPageWas)

        let selectedIndex = getItemIndex ()

        let newPageNo =
            match selectedIndex with
            //if some then divide the index + 1 by the new items
            // per page and add one if not a clean division
            | Some index ->
                ((index + 1) / itemsPerPage)
                + (if ((index + 1) % itemsPerPage) = 0 then 0 else 1)
            //if none then divide all the records up to and including the current page by the new items
            // per page and add one if not a clean division
            | None ->
                (recordsUpToAndIncludePageWas / itemsPerPage)
                + (if (recordsUpToAndIncludePageWas % itemsPerPage) = 0 then
                       0
                   else
                       1)

        min newPageNo currentPaging.TotalPages

    let calculateNewPageNumberFromButtonClickedAndPaging paging button =
        match button with
        | First -> 1
        | Previous -> paging.PageNo - 1
        | Next -> paging.PageNo + 1
        | Last -> paging.TotalPages

    let update msg state : State * Cmd<_> =
        match msg with
        | RequestLoadCustomers -> state, Cmd.OfFunc.perform loadCustomers state LoadCustomersDone
        | LoadCustomersDone customerResults -> processCustomersResults state customerResults
        | SelectCustomer selectedCustId ->
            if state.SelectedCustomerId <> selectedCustId then
                { state with SelectedCustomerId = selectedCustId },
                match selectedCustId with
                | Some _ -> Cmd.ofMsg RequestLoadWorkItemsForSelectedCustomerId
                | None -> Cmd.none
            else
                state, Cmd.none
        | RequestLoadWorkItemsForSelectedCustomerId -> state, Cmd.OfFunc.perform loadWorkItems state LoadWorkItemsDone
        | LoadWorkItemsDone workItemsResults -> processWorkItemsResults state workItemsResults
        | SelectWorkItem selectedWorkItemId ->
            if state.SelectedWorkItemId <> selectedWorkItemId then
                { state with SelectedWorkItemId = selectedWorkItemId },
                match selectedWorkItemId with
                | Some _ -> Cmd.ofMsg <| RequestLoadTimeEntriesForSelectedWorkItemId
                | None -> Cmd.none
            else
                state, Cmd.none
        | RequestLoadTimeEntriesForSelectedWorkItemId -> state, Cmd.OfFunc.perform loadTimeEntries state LoadTimeEntriesDone
        | LoadTimeEntriesDone timeEntriesResults -> processTimeEntriesResults state timeEntriesResults
        | SelectTimeEntry selectedTimeEntryId ->
            if state.SelectedTimeEntryId <> selectedTimeEntryId then
                { state with SelectedTimeEntryId = selectedTimeEntryId },
                match selectedTimeEntryId with
                | Some _ -> Cmd.none
                | None -> Cmd.none
            else
                state, Cmd.none
        | ShowErrorMessage errorMessageString ->
            let windowService = Globals.GetWindowService()
            state, Cmd.OfTask.perform windowService.ShowErrorMsg errorMessageString (fun _ -> NothingMsg)
        | AddCustomer -> state, Cmd.OfTask.perform customerDialog None CustomerDialogClosed
        | ReviewSelectedCustomer ->
            state,
            match state.SelectedCustomerId with
            | Some selectedCustomerId -> Cmd.ofMsg <| ReviewCustomer selectedCustomerId
            | None -> Cmd.none
        | ReviewCustomer customerId -> state, Cmd.OfTask.perform customerDialog (Some customerId) CustomerDialogClosed
        | CustomerDialogClosed dialogResult ->
            match dialogResult with
            | Ok result ->
                match result with
                | DialogResult.Cancelled -> state, Cmd.none
                | _ -> state, Cmd.ofMsg RequestLoadCustomers //reload
            | Error eMsg -> state, Cmd.ofMsg (ShowErrorMessage $"Problem occured while adding/reviewing the customer. {eMsg}") //hopefully shouldn't occur
        | AddWorkItem ->
            let workItemId: WorkItemId option = None

            state,
            match state.SelectedCustomerId with
            | Some customerId -> Cmd.OfTask.perform (workItemDialog customerId) workItemId WorkItemDialogClosed
            | None -> Cmd.none //shouldn't happen
        | ReviewSelectedWorkItem ->
            state,
            match state.SelectedCustomerId with
            | Some customerId ->
                match state.SelectedWorkItemId with
                | Some workItemId -> Cmd.ofMsg <| ReviewWorkItem workItemId
                | None -> Cmd.none //shouldn't happen - button shouldn't be enabled
            | None -> Cmd.none //shouldn't happen - button shouldn't be enabled
        | ReviewWorkItem workItemId ->
            state,
            match state.SelectedCustomerId with
            | Some customerId -> Cmd.OfTask.perform (workItemDialog customerId) (Some workItemId) WorkItemDialogClosed
            | None -> Cmd.none //shouldn't happen
        | WorkItemDialogClosed dialogResult ->
            match dialogResult with
            | Ok result ->
                match result with
                | DialogResult.Cancelled -> state, Cmd.none
                | _ -> state, Cmd.ofMsg RequestLoadWorkItemsForSelectedCustomerId //reload
            | Error eMsg -> state, Cmd.ofMsg (ShowErrorMessage $"Problem occured while adding/reviewing the work item. {eMsg}") //hopefully shouldn't occur
        | AddTimeEntry ->
            let timeEntryId: TimeEntryId option = None

            state,
            match state.SelectedWorkItemId with
            | Some workItemId -> Cmd.OfTask.perform (timeEntryDialog workItemId) timeEntryId TimeEntryDialogClosed
            | None -> Cmd.none //shouldn't happen
        | ReviewSelectedTimeEntry ->
            state,
            match state.SelectedCustomerId with
            | Some customerId ->
                match state.SelectedTimeEntryId with
                | Some timeEntryId -> Cmd.ofMsg <| ReviewTimeEntry timeEntryId
                | None -> Cmd.none //shouldn't happen - button shouldn't be enabled
            | None -> Cmd.none //shouldn't happen - button shouldn't be enabled
        | ReviewTimeEntry timeEntryId ->
            state,
            match state.SelectedWorkItemId with
            | Some workItemId -> Cmd.OfTask.perform (timeEntryDialog workItemId) (Some timeEntryId) TimeEntryDialogClosed
            | None -> Cmd.none //shouldn't happen
        | TimeEntryDialogClosed dialogResult ->
            match dialogResult with
            | Ok result ->
                match result with
                | DialogResult.Cancelled -> state, Cmd.none
                | _ -> state, Cmd.ofMsg RequestLoadTimeEntriesForSelectedWorkItemId //reload
            | Error eMsg -> state, Cmd.ofMsg (ShowErrorMessage $"Problem occured while adding/reviewing the work item. {eMsg}") //hopefully shouldn't occur
        | IncludeInactiveCustomers includeInactiveCustomers ->
            { state with
                  IncludeInactiveCustomers = includeInactiveCustomers },
            Cmd.ofMsg <| RequestLoadCustomers

        | IncludeCompletedWorkitems includeCompleted ->
            let newState = { state with IncludeCompletedWorkItems = includeCompleted }

            newState, Cmd.ofMsg <| RequestLoadWorkItemsForSelectedCustomerId
        | NothingMsg _ -> state, Cmd.none
        | CustomerItemsPerPageChange itemsPerPage ->
            if state.CustomerPaging.ItemsPerPage <> itemsPerPage then
                let getSelectedCustomerIndex () =
                    match state.SelectedCustomerId with
                    | Some id -> List.tryFindIndex (fun (cust: Customer) -> cust.CustomerId = id) state.Customers
                    | None -> None

                let newPageNo =
                    calculateNewPageNumberFromNewItemsPerPage state.CustomerPaging itemsPerPage getSelectedCustomerIndex

                { state with
                      CustomerPaging = { state.CustomerPaging with ItemsPerPage = itemsPerPage } },
                Cmd.ofMsg <| CustomerListCurrentPageChange newPageNo
            else
                state, Cmd.none
        | CustomerListCurrentPageChange pageNo ->
            { state with
                  CustomerPaging = { state.CustomerPaging with PageNo = pageNo } },
            Cmd.ofMsg RequestLoadCustomers
        | WorkItemItemsPerPageChange itemsPerPage ->
            if state.WorkItemPaging.ItemsPerPage <> itemsPerPage then
                let getSelectedWorkItemIndex () =
                    match state.SelectedWorkItemId with
                    | Some id -> List.tryFindIndex (fun (cust: WorkItem) -> cust.WorkItemId = id) state.WorkItems
                    | None -> None

                let newPageNo =
                    calculateNewPageNumberFromNewItemsPerPage state.WorkItemPaging itemsPerPage getSelectedWorkItemIndex

                { state with
                      WorkItemPaging = { state.WorkItemPaging with ItemsPerPage = itemsPerPage } },
                Cmd.ofMsg <| WorkItemListCurrentPageChange newPageNo
            else
                state, Cmd.none
        | WorkItemListCurrentPageChange pageNo ->
            { state with
                  WorkItemPaging = { state.WorkItemPaging with PageNo = pageNo } },
            Cmd.ofMsg RequestLoadWorkItemsForSelectedCustomerId
        | TimeEntryItemsPerPageChange itemsPerPage ->
            if state.TimeEntryPaging.ItemsPerPage <> itemsPerPage then
                let getSelectedTimeEntryIndex () =
                    match state.SelectedTimeEntryId with
                    | Some id -> List.tryFindIndex (fun (cust: TimeEntry) -> cust.TimeEntryId = id) state.TimeEntries
                    | None -> None

                let newPageNo =
                    calculateNewPageNumberFromNewItemsPerPage state.TimeEntryPaging itemsPerPage getSelectedTimeEntryIndex

                { state with
                      TimeEntryPaging = { state.TimeEntryPaging with ItemsPerPage = itemsPerPage } },
                Cmd.ofMsg <| TimeEntryListCurrentPageChange newPageNo
            else
                state, Cmd.none
        | TimeEntryListCurrentPageChange pageNo ->
            { state with
                  TimeEntryPaging = { state.TimeEntryPaging with PageNo = pageNo } },
            Cmd.ofMsg RequestLoadTimeEntriesForSelectedWorkItemId
        | CustomerPagingButtonClicked button ->
            let newPageNo =
                calculateNewPageNumberFromButtonClickedAndPaging state.CustomerPaging button

            state,
            Cmd.batch [ Cmd.ofMsg <| EnableCustomerPaging false
                        Cmd.ofMsg <| CustomerListCurrentPageChange newPageNo ]
        | WorkItemPagingButtonClicked button ->
            let newPageNo =
                calculateNewPageNumberFromButtonClickedAndPaging state.WorkItemPaging button

            state,
            Cmd.batch [ Cmd.ofMsg <| EnableWorkItemPaging false
                        Cmd.ofMsg <| WorkItemListCurrentPageChange newPageNo ]
        | TimeEntryPagingButtonClicked button ->
            let newPageNo =
                calculateNewPageNumberFromButtonClickedAndPaging state.TimeEntryPaging button

            state,
            Cmd.batch [ Cmd.ofMsg <| EnableTimeEntryPaging false
                        Cmd.ofMsg <| TimeEntryListCurrentPageChange newPageNo ]
        | EnableCustomerPaging enabled ->
            { state with
                  CustomerPaging = { state.CustomerPaging with EnableGoButtons = false } },
            Cmd.none
        | EnableWorkItemPaging enabled ->
            { state with
                  WorkItemPaging = { state.WorkItemPaging with EnableGoButtons = false } },
            Cmd.none
        | EnableTimeEntryPaging enabled ->
            { state with
                  TimeEntryPaging = { state.TimeEntryPaging with EnableGoButtons = false } },
            Cmd.none

    type ItemsPerPageOption = { ItemsPerPage: int }


    let itemsPerPageOptionView (option: ItemsPerPageOption) =
        TextBlock.create [ TextBlock.classes [ "itemsPerPageOption" ]
                           TextBlock.text (string option.ItemsPerPage) ]

    let pagingControlView containingLayoutAttr (pageing: Paging) (itemsPerChangedMessage: int -> Msg) (pageButtonClicked: PagingButton -> Msg) (dispatch: Msg -> unit) =
        let itemsPerPageOptions =
            [ { ItemsPerPage = 10 }
              { ItemsPerPage = 20 }
              { ItemsPerPage = 30 }
              { ItemsPerPage = 50 } ]

        Grid.create [ Grid.classes [ "pagingContainer" ]
                      Grid.columnDefinitions "auto,auto,*,auto,auto,*,auto"
                      containingLayoutAttr
                      Grid.children [ Button.create [ Button.classes [ "pagingControl"; "first" ]
                                                      Grid.column 0
                                                      Button.isEnabled (pageing.CanGoFirst)
                                                      Button.onClick (fun _ -> dispatch <| pageButtonClicked PagingButton.First) ]
                                      Button.create [ Button.classes [ "pagingControl"; "previous" ]
                                                      Grid.column 1
                                                      Button.isEnabled (pageing.CanGoPrevious)
                                                      Button.onClick (fun _ -> dispatch <| pageButtonClicked PagingButton.Previous) ]
                                      TextBlock.create [ TextBlock.classes [ "pagingControl"; "currentPageNo" ]
                                                         TextBlock.text $"{pageing.PageNo} of {pageing.TotalPages}"
                                                         Grid.column 2 ]
                                      Button.create [ Button.classes [ "pagingControl"; "next" ]
                                                      Grid.column 3
                                                      Button.isEnabled (pageing.CanGoNext)
                                                      Button.onClick (fun _ -> dispatch <| pageButtonClicked PagingButton.Next) ]
                                      Button.create [ Button.classes [ "pagingControl"; "last" ]
                                                      Grid.column 4
                                                      Button.isEnabled (pageing.CanGoLast)
                                                      Button.onClick (fun _ -> dispatch <| pageButtonClicked PagingButton.Previous) ]
                                      TextBlock.create [ TextBlock.classes [ "pagingControl"; "itemsPerPageLabel" ]
                                                         Grid.column 5
                                                         TextBlock.text "Per Page" ]
                                      ComboBox.create [ ComboBox.classes [ "pagingControl"; "itemsPerPage" ]
                                                        Grid.column 6
                                                        ComboBox.dataItems itemsPerPageOptions
                                                        ComboBox.itemTemplate (
                                                            DataTemplateView<ItemsPerPageOption>.create ((fun perPageOption -> itemsPerPageOptionView perPageOption))
                                                        )
                                                        ComboBox.selectedItem (
                                                            List.tryFind (fun opt -> opt.ItemsPerPage = pageing.ItemsPerPage) itemsPerPageOptions
                                                            |> Option.map (fun o -> o :> obj)
                                                            |> Option.toObj
                                                        )
                                                        ComboBox.onSelectedItemChanged
                                                            (fun obj ->
                                                                let selectedOption =
                                                                    obj |> Option.ofObj |> (Option.map (fun o -> o :?> ItemsPerPageOption))

                                                                dispatch
                                                                <| match selectedOption with
                                                                   | Some opt -> itemsPerChangedMessage opt.ItemsPerPage
                                                                   | None -> NothingMsg) ] ] ]

    let customerListItemView (state: State) (customer: Customer) =
        DockPanel.create [ DockPanel.lastChildFill true
                           DockPanel.children [ Image.create [ DockPanel.dock Dock.Left
                                                               let imageClass =
                                                                   match customer.CustomerState with
                                                                   | CustomerState.Active -> "customerActive"
                                                                   | _ -> "customerInactive"

                                                               Image.classes [ "itemStateImage"; imageClass ] ]
                                                TextBlock.create [ TextBlock.text (customer.Name.Value)
                                                                   TextBlock.classes [ "customerName" ] ] ] ]

    let customerListView (state: State) (dispatch: Msg -> unit) =
        Grid.create [ Grid.rowDefinitions "auto,auto,auto,*,auto"
                      Grid.column 0
                      Grid.children [ TextBlock.create [ Grid.row 0
                                                         TextBlock.text "Customers"
                                                         TextBlock.classes [ "listTitle" ] ]
                                      Border.create [ Border.classes [ "listOptions" ]
                                                      Grid.row 1
                                                      Border.child (
                                                          StackPanel.create [ StackPanel.classes [ "listOptions" ]
                                                                              StackPanel.children [ CheckBox.create [ CheckBox.classes [ "listOption" ]
                                                                                                                      CheckBox.content "Include Inactive"
                                                                                                                      CheckBox.isChecked state.IncludeInactiveCustomers
                                                                                                                      CheckBox.onChecked
                                                                                                                          (fun _ -> dispatch <| IncludeInactiveCustomers true)
                                                                                                                      CheckBox.onUnchecked
                                                                                                                          (fun _ -> dispatch <| IncludeInactiveCustomers false) ] ] ]
                                                      ) ]
                                      Grid.create [ Grid.row 2
                                                    Grid.columnDefinitions "*,auto,auto"
                                                    Grid.children [ Button.create [ Button.classes [ "create"; "listOperationButton" ]
                                                                                    Grid.column 1
                                                                                    Button.onClick (fun _ -> AddCustomer |> dispatch) ]
                                                                    Button.create [ Button.classes [ "review"; "listOperationButton" ]
                                                                                    Grid.column 2
                                                                                    Button.isEnabled state.SelectedCustomerId.IsSome
                                                                                    Button.onClick (fun _ -> ReviewSelectedCustomer |> dispatch) ] ] ]
                                      ListBox.create [ Grid.row 3
                                                       ListBox.classes [ "itemList" ]
                                                       ListBox.dataItems state.Customers
                                                       ListBox.itemTemplate (DataTemplateView<Customer>.create ((fun customer -> customerListItemView state customer)))
                                                       ListBox.selectedItem (
                                                           state.SelectedCustomerId
                                                           |> Option.map (fun custId -> List.tryFind (fun (cust: Customer) -> cust.CustomerId = custId) state.Customers)
                                                           |> Option.flatten
                                                           |> Option.map (fun customer -> customer :> obj)
                                                           |> Option.toObj
                                                       )
                                                       ListBox.onSelectedItemChanged (
                                                           (fun item ->
                                                               item
                                                               |> Option.ofObj
                                                               |> Option.map (fun o -> o :?> Customer)
                                                               |> Option.map (fun cust -> cust.CustomerId)
                                                               |> SelectCustomer
                                                               |> dispatch)
                                                       )
                                                       ListBox.onDoubleTapped (fun _ -> ReviewSelectedCustomer |> dispatch) ]
                                      pagingControlView (Grid.row 4) state.CustomerPaging CustomerItemsPerPageChange CustomerPagingButtonClicked dispatch ] ]

    let getWorkItemFlagImages (workItem: WorkItem) =
        seq {
            yield
                (if workItem.IsCompleted then
                     Some "workItemCompleted"
                 elif workItem.DueDate.IsSome && workItem.DueDate.Value < DateTime.Today then
                     Some "workItemOverdue"
                 else
                     Some "workItemNotCompleted")

            yield
                (if workItem.IsBillable then
                     if workItem.IsCompleted then
                         if workItem.BeenBilled then
                             Some "workItemBeenBilled"
                         else
                             Some "workItemNotBeenBilled"
                     else
                         None
                 else
                     Some "workItemNotBillable")

            yield
                (if workItem.IsFixedPrice then
                     Some "workItemIsFixedPrice"
                 else
                     Some "workItemIsNotFixedPrice")
        }
        |> Seq.filter (fun classStr -> classStr.IsSome)
        |> Seq.map
            (fun classStr ->
                let imageView =
                    Image.create [ DockPanel.dock Dock.Left
                                   Image.classes [ classStr.Value; "workItemFlagImage" ] ]

                imageView :> IView)

    let workItemListItemView (state: State) (workItem: WorkItem) =
        DockPanel.create [ DockPanel.lastChildFill true
                           DockPanel.children (
                               List.append
                                   (Seq.toList (getWorkItemFlagImages workItem))
                                   [ TextBlock.create [ TextBlock.text (workItem.Title.Value)
                                                        TextBlock.classes [ "workItemName" ] ] ]
                           ) ]

    let workItemListView (state: State) (dispatch: Msg -> unit) =
        Grid.create [ Grid.rowDefinitions "auto,auto,auto,*,auto"
                      Grid.column 2
                      Grid.children [ TextBlock.create [ Grid.row 0
                                                         TextBlock.text "Work Items"
                                                         TextBlock.classes [ "listTitle" ] ]
                                      Border.create [ Border.classes [ "listOptions" ]
                                                      Grid.row 1
                                                      Border.child (
                                                          StackPanel.create [ StackPanel.classes [ "listOptions" ]
                                                                              StackPanel.children [ CheckBox.create [ CheckBox.classes [ "listOption" ]
                                                                                                                      CheckBox.content "Include Completed"
                                                                                                                      CheckBox.isChecked state.IncludeCompletedWorkItems
                                                                                                                      CheckBox.onChecked
                                                                                                                          (fun _ -> dispatch <| IncludeCompletedWorkitems true)
                                                                                                                      CheckBox.onUnchecked
                                                                                                                          (fun _ -> dispatch <| IncludeCompletedWorkitems false) ] ] ]
                                                      ) ]
                                      Grid.create [ Grid.row 2
                                                    Grid.columnDefinitions "*,auto,auto"
                                                    Grid.children [ Button.create [ Button.classes [ "create"; "listOperationButton" ]
                                                                                    Grid.column 1
                                                                                    Button.isEnabled state.SelectedCustomerId.IsSome
                                                                                    Button.onClick (fun _ -> AddWorkItem |> dispatch) ]
                                                                    Button.create [ Button.classes [ "review"; "listOperationButton" ]
                                                                                    Grid.column 2
                                                                                    Button.isEnabled (state.SelectedCustomerId.IsSome && state.SelectedWorkItemId.IsSome)
                                                                                    Button.onClick (fun _ -> ReviewSelectedWorkItem |> dispatch) ] ] ]
                                      ListBox.create [ Grid.row 3
                                                       ListBox.dataItems state.WorkItems
                                                       ListBox.itemTemplate (DataTemplateView<WorkItem>.create ((fun workItem -> workItemListItemView state workItem)))
                                                       ListBox.selectedItem (
                                                           state.SelectedWorkItemId
                                                           |> Option.map
                                                               (fun workItemId -> List.tryFind (fun (workItem: WorkItem) -> workItem.WorkItemId = workItemId) state.WorkItems)
                                                           |> Option.flatten
                                                           |> Option.map (fun workItem -> workItem :> obj)
                                                           |> Option.toObj
                                                       )
                                                       ListBox.onSelectedItemChanged (
                                                           (fun item ->
                                                               item
                                                               |> Option.ofObj
                                                               |> Option.map (fun obj -> (item :?> WorkItem))
                                                               |> Option.map (fun workItem -> workItem.WorkItemId)
                                                               |> SelectWorkItem
                                                               |> dispatch)
                                                       )
                                                       ListBox.onDoubleTapped (fun _ -> ReviewSelectedWorkItem |> dispatch) ]
                                      pagingControlView (Grid.row 4) state.WorkItemPaging WorkItemItemsPerPageChange WorkItemPagingButtonClicked dispatch ] ]

    let timeEntryListItemView (state: State) (timeEntry: TimeEntry) =
        DockPanel.create [ DockPanel.lastChildFill true
                           DockPanel.children [ TextBlock.create [ TextBlock.text (timeEntry.TimeStart.ToString())
                                                                   TextBlock.classes [ "timeEntryTimeStart" ]
                                                                   DockPanel.dock Dock.Right ]
                                                TextBlock.create [ TextBlock.text (timeEntry.Description.Value)
                                                                   TextBlock.classes [ "timeEntryDescription" ] ] ] ]

    let timeEntryListView (state: State) (dispatch: Msg -> unit) =
        Grid.create [ Grid.rowDefinitions "auto,auto,*,auto"
                      Grid.column 4
                      Grid.children [ TextBlock.create [ Grid.row 0
                                                         TextBlock.text "Time Entries"
                                                         TextBlock.classes [ "listTitle" ] ]
                                      Grid.create [ Grid.row 1
                                                    Grid.columnDefinitions "*,auto,auto"
                                                    Grid.children [ Button.create [ Button.classes [ "create"; "listOperationButton" ]
                                                                                    Grid.column 1
                                                                                    Button.isEnabled (state.SelectedCustomerId.IsSome && state.SelectedWorkItemId.IsSome)
                                                                                    Button.onClick (fun _ -> AddTimeEntry |> dispatch) ]
                                                                    Button.create [ Button.classes [ "review"; "listOperationButton" ]
                                                                                    Grid.column 2
                                                                                    Button.isEnabled (
                                                                                        state.SelectedCustomerId.IsSome
                                                                                        && state.SelectedWorkItemId.IsNone
                                                                                        && state.SelectedTimeEntryId.IsSome
                                                                                    )
                                                                                    Button.onClick (fun _ -> ReviewSelectedTimeEntry |> dispatch) ] ] ]
                                      ListBox.create [ Grid.row 2
                                                       ListBox.classes [ "itemList" ]
                                                       ListBox.dataItems state.TimeEntries
                                                       ListBox.itemTemplate (DataTemplateView<TimeEntry>.create ((fun timeEntry -> timeEntryListItemView state timeEntry)))
                                                       ListBox.selectedItem (
                                                           state.SelectedTimeEntryId
                                                           |> Option.map
                                                               (fun timeEntryId ->
                                                                   List.tryFind (fun (timeEntry: TimeEntry) -> timeEntry.TimeEntryId = timeEntryId) state.TimeEntries)
                                                           |> Option.flatten
                                                           |> Option.map (fun timeEntry -> timeEntry :> obj)
                                                           |> Option.toObj
                                                       )
                                                       ListBox.onSelectedItemChanged (
                                                           (fun item ->
                                                               item
                                                               |> Option.ofObj
                                                               |> Option.map (fun o -> o :?> TimeEntry)
                                                               |> Option.map (fun timeEntry -> timeEntry.WorkItemId)
                                                               |> SelectTimeEntry
                                                               |> dispatch)
                                                       )
                                                       ListBox.onDoubleTapped (fun _ -> ReviewSelectedTimeEntry |> dispatch) ]
                                      pagingControlView (Grid.row 4) state.TimeEntryPaging TimeEntryItemsPerPageChange TimeEntryPagingButtonClicked dispatch ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        Grid.create [ Grid.margin 10.0
                      Grid.columnDefinitions "*,8,*,8,*"
                      Grid.children [ customerListView state dispatch
                                      GridSplitter.create [ Grid.column 1 ]
                                      workItemListView state dispatch
                                      GridSplitter.create [ Grid.column 3 ]
                                      timeEntryListView state dispatch ] ]

    type Host() as this =
        inherit Hosts.HostControl()
        //let ownerWindow = Application.Current.ApplicationLifetime.

        do
            /// You can use `.mkProgram` to pass Commands around
            /// if you decide to use it, you have to also return a Command in the initFn
            /// (init, Cmd.none)
            /// you can learn more at https://elmish.github.io/elmish/basics.html

            Elmish.Program.mkProgram init update view
            |> Program.withHost this
            |> Program.withConsoleTrace
            |> Program.run
