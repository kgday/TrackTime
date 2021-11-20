namespace TrackTime

/// This is the main module of your application
/// here you handle all of your child pages as well as their
/// messages and their updates, useful to update multiple parts
/// of your application, Please refer to the `view` function
/// to see how to handle different kinds of "*child*" controls
module Shell =
    open Elmish
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Input
    open Avalonia.FuncUI
    open Avalonia.FuncUI.Builder
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Elmish

    type State =
        { 
          AboutState: About.State 
          EntryPageState : EntryPage.State
        }

    type Msg =
        | AboutMsg of About.Msg
        | EntryPageMsg of EntryPage.Msg

    let init() : State * Cmd<Msg> =
        let aboutState, aboutPageCmd = About.init
        let entryPageState, entryPageCmd = EntryPage.init
        let aboutCmdMapped = Cmd.map AboutMsg aboutPageCmd
        let entryPageCmdMapped = Cmd.map EntryPageMsg entryPageCmd
        { AboutState = aboutState 
          EntryPageState = entryPageState},
        /// If your children controls don't emit any commands
        /// in the init function, you can just return Cmd.none
        /// otherwise, you can use a batch operation on all of them
        /// you can add more init commands as you need
        Cmd.batch [ aboutCmdMapped
                    entryPageCmdMapped ]

    let update ownerWindow (msg: Msg) (state: State): State * Cmd<_> =
        match msg with
        | AboutMsg msg ->
            let aboutState, cmd =
                About.update msg state.AboutState
            { state with AboutState = aboutState },
            /// map the message to the kind of message 
            /// your child control needs to handle
            Cmd.map AboutMsg cmd
        | EntryPageMsg msg ->
            let entryPageState, cmd =
                EntryPage.update ownerWindow msg state.EntryPageState
            { state with EntryPageState = entryPageState},
            Cmd.map EntryPageMsg cmd


    let view (state: State) (dispatch) =
        DockPanel.create
            [ DockPanel.children
                [ TabControl.create
                    [ TabControl.tabStripPlacement Dock.Top
                      TabControl.viewItems
                          [ TabItem.create
                                [ TabItem.header "Entry"
                                  /// If you don't need to be aware of the child control's state
                                  /// you can use the ViewBuilder to create the Host element and render it
                                  TabItem.content (EntryPage.view state.EntryPageState (EntryPageMsg >> dispatch)) ]
                            TabItem.create
                                [ TabItem.header "User Profiles Page"
                                  TabItem.content (ViewBuilder.Create<UserProfiles.Host>([])) ]
                            TabItem.create
                                [ TabItem.header "About"
                                  /// Use your child control's view function to render it, also don't forget to compose
                                  /// your dispatch function so it can handle the child control's message
                                  TabItem.content (About.view state.AboutState (AboutMsg >> dispatch)) ] ] ] ] ]

    /// This is the main window of your application
    /// you can do all sort of useful things here like setting heights and widths
    /// as well as attaching your dev tools that can be super useful when developing with
    /// Avalonia
    type MainWindow() as this =
        inherit HostWindow()
        do
            base.Title <- "Track You Time"
            base.Width <- 800.0
            base.Height <- 600.0
            base.MinWidth <- 800.0
            base.MinHeight <- 600.0

            //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
            //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
            let programUpdate = update this
            Elmish.Program.mkProgram init programUpdate view
            |> Program.withHost this
            |> Program.run
