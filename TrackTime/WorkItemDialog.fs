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
open Avalonia.Layout


module WorkItem =

    type Mode =
        | Creating
        | Reviewing

    type State =
        { Mode: Mode
          WorkItem: WorkItem
          ErrorMessage: string option 
          SaveEnabled : bool
          DeleteEnabled : bool}
        member this.CanSave = this.WorkItem.IsValidValue
        member this.CanDelete = this.DeleteEnabled && this.Mode <> Mode.Creating

        member this.StatusMessages =
            match this.ErrorMessage with
            | Some msg -> seq { msg }
            | None -> this.WorkItem.ErrorMsgs


    type Msg =
        | LoadForReview of workItemId: WorkItemId
        | Loaded of Result<WorkItem, exn>
        | CloseDialog of Result<DialogResult, string>
        | TitleChanged of string
        | DescriptionChanged of string
        | IsCompletedChanged of bool
        | IsBillableChanged of bool
        | IsFixedPriceChanged of bool
        | DateCreatedChanged of DateTime
        | DueDateChanged of DateTime option
        | BeenBilledChanged of bool
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

    let init (customerId: CustomerId) =
        { Mode = Creating
          WorkItem = { WorkItem.Empty with CustomerId = customerId }
          SaveEnabled = true
          DeleteEnabled = true
          ErrorMessage = None },
         Cmd.none

    let loadWorkItem id = AppDataService.getOneWorkItem id
    let closeDialog (host: HostWindow) (result: Result<DialogResult, string>) = host.Close(result)
    let addWorkItem workItem = AppDataService.addWorkItem workItem
    let updateWorkItem workItem = AppDataService.updateWorkItem workItem
    let deleteWorkItem workItem = AppDataService.deleteWorkItem workItem


    let processSaveRequest state =
        match state.Mode with
        | Reviewing ->
            match updateWorkItem state.WorkItem with
            | Ok updated ->
                if updated then
                    Ok DialogResult.Updated
                else
                    Error "Unknown error occured updating the database"
            | Error e -> Error $"Error updating the database. {e.Message}"
        | _ ->
            match AppDataService.addWorkItem state.WorkItem with
            | Ok newId -> Ok <| DialogResult.Created newId
            | Error e -> Error $"Error adding to the database. {e.Message}"

    let processSaveResult (result: Result<DialogResult, string>) =
        match result with
        | Ok result -> result |> Ok |> CloseDialog
        | Error eMsg -> eMsg |> ErrorMessage


    let confirmDeletion window (state: State) =
        Dialog.showConfirmationMessageDialog window $"Are you sure you wish to delete workItem {state.WorkItem.Title}?"

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
        | LoadForReview workItemId -> state, Cmd.OfFunc.perform loadWorkItem workItemId Loaded
        | Loaded workItemResult ->
            match workItemResult with
            | Ok workItem -> { state with WorkItem = workItem; Mode = Reviewing }, Cmd.none
            | Error (e: exn) ->
                state,
                $"Error loading the workItem. {e.Message}"
                |> Error
                |> CloseDialog
                |> Cmd.ofMsg
        | CloseDialog withResult -> state, Cmd.OfFunc.perform (closeDialog host) withResult (fun () -> NoneMsg)
        | TitleChanged workItem ->
            { state with
                  WorkItem =
                      { state.WorkItem with
                            Title = WorkItemTitle.Create workItem } },
            Cmd.none
        | DescriptionChanged workItemDescription ->
            { state with
                  WorkItem =
                      { state.WorkItem with
                            Description = WorkItemDescriptionOptional.Create <| Some workItemDescription } },
            Cmd.none
        | NotesChanged notes ->
            { state with
                  WorkItem =
                      { state.WorkItem with
                            Notes =
                                (if notes |> String.IsNullOrWhiteSpace then
                                     None
                                 else
                                     Some notes) } },
            Cmd.none
        | IsCompletedChanged isCompleted ->
            { state with
                  WorkItem = { state.WorkItem with IsCompleted = isCompleted } },
            Cmd.none
        | BeenBilledChanged beenBilled ->
            { state with
                  WorkItem = { state.WorkItem with BeenBilled = beenBilled } },
            Cmd.none
        | DateCreatedChanged dateCreated ->
            { state with
                  WorkItem = { state.WorkItem with DateCreated = dateCreated } },
            Cmd.none
        | DueDateChanged dueDate ->
            { state with
                  WorkItem = { state.WorkItem with DueDate = dueDate } },
            Cmd.none
        | IsBillableChanged isBillable ->
            { state with
                  WorkItem = { state.WorkItem with IsBillable = isBillable } },
            Cmd.none
        | IsFixedPriceChanged isFixedPrice ->
            { state with
                  WorkItem = { state.WorkItem with IsFixedPrice = isFixedPrice } },
            Cmd.none
        | SaveRequested -> state, Cmd.OfFunc.perform processSaveRequest state processSaveResult
        | DeleteConfirmationRequested -> state, Cmd.OfTask.perform (confirmDeletion host) state DeleteConfirmationReceived
        | DeleteConfirmationReceived confirmed -> 
            if confirmed then
                state, Cmd.OfFunc.perform deleteWorkItem state.WorkItem processDeleteResult
            else
                state, Cmd.none
        | ErrorMessage errorMsg -> { state with ErrorMessage = Some errorMsg }, Cmd.OfTask.perform delay 10000 (fun _ -> ClearError)
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
                      Grid.rowDefinitions "auto,auto,auto,auto,auto,*,auto,auto"
                      Grid.children [ TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 0
                                                         Grid.column 0
                                                         TextBlock.text "Title" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "workItemTitleEdit" ]
                                                       Grid.row 0
                                                       Grid.column 1
                                                       TextBox.text <| state.WorkItem.Title.Value
                                                       TextBox.onTextChanged (TitleChanged >> dispatch) ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 1
                                                         Grid.column 0
                                                         TextBlock.text "Description" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "workItemDescriptionEdit" ]
                                                       Grid.row 1
                                                       Grid.column 1
                                                       TextBox.text <| state.WorkItem.Description.ToString()
                                                       TextBox.onTextChanged (DescriptionChanged >> dispatch) ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 2
                                                         Grid.column 0
                                                         TextBlock.text "Date Created" ]
                                      DatePicker.create [ DatePicker.classes [ "editField; workItemDateCreatedEdit" ]
                                                          Grid.row 2
                                                          Grid.column 1
                                                          DatePicker.selectedDate (state.WorkItem.DateCreated)
                                                          DatePicker.onSelectedDateChanged
                                                              (fun date ->
                                                                  dispatch
                                                                  <| DateCreatedChanged(
                                                                      if date.HasValue then
                                                                          date.Value.DateTime.Date
                                                                      else
                                                                          DateTime.Today
                                                                  )) ]

                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 3
                                                         Grid.column 0
                                                         TextBlock.text "Due Date" ]

                                      DatePicker.create [ DatePicker.classes [ "editField; workItemDateCreatedEdit" ]
                                                          Grid.row 3
                                                          Grid.column 1
                                                          DatePicker.selectedDate (
                                                              match state.WorkItem.DueDate with
                                                              | Some date -> Some(DateTimeOffset(date))
                                                              | None -> None
                                                          )
                                                          DatePicker.onSelectedDateChanged
                                                              (fun dtOffset ->
                                                                  dispatch
                                                                  <| DueDateChanged(Option.map (fun (dto: DateTimeOffset) -> dto.DateTime.Date) (Option.ofNullable dtOffset))) ]

                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 4
                                                         Grid.column 0
                                                         TextBlock.text "Flags" ]
                                      WrapPanel.create [ WrapPanel.classes [ "editField"; "flagsEdit" ]
                                                         Grid.row 4
                                                         Grid.column 1
                                                         WrapPanel.children [ CheckBox.create [ CheckBox.classes [ "workItemFlag"
                                                                                                                   "workItemStateIsBillable" ]
                                                                                                CheckBox.content "Is Billable"
                                                                                                CheckBox.isChecked (state.WorkItem.IsBillable)
                                                                                                CheckBox.onChecked (fun _ -> dispatch <| IsBillableChanged true)
                                                                                                CheckBox.onUnchecked (fun _ -> dispatch <| IsBillableChanged false) ]
                                                                              CheckBox.create [ CheckBox.classes [ "workItemFlag"; "workItemIsCompleted" ]
                                                                                                CheckBox.content "Is Completed"
                                                                                                CheckBox.isChecked (state.WorkItem.IsCompleted)
                                                                                                CheckBox.onChecked (fun _ -> dispatch <| IsCompletedChanged true)
                                                                                                CheckBox.onUnchecked (fun _ -> dispatch <| IsCompletedChanged false) ]
                                                                              CheckBox.create [ CheckBox.classes [ "workItemFlag"
                                                                                                                   "workItemStateIsFixedPriced" ]
                                                                                                CheckBox.content "Is Fixed Priced"
                                                                                                CheckBox.isChecked (state.WorkItem.IsFixedPrice)
                                                                                                CheckBox.onChecked (fun _ -> dispatch <| IsFixedPriceChanged true)
                                                                                                CheckBox.onUnchecked (fun _ -> dispatch <| IsFixedPriceChanged false) ]
                                                                              CheckBox.create [ CheckBox.classes [ "workItemFlag"
                                                                                                                   "workItemStateBeenBilled" ]
                                                                                                CheckBox.content "Been Billed"
                                                                                                CheckBox.isVisible (state.WorkItem.IsBillable)
                                                                                                CheckBox.isChecked (state.WorkItem.BeenBilled)
                                                                                                CheckBox.onChecked (fun _ -> dispatch <| BeenBilledChanged true)
                                                                                                CheckBox.onUnchecked (fun _ -> dispatch <| BeenBilledChanged false) ] ] ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 5
                                                         Grid.column 0
                                                         TextBlock.text "Notes" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "notesEdit" ]
                                                       Grid.row 5
                                                       Grid.column 1
                                                       TextBox.text (
                                                           match state.WorkItem.Notes with
                                                           | Some notes -> notes
                                                           | None -> ""
                                                       )
                                                       TextBox.onTextChanged (NotesChanged >> dispatch) ]
                                      if (not state.WorkItem.IsValidValue) || state.ErrorMessage.IsSome then
                                          Border.create [ Border.classes [ "error" ]
                                                          Border.row 6
                                                          Border.column 1
                                                          Border.child (
                                                              StackPanel.create [ StackPanel.classes [ "error" ]
                                                                                  StackPanel.children [ for msg in state.StatusMessages do
                                                                                                            yield
                                                                                                                TextBlock.create [ TextBlock.classes [ "error" ]
                                                                                                                                   TextBlock.text msg ] ] ]
                                                          ) ]
                                      StackPanel.create [ Grid.row 7
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

type WorkItemDialog(customerId, workItemId) as this =
    inherit HostWindow()

    do
        base.Title <- "Work Item"
        base.Width <- 450.0
        base.Height <- 400.0
        base.WindowStartupLocation <- WindowStartupLocation.CenterOwner
        base.MinWidth <- 450.0
        base.MinHeight <- 400.0

        let subscribeActivated _ =
            match workItemId with
            |None -> Cmd.none
            |Some wiId -> 
                let sub dispatch =
                    this.Activated.Take(1).Subscribe(fun _ -> wiId |> WorkItem.LoadForReview |> dispatch )
                    |>  ignore
                Cmd.ofSub sub


        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        let update = WorkItem.update this
        let init = WorkItem.init 
        let view = WorkItem.view

        Elmish.Program.mkProgram init update view
        |> Program.withHost this
        //|> Program.withConsoleTrace
        |> Program.withSubscription subscribeActivated
        |> Program.runWith customerId
