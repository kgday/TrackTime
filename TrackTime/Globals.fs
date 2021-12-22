namespace TrackTime
open System
open Avalonia.Controls
open System.Threading.Tasks
open System

type IWindowService =
    abstract OpenModelDialog : createFunc : (unit -> Window) -> Task<Result<DialogResult, string>>
    abstract ShowErrorMsg : msgStr : string -> Task<unit>
    abstract ShowConfirmationMsg : msgStr : string -> Task<bool>

    module Globals =
        let mutable private windowService : IWindowService option = None
        let SetWindowService newWindowService =
            windowService <- Some newWindowService;
        let GetWindowService() = 
            match windowService with
            |Some winService -> winService
            |None -> raise (InvalidProgramException ("Invalid attempt to acc"))

