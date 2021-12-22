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


module TimeEntry =

    type Mode =
        | Creating
        | Reviewing

    type State =
        { Mode: Mode
          TimeEntry: TimeEntry
          ErrorMessage: string option 
          SaveEnabled : bool
          DeleteEnabled : bool}
        member this.CanSave = this.SaveEnabled && this.TimeEntry.IsValidValue
        member this.CanDelete = this.DeleteEnabled && this.Mode <> Mode.Creating

        member this.StatusMessages =
            match this.ErrorMessage with
            | Some msg -> seq { msg }
            | None -> this.TimeEntry.ErrorMsgs


    type Msg =
        | LoadForReview of timeEntryId: TimeEntryId
        | Loaded of Result<TimeEntry, exn>
        | CloseDialog of Result<DialogResult, string>
        | RequestWorkItemForInitialValues of WorkItemId
        | SetInitalValuesFromWorkItem of Result<WorkItem, exn>
        | IsBillableChanged of bool
        | DescriptionChanged of string
        | TimeStartDateChanged of DateTime
        | TimeStartTimeChanged of TimeSpan
        | TimeEndDateChanged of DateTime option
        | TimeEndTimeChanged of TimeSpan option        
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

    let init (workItemId: WorkItemId) (timeEntryId: TimeEntryId option) =
        { Mode = Creating
          TimeEntry = { TimeEntry.Empty with WorkItemId = workItemId}
          SaveEnabled = true
          DeleteEnabled = true
          ErrorMessage = None },
        match timeEntryId with
        | Some id -> Cmd.ofMsg (LoadForReview id)
        | None -> Cmd.ofMsg ( RequestWorkItemForInitialValues workItemId)

    let loadTimeEntry id = AppDataService.getOneTimeEntry id

    let getWorkItem workItemId =
        AppDataService.getOneWorkItem workItemId

    let closeDialog (host: HostWindow) (result: Result<DialogResult, string>) = host.Close(result)
    let addTimeEntry timeEntry = AppDataService.addTimeEntry timeEntry
    let updateTimeEntry timeEntry = AppDataService.updateTimeEntry timeEntry
    let deleteTimeEntry timeEntry = AppDataService.deleteTimeEntry timeEntry


    let processSaveRequest state =
        match state.Mode with
        | Reviewing ->
            match updateTimeEntry state.TimeEntry with
            | Ok updated ->
                if updated then
                    Ok DialogResult.Updated
                else
                    Error "Unknown error occured updating the database"
            | Error e -> Error $"Error updating the database. {e.Message}"
        | _ ->
            match AppDataService.addTimeEntry state.TimeEntry with
            | Ok _ -> Ok DialogResult.Created
            | Error e -> Error $"Error adding to the database. {e.Message}"

    let processSaveResult (result: Result<DialogResult, string>) =
        match result with
        | Ok result -> result |> Ok |> CloseDialog
        | Error eMsg -> eMsg |> ErrorMessage


    let confirmDeletion window (state: State) =
        Dialog.showConfirmationMessageDialog window $"Are you sure you wish to delete timeEntry {state.TimeEntry.Description} started {state.TimeEntry.TimeStart}?"

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
        | LoadForReview timeEntryId -> state, Cmd.OfFunc.perform loadTimeEntry timeEntryId Loaded
        | Loaded timeEntryResult ->
            match timeEntryResult with
            | Ok timeEntry -> { state with TimeEntry = timeEntry; Mode = Reviewing }, Cmd.none
            | Error (e: exn) ->
                state,
                $"Error loading the timeEntry. {e.Message}"
                |> Error
                |> CloseDialog
                |> Cmd.ofMsg
        | CloseDialog withResult -> state, Cmd.OfFunc.perform (closeDialog host) withResult (fun () -> NoneMsg)
        | RequestWorkItemForInitialValues workItemId -> state, Cmd.OfFunc.perform getWorkItem workItemId SetInitalValuesFromWorkItem
        | SetInitalValuesFromWorkItem workItemResult ->
             match workItemResult with
             |Ok workItem -> 
                {state with
                    TimeEntry = {state.TimeEntry with IsBillable = workItem.IsBillable; Description = TimeEntryDescription.Create <| workItem.Title.Value}},
                Cmd.none
             |Error (e : exn) -> 
                state,
                $"Error loading the timeEntry. {e.Message}"
                |> Error
                |> CloseDialog
                |> Cmd.ofMsg 
        | IsBillableChanged isBillable -> {state with TimeEntry = {state.TimeEntry with  IsBillable = isBillable}}, Cmd.none
        | DescriptionChanged timeEntryDescription ->
            { state with
                  TimeEntry =
                      { state.TimeEntry with
                            Description = TimeEntryDescription.Create <| timeEntryDescription } },
            Cmd.none
        | NotesChanged notes ->
            { state with
                  TimeEntry =
                      { state.TimeEntry with
                            Notes =
                                (if notes |> String.IsNullOrWhiteSpace then
                                     None
                                 else
                                     Some notes) } },
            Cmd.none
        | BeenBilledChanged beenBilled ->
            { state with
                  TimeEntry = { state.TimeEntry with BeenBilled = beenBilled } },
            Cmd.none
        | TimeStartDateChanged timeStartDate ->
            { state with
                  TimeEntry =
                      { state.TimeEntry with
                            TimeStart = timeStartDate.Add state.TimeEntry.TimeStart.TimeOfDay } },
            Cmd.none
        | TimeStartTimeChanged timeStartTime ->
            { state with
                  TimeEntry =
                      { state.TimeEntry with
                            TimeStart = state.TimeEntry.TimeStart.Date.Add timeStartTime } },
            Cmd.none
        | TimeEndDateChanged timeEndDate ->
            let timeEndTime =
                state.TimeEntry.TimeEnd
                |> Option.map (fun (dt: DateTime) -> dt.TimeOfDay)
                |> (Option.defaultValue DateTime.Now.TimeOfDay)

            let newTimeEndOption =
                Option.map (fun (teDate: DateTime) -> teDate.Date.Add timeEndTime) timeEndDate

            { state with
                  TimeEntry = { state.TimeEntry with TimeEnd = newTimeEndOption } },
            Cmd.none
        | TimeEndTimeChanged timeEndTime ->
            let timeEndDate =
                state.TimeEntry.TimeEnd |> (Option.defaultValue state.TimeEntry.TimeStart)

            let newTimeEndOption =
                Option.map (fun (teTime: TimeSpan) -> timeEndDate.Date.Add teTime) timeEndTime

            { state with
                  TimeEntry = { state.TimeEntry with TimeEnd = newTimeEndOption } },
            Cmd.none
        | SaveRequested -> state, Cmd.OfFunc.perform processSaveRequest state processSaveResult
        | DeleteConfirmationRequested -> state, Cmd.OfTask.perform (confirmDeletion host) state DeleteConfirmationReceived
        | DeleteConfirmationReceived confirmed -> state, Cmd.OfFunc.perform deleteTimeEntry state.TimeEntry processDeleteResult
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
                                                         Grid.row 1
                                                         Grid.column 0
                                                         TextBlock.text "Description" ]
                                      TextBox.create [ TextBox.classes [ "editField"; "timeEntryDescriptionEdit" ]
                                                       Grid.row 1
                                                       Grid.column 1
                                                       TextBox.text <| state.TimeEntry.Description.ToString()
                                                       TextBox.onTextChanged (DescriptionChanged >> dispatch) ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 2
                                                         Grid.column 0
                                                         TextBlock.text "Date Created" ]
                                      WrapPanel.create [ WrapPanel.classes [ "editField"; "datePanel" ]
                                                         Grid.row 2
                                                         Grid.column 1
                                                         WrapPanel.children [ DatePicker.create [ DatePicker.classes [ "editField"; "timeStart" ]
                                                                                                  DatePicker.selectedDate (state.TimeEntry.TimeStart.Date)
                                                                                                  DatePicker.onSelectedDateChanged
                                                                                                      (fun date ->
                                                                                                          date
                                                                                                          |> Option.ofNullable
                                                                                                          |> Option.map (fun dto -> dto.DateTime.Date)
                                                                                                          |> Option.defaultValue DateTime.Today
                                                                                                          |> TimeStartDateChanged
                                                                                                          |> dispatch) ]
                                                                              TimePicker.create [ TimePicker.classes [ "editField"; "timeStart" ]
                                                                                                  TimePicker.selectedTime (state.TimeEntry.TimeStart.TimeOfDay)
                                                                                                  TimePicker.onSelectedTimeChanged
                                                                                                      (fun time ->
                                                                                                          time
                                                                                                          |> Option.ofNullable
                                                                                                          |> Option.defaultValue DateTime.Now.TimeOfDay
                                                                                                          |> TimeStartTimeChanged
                                                                                                          |> dispatch) ] ] ]
                                      TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                                         Grid.row 3
                                                         Grid.column 0
                                                         TextBlock.text "Time Ended" ]
                                      WrapPanel.create [ WrapPanel.classes [ "editField"; "datePanel" ]
                                                         Grid.row 3
                                                         Grid.column 1
                                                         WrapPanel.children [ DatePicker.create [ DatePicker.classes [ "editField"; "timeEnd" ]
                                                                                                  DatePicker.selectedDate (
                                                                                                      Option.map
                                                                                                          (fun (dt: DateTime) -> DateTimeOffset(dt.Date))
                                                                                                          state.TimeEntry.TimeEnd
                                                                                                  )
                                                                                                  DatePicker.onSelectedDateChanged
                                                                                                      (fun date ->
                                                                                                          date
                                                                                                          |> Option.ofNullable
                                                                                                          |> Option.map (fun dto -> dto.DateTime.Date)
                                                                                                          |> TimeEndDateChanged
                                                                                                          |> dispatch) ]
                                                                              TimePicker.create [ TimePicker.classes [ "editField"; "timeEnd" ]
                                                                                                  TimePicker.selectedTime (
                                                                                                      state.TimeEntry.TimeEnd
                                                                                                      |> Option.map (fun (dt: DateTime) -> dt.TimeOfDay)
                                                                                                      |> Option.toNullable
                                                                                                  )
                                                                                                  TimePicker.onSelectedTimeChanged
                                                                                                      (fun time -> time |> Option.ofNullable |> TimeEndTimeChanged |> dispatch) ] ] ]

                                      //TextBlock.create [ TextBlock.classes [ "editFieldLabel" ]
                                      //                   Grid.row 4
                                      //                   Grid.column 0
                                      //                   TextBlock.text "Flags" ]
                                      WrapPanel.create [ WrapPanel.classes [ "editField"; "flagsEdit" ]
                                                         Grid.row 4
                                                         Grid.column 1
                                                         WrapPanel.children [
                                                                              (*CheckBox.create [ CheckBox.classes [ "timeEntryFlag"*)
                                                                              //                                     "timeEntryStateIsBillable" ]
                                                                              //                  CheckBox.content "Is Billable"
                                                                              //                  CheckBox.isChecked (state.TimeEntry.IsBillable)
                                                                              //                  CheckBox.onChecked (fun _ -> dispatch <| IsBillableChanged true)
                                                                              //                  CheckBox.onUnchecked (fun _ -> dispatch <| IsBillableChanged false) ]
                                                                              //CheckBox.create [ CheckBox.classes [ "timeEntryFlag"; "timeEntryIsCompleted" ]
                                                                              //                  CheckBox.content "Is Completed"
                                                                              //                  CheckBox.isChecked (state.TimeEntry.IsCompleted)
                                                                              //                  CheckBox.onChecked (fun _ -> dispatch <| IsCompletedChanged true)
                                                                              //                  CheckBox.onUnchecked (fun _ -> dispatch <| IsCompletedChanged false) ]
                                                                              //CheckBox.create [ CheckBox.classes [ "timeEntryFlag"
                                                                              //                                     "timeEntryStateIsFixedPriced" ]
                                                                              //                  CheckBox.content "Is Fixed Priced"
                                                                              //                  CheckBox.isChecked (state.TimeEntry.IsFixedPrice)
                                                                              //                  CheckBox.onChecked (fun _ -> dispatch <| IsFixedPriceChanged true)
                                                                              //                  CheckBox.onUnchecked (fun _ -> dispatch <| IsFixedPriceChanged false) ]
                                                                              CheckBox.create [ CheckBox.classes [ "timeEntryFlag"
                                                                                                                   "timeEntryStateBeenBilled" ]
                                                                                                CheckBox.content "Been Billed"
                                                                                                CheckBox.isVisible (state.TimeEntry.IsBillable)
                                                                                                CheckBox.isChecked (state.TimeEntry.BeenBilled)
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
                                                           match state.TimeEntry.Notes with
                                                           | Some notes -> notes
                                                           | None -> ""
                                                       )
                                                       TextBox.onTextChanged (NotesChanged >> dispatch) ]
                                      if (not state.TimeEntry.IsValidValue) || state.ErrorMessage.IsSome then
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
                                                                                                Button.onClick (fun _ -> dispatch SaveRequested) ]
                                                                                Button.create [ Button.classes [ "crudButtons"; "cancel" ]
                                                                                                Button.isEnabled true
                                                                                                Button.onClick (fun _ -> dispatch CancelRequested) ] ]

                                                           ] ] ]

type TimeEntryDialog(workItemId, timeEntryId) as this =
    inherit HostWindow()

    do
        base.Title <- "Work Item"
        base.Width <- 800.0
        base.Height <- 400.0
        base.WindowStartupLocation <- WindowStartupLocation.CenterOwner
        base.MinWidth <- 450.0
        base.MinHeight <- 400.0


        //let init () = TimeEntry.init timeEntryId

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        let update = TimeEntry.update this
        let init = TimeEntry.init workItemId
        let view = TimeEntry.view

        Elmish.Program.mkProgram init update view
        |> Program.withHost this
        |> Program.withConsoleTrace
        |> Program.runWith timeEntryId
