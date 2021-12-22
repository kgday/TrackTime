namespace TrackTime

[<AutoOpen>]
module ImageButtonContents =
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia

    let create iconImage text =
        StackPanel.create [ StackPanel.classes [ "imageButtonContent" ]
                            StackPanel.children [ iconImage
                                                  TextBlock.create [ TextBlock.classes [ "iconButtonText" ]
                                                                     TextBlock.text text ] ] ]
