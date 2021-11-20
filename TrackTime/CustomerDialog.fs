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
open Microsoft.EntityFrameworkCore.Metadata


module Customer =

    type Mode =
        | Creating
        | Reviewing

    type State =
        { Mode: Mode
          Customer: Customer
          CanSave: bool
          StatusMessages: string seq }

    type Msg =
        | LoadForReview of customerId: CustomerId
        | Loaded of Result<Customer option, exn>
        | CloseDialog of Result<Dialog.Result, string>
        | NameChanged of string
        | PhoneNoChanged of string
        | EmailChanged of string
        | StateChanged of CustomerState
        | NotesChanged of string
        | SaveRequested
        | DeleteConfirmationRequested
        | DeleteConfirmationReceived of bool
        | CancelRequested
        | ErrorMessage of string
        | NoneMsg

    let init (customerId: CustomerId option) =
        { Mode = Creating
          Customer = Customer.Empty
          CanSave = false
          StatusMessages = [] },
        match customerId with
        | Some id -> Cmd.ofMsg (LoadForReview id)
        | None -> Cmd.none

    let loadCustomer id = AppDataService.getOneCustomer id
    let closeDialog (host: HostWindow) (result: Result<Dialog.Result, string>) = host.Close(result)
    let addCustomer customer = AppDataService.addCustomer customer
    let updateCustomer customer = AppDataService.updateCustomer customer
    let deleteCustomer customer = AppDataService.deleteCustomer customer


    let processSaveRequest state =
        match state.Mode with
        | Reviewing ->
            match updateCustomer state.Customer with
            | Ok updated ->
                if updated then
                    Ok Dialog.Result.Updated
                else
                    Error "Unknown error occured updating the database"
            | Error e -> Error $"Error updating the database. {e.Message}"
        | _ ->
            match AppDataService.addCustomer state.Customer with
            | Ok _ -> Ok Dialog.Result.Created
            | Error e -> Error $"Error adding to the database. {e.Message}"

    let processSaveResult (result: Result<Dialog.Result, string>) =
        match result with
        | Ok result -> result |> Ok |> CloseDialog
        | Error eMsg -> eMsg |> ErrorMessage


    let confirmDeletion window (state: State) =
        Dialog.showConfirmationMessageDialog window $"Are you sure you wish to delete customer {state.Customer.Name}?"

    let processDeleteResult (result: Result<bool, exn>) =
        match result with
        | Ok deleted ->
            if deleted then
                Dialog.Deleted |> Ok |> CloseDialog
            else
                ErrorMessage "Unknown error occured deleting from the database"
        | Error e -> e.Message |> ErrorMessage



    let update (host: HostWindow) (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | LoadForReview customerId -> state, Cmd.OfFunc.perform loadCustomer customerId Loaded
        | Loaded customerResult ->
            match customerResult with
            | Ok customer ->
                match customer with
                | Some cust ->
                    { state with
                          Customer = cust
                          Mode = Reviewing
                          StatusMessages = cust.ErrorMsgs },
                    Cmd.none
                | None ->
                    { state with
                          Customer = Customer.Empty
                          Mode = Reviewing
                          StatusMessages = [] },
                    "Customer not found."
                    |> Error
                    |> CloseDialog
                    |> Cmd.ofMsg (*Shouldn't happen*)
            | Error (e: exn) ->
                state,
                "Error loading the customer."
                |> Error
                |> CloseDialog
                |> Cmd.ofMsg
        | CloseDialog withResult -> state, Cmd.OfFunc.perform (closeDialog host) withResult (fun () -> NoneMsg)
        | NameChanged customer ->
            { state with
                  Customer =
                      { state.Customer with
                            Name = CustomerName.Create customer }
                  StatusMessages = state.Customer.ErrorMsgs },
            Cmd.none
        | EmailChanged email ->
            { state with
                  Customer =
                      { state.Customer with
                            Email = EmailAddressOptional.Create email }
                  StatusMessages = state.Customer.ErrorMsgs },
            Cmd.none
        | PhoneNoChanged phoneNo ->
            { state with
                  Customer =
                      { state.Customer with
                            Phone = PhoneNoOptional.Create phoneNo }
                  StatusMessages = state.Customer.ErrorMsgs },
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
                  Customer =
                      { state.Customer with
                            CustomerState = custState } },
            Cmd.none
        | SaveRequested -> state, Cmd.OfFunc.perform processSaveRequest state processSaveResult
        | DeleteConfirmationRequested ->
            state, Cmd.OfTask.perform (confirmDeletion host) state DeleteConfirmationReceived
        | DeleteConfirmationReceived confirmed ->
            state, Cmd.OfFunc.perform deleteCustomer state.Customer processDeleteResult
        | ErrorMessage errorMsg ->
            { state with
                  StatusMessages = [ errorMsg ] },
            Cmd.none
        | CancelRequested -> state, Dialog.Cancelled |> Ok |> CloseDialog |> Cmd.ofMsg
        | NoneMsg -> state, Cmd.none


    let view (state: State) (dispatch: Msg -> unit) =
        Grid.create
            [ Grid.classes [ "editDialogMainGrid" ]
              Grid.columnDefinitions "*,2*"
              Grid.rowDefinitions "auto,auto,auto,auto,*,auto,auto"
              Grid.children
                  [ TextBlock.create
                      [ TextBlock.classes [ "editFieldLabel" ]
                        Grid.row 0
                        Grid.column 0
                        TextBlock.text "Name" ]
                    TextBox.create
                        [ TextBox.classes [ "editField"; "customerNameEdit" ]
                          Grid.row 0
                          Grid.column 1
                          TextBox.text state.Customer.Name.Value
                          TextBox.onTextChanged (NameChanged >> dispatch) ]
                    TextBlock.create
                        [ TextBlock.classes [ "editFieldLabel" ]
                          Grid.row 1
                          Grid.column 0
                          TextBlock.text "Phone" ]
                    TextBox.create
                        [ TextBox.classes [ "editField"; "customerPhoneEdit" ]
                          Grid.row 1
                          Grid.column 1
                          TextBox.text state.Customer.Phone.Value
                          TextBox.onTextChanged (PhoneNoChanged >> dispatch) ]
                    TextBlock.create
                        [ TextBlock.classes [ "editFieldLabel" ]
                          Grid.row 2
                          Grid.column 0
                          TextBlock.text "Email" ]
                    TextBox.create
                        [ TextBox.classes [ "editField"; "customerEmailEdit" ]
                          Grid.row 2
                          Grid.column 1
                          TextBox.text state.Customer.Email.Value
                          TextBox.onTextChanged (EmailChanged >> dispatch) ]
                    TextBlock.create
                        [ TextBlock.classes [ "editFieldLabel" ]
                          Grid.row 3
                          Grid.column 0
                          TextBlock.text "Customer Is" ]
                    StackPanel.create
                        [ StackPanel.classes [ "editField"; "customerStateEdit" ]
                          Grid.row 3
                          Grid.column 1
                          StackPanel.children
                              [ RadioButton.create
                                  [ RadioButton.groupName "customerState"
                                    RadioButton.content "Active"
                                    RadioButton.isChecked (state.Customer.CustomerState = CustomerState.Active)
                                    RadioButton.onChecked (
                                        (fun _ -> dispatch (StateChanged CustomerState.Active)),
                                        OnChangeOf(state.Customer.CustomerState)
                                    ) ]
                                RadioButton.create
                                    [ RadioButton.groupName "customerState"
                                      RadioButton.content "Inactive"
                                      RadioButton.isChecked (state.Customer.CustomerState = CustomerState.InActive)
                                      RadioButton.onChecked (
                                          (fun _ -> dispatch (StateChanged CustomerState.InActive)),
                                          OnChangeOf(state.Customer.CustomerState)
                                      ) ] ] ]
                    TextBlock.create
                        [ TextBlock.classes [ "editFieldLabel" ]
                          Grid.row 4
                          Grid.column 0
                          TextBlock.text "Notes" ]
                    TextBox.create
                        [ TextBox.classes [ "editField"; "customerNotesEdit" ]
                          Grid.row 4
                          Grid.column 1
                          TextBox.text (
                              match state.Customer.Notes with
                              | Some notes -> notes
                              | None -> ""
                          )
                          TextBox.onTextChanged (NotesChanged >> dispatch) ]
                    if not state.Customer.IsValidValue then
                        Border.create
                            [ Border.classes [ "errorBorder" ]
                              Border.row 5
                              Border.column 1
                              Border.child (
                                  StackPanel.create
                                      [ StackPanel.classes [ "errorList" ]
                                        StackPanel.children
                                            [ for msg in state.StatusMessages do
                                                  yield
                                                      TextBlock.create
                                                          [ TextBlock.classes [ "errorMessage" ]
                                                            TextBlock.text msg ] ] ]
                              ) ]
                    StackPanel.create
                        [ Grid.row 6
                          Grid.column 1
                          StackPanel.orientation Layout.Orientation.Horizontal
                          StackPanel.children
                              [ Button.create
                                  [ Button.content "Delete"
                                    Button.classes [ "crudButtons"; "buttonItemDeleted" ]
                                    Button.isEnabled state.CanSave
                                    Button.onClick (fun _ -> dispatch DeleteConfirmationRequested) ]
                                Button.create
                                    [ Button.content "Save"
                                      Button.classes [ "crudButtons"; "buttonItemSave" ]
                                      Button.isEnabled state.CanSave
                                      Button.onClick (fun _ -> dispatch SaveRequested) ]
                                Button.create
                                    [ Button.content "Cancel"
                                      Button.classes [ "crudButtons"; "buttonCancel" ]
                                      Button.isEnabled state.CanSave
                                      Button.onClick (fun _ -> dispatch CancelRequested) ] ]

                          ] ] ]

type CustomerDialog(id: CustomerId option) as this =
    inherit HostWindow()
    let customerId = id

    do
        base.Title <- "Customer"
        base.Width <- 800.0
        base.Height <- 600.0
        base.MinWidth <- 800.0
        base.MinHeight <- 600.0

        //let init () = Customer.init customerId

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        let update = Customer.update this
        let init = Customer.init
        let view = Customer.view

        Elmish.Program.mkProgram init update view
        |> Program.withHost this
        |> Program.runWith customerId
