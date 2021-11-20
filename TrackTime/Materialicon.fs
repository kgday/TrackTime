// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL.MaterialIcon

[<AutoOpen>]
module MaterialIcon =
    open System.Windows.Input 
    open Avalonia.Controls
    open Avalonia.Interactivity
    open Avalonia.Input
    open Avalonia.FuncUI.Builder
    open Avalonia.FuncUI.Types
    open Material.Icons.Avalonia
    open Material.Icons

    let create (attrs: IAttr<MaterialIcon> list): IView<MaterialIcon> =
        ViewBuilder.Create<MaterialIcon>(attrs)

    type MaterialIcon with
      static member kind<'t when 't :> MaterialIcon>(value: MaterialIconKind) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<MaterialIconKind>(MaterialIcon.KindProperty, value, ValueNone)

