namespace TrackTime

open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.FuncUI
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open System.Data
open DataModels
open Avalonia.FuncUI.DSL
open System
open Avalonia.FuncUI.DSL.MaterialIcon


module Customer =

    type Mode =
        | Creating
        | Reviewing

    type State =
        { Mode: Mode
          Customer: Customer
          ErrorMessage: string option
          SaveEnabled : bool
          DeleteEnabled : bool}
        member this.CanSave = this.SaveEnabled && this.Customer.IsValidValue
        member this.CanDelete = this.DeleteEnabled && this.Mode <> Mode.Creating

        member this.StatusMessages =
            match this.ErrorMessage with
            | Some msg -> seq { msg }
            | None -> this.Customer.ErrorMsgs


    type Msg =
        | LoadForReview of customerId: CustomerId
        | Loaded of Result<Customer, exn>
        | CloseDialog of Result<DialogResult, string>
        | NameChanged of string
        | PhoneNoChanged of string
        | EmailChanged of string
        | StateChanged of CustomerState
        | NotesChanged of string
        | SaveRequested
        | DeleteConfirmationRequested
        | DeleteConfirmationReceived of bool
        | ErrorMessage of string
        | ClearError
        | CancelRequested
        | NoneMsg
        | SaveButtonClick
        | SetSaveEnabled of bool
        | SetDeleteEnabled of bool
        | DeleteButtonClick

    let init () =
        { Mode = Creating
          Customer = Customer.Empty
          ErrorMessage = None
          DeleteEnabled = true
          SaveEnabled = true},
        Cmd.none

    let loadCustomer id = AppDataService.getOneCustomer id
    let closeDialog (host: HostWindow) (result: Result<DialogResult, string>) = host.Close(result)
    let addCustomer customer = AppDataService.addCustomer customer
    let updateCustomer customer = AppDataService.updateCustomer customer
    let deleteCustomer customer = AppDataService.deleteCustomer customer


    let processSaveRequest state =
        match state.Mode with
        | Reviewing ->
            match updateCustomer state.Customer with
            | Ok updated ->
                if updated then
                    Ok DialogResult.Updated
                else
                    Error "Unknown error occured updating the database"
            | Error e -> Error $"Error updating the database. {e.Message}"
        | _ ->
            match AppDataService.addCustomer state.Customer with
            | Ok newId -> Ok <| DialogResult.Created newId
            | Error e -> Error $"Error adding to the database. {e.Message}"

    let processSaveResult (result: Result<DialogResult, string>) =
        match result with
        | Ok result -> result |> Ok |> CloseDialog
        | Error eMsg -> eMsg |> ErrorMessage


    let confirmDeletion window (state: State) =
        Dialog.showConfirmationMessageDialog window $"Are you sure you wish to delete customer {state.Customer.Name}?"

    let processDeleteResult (result: Result<bool, exn>) =
        match result with
        | Ok deleted ->
            if deleted then
                DialogResult.Deleted |> Ok |> CloseDialog
            else
                ErrorMessage "Unknown error occured deleting from the database"
        | Error e -> e.Message |> ErrorMessage

    let delay (timeoutMS: int) =
        task { return! System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(timeoutMS)) }

    let update (host: HostWindow) (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | LoadForReview customerId -> state, Cmd.OfFunc.perform loadCustomer customerId Loaded
        | Loaded customerResult ->
            match customerResult with
            | Ok cust -> { state with Customer = cust; Mode = Reviewing }, Cmd.none
            | Error (e: exn) ->
                state,
                $"Error loading the customer. {e.Message}"
                |> Error
                |> CloseDialog
                |> Cmd.ofMsg
        | CloseDialog withResult -> state, Cmd.OfFunc.perform (closeDialog host) withResult (fun () -> NoneMsg)
        | NameChanged customer ->
            { state with
                  Customer = { state.Customer with Name = CustomerName.Create customer } },
            Cmd.none
        | EmailChanged emailStr ->
            { state with
                  Customer =
                      { state.Customer with
                            Email = EmailAddressOptional.Create <| Some emailStr } },
            Cmd.none
        | PhoneNoChanged phoneNoStr ->
            { state with
                  Customer =
                      { state.Customer with
                            Phone = PhoneNoOptional.Create <| Some phoneNoStr } },
            Cmd.none
        | NotesChanged notes ->
            { state with
                  Customer =
                      { state.Customer with
                            Notes =
                                (if notes |> String.IsNullOrWhiteSpace then
                                     None
                                 else
                                     Some notes) } },
            Cmd.none
        | StateChanged custState ->
            { state with
                  Customer = { state.Customer with CustomerState = custState } },
            Cmd.none
        | SaveRequested -> state, Cmd.OfFunc.perform processSaveRequest state processSaveResult
        | DeleteConfirmationRequested -> state, Cmd.OfTask.perform (confirmDeletion host) state DeleteConfirmationReceived
        | DeleteConfirmationReceived confirmed ->
            if confirmed then
                state, Cmd.OfFunc.perform deleteCustomer state.Customer processDeleteResult
            else
                state, Cmd.none
        | ErrorMessage errorMsg -> { state with ErrorMessage = Some errorMsg; SaveEnabled = false }, Cmd.OfTask.perform delay 10000 (fun _ -> ClearError)
        | ClearError -> { state with ErrorMessage = None}, Cmd.batch [ Cmd.ofMsg <| SetSaveEnabled true; Cmd.ofMsg <| SetDeleteEnabled true]
        | CancelRequested -> state, DialogResult.Cancelled |> Ok |> CloseDialog |> Cmd.ofMsg
        | NoneMsg -> state, Cmd.none
        | SaveButtonClick -> state, Cmd.batch [Cmd.ofMsg <| SetSaveEnabled false; Cmd.ofMsg <| SaveRequested]
        | SetSaveEnabled enabled -> {state with SaveEnabled = enabled}, Cmd.none
        | SetDeleteEnabled enabled -> {state with DeleteEnabled = enabled}, Cmd.none
        | DeleteButtonClick -> state, Cmd.batch [Cmd.ofMsg <| SetDeleteEnabled false; Cmd.ofMsg <| DeleteConfirmationRequested]


    let view (state: State) (dispatch: Msg -> unit) =
        Grid.create [ Grid.classes [ "editDialogMainGrid" ]
                      Grid.columnDefinitions "2*,5*"
                      Grid.rowDefinitions "auto,auto,auto,auto,*,auto,auto"
                      Grid.children [ TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 0
                                                         Grid.column 0
                                                         TextBlock.text "Name" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "customerNameEdit" ]
                                                       Grid.row 0
                                                       Grid.column 1
                                                       TextBox.text (state.Customer.Name.Value)
                                                       TextBox.onTextChanged (NameChanged >> dispatch) ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 1
                                                         Grid.column 0
                                                         TextBlock.text "Phone" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "customerPhoneEdit" ]
                                                       Grid.row 1
                                                       Grid.column 1
                                                       TextBox.text <| state.Customer.Phone.ToString()
                                                       TextBox.onTextChanged (PhoneNoChanged >> dispatch) ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 2
                                                         Grid.column 0
                                                         TextBlock.text "Email" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "customerEmailEdit" ]
                                                       Grid.row 2
                                                       Grid.column 1
                                                       TextBox.text <| state.Customer.Email.ToString()
                                                       TextBox.onTextChanged (EmailChanged >> dispatch) ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 3
                                                         Grid.column 0
                                                         TextBlock.text "Customer Is" ]
                                      StackPanel.create [ StackPanel.classes [ "editField"; "customerStateEdit" ]
                                                          Grid.row 3
                                                          Grid.column 1
                                                          StackPanel.children [ RadioButton.create [ RadioButton.groupName "customerState"
                                                                                                     RadioButton.classes [ "customerState"
                                                                                                                           "customerStateActive" ]
                                                                                                     RadioButton.content "Active"
                                                                                                     RadioButton.isChecked (state.Customer.CustomerState = CustomerState.Active)
                                                                                                     RadioButton.onChecked (
                                                                                                         (fun _ -> dispatch (StateChanged CustomerState.Active)),
                                                                                                         OnChangeOf(state.Customer.CustomerState)
                                                                                                     ) ]
                                                                                RadioButton.create [ RadioButton.groupName "customerState"
                                                                                                     RadioButton.classes [ "customerState"
                                                                                                                           "customerStateActive" ]
                                                                                                     RadioButton.content "Inactive"
                                                                                                     RadioButton.isChecked (state.Customer.CustomerState = CustomerState.InActive)
                                                                                                     RadioButton.onChecked (
                                                                                                         (fun _ -> dispatch (StateChanged CustomerState.InActive)),
                                                                                                         OnChangeOf(state.Customer.CustomerState)
                                                                                                     ) ] ] ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 4
                                                         Grid.column 0
                                                         TextBlock.text "Notes" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "notesEdit" ]
                                                       Grid.row 4
                                                       Grid.column 1
                                                       TextBox.text (
                                                           match state.Customer.Notes with
                                                           | Some notes -> notes
                                                           | None -> ""
                                                       )
                                                       TextBox.onTextChanged (NotesChanged >> dispatch) ]
                                      if (not state.Customer.IsValidValue)  || state.ErrorMessage.IsSome then
                                          Border.create [ Border.classes [ "error" ]
                                                          Border.row 5
                                                          Border.column 1
                                                          Border.child (
                                                              StackPanel.create [ StackPanel.classes [ "error" ]
                                                                                  StackPanel.children [ for msg in state.StatusMessages do
                                                                                                            yield
                                                                                                                TextBlock.create [ TextBlock.classes [ "error" ]
                                                                                                                                   TextBlock.text msg ] ] ]
                                                          ) ]
                                      StackPanel.create [ Grid.row 6
                                                          Grid.column 0
                                                          Grid.columnSpan 2
                                                          StackPanel.orientation Layout.Orientation.Horizontal
                                                          StackPanel.classes [ "bottomButtonPanel" ]
                                                          StackPanel.children [ Button.create [ Button.classes [ "crudButtons"; "delete" ]
                                                                                                Button.isVisible (state.CanDelete)
                                                                                                Button.onClick (fun _ -> dispatch DeleteButtonClick) ]
                                                                                Button.create [ Button.classes [ "crudButtons"; "save" ]
                                                                                                Button.isEnabled state.CanSave
                                                                                                Button.onClick (fun _ -> dispatch SaveButtonClick) ]
                                                                                Button.create [ Button.classes [ "crudButtons"; "cancel" ]
                                                                                                Button.isEnabled true
                                                                                                Button.onClick (fun _ -> dispatch CancelRequested) ] ]

                                                           ] ] ]
open System.Reactive
open System.Reactive.Linq

type CustomerDialog(customerId: CustomerId option) as this =
    inherit HostWindow()

    do
        base.Title <- "Customer"
        base.Width <- 400.0
        base.Height <- 300.0
        base.WindowStartupLocation <- WindowStartupLocation.CenterOwner
        base.MinWidth <- 400.0
        base.MinHeight <- 300.0

        let subscribeActivated _ =
            match customerId with
            |None -> Cmd.none
            |Some custId -> 
                let sub dispatch =
                    this.Activated.Take(1).Subscribe(fun _ -> custId |>  Customer.LoadForReview |> dispatch )
                    |>  ignore
                Cmd.ofSub sub

        //let init () = Customer.init customerId

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        let update = Customer.update this
        let init = Customer.init
        let view = Customer.view

        Elmish.Program.mkProgram init update view
        |> Program.withHost this
        |> Program.withSubscription subscribeActivated
        //|> Program.withConsoleTrace
        |> Program.run
