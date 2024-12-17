module Program

open System
open Elmish.WPF
open Serilog
open Serilog.Extensions.Logging
open Elmish

module App =

    type TreeItem =
        { Id: Guid
          Data: string
          Children: TreeItem list }

    let buildMockTree () =
        let node111 =
            { Id = Guid.NewGuid()
              Data = "1.1.1"
              Children = [] }

        let node112 =
            { Id = Guid.NewGuid()
              Data = "1.1.2"
              Children = [] }

        let node113 =
            { Id = Guid.NewGuid()
              Data = "1.1.3"
              Children = [] }

        let node11 =
            { Id = Guid.NewGuid()
              Data = "1.1"
              Children = [ node111; node112; node113 ] }

        let node12 =
            { Id = Guid.NewGuid()
              Data = "1.2"
              Children = [] }

        let node1 =
            { Id = Guid.NewGuid()
              Data = "1"
              Children = [ node11; node12 ] }

        let node2111 =
            { Id = Guid.NewGuid()
              Data = "2.1.1.1"
              Children = [] }

        let node21 =
            { Id = Guid.NewGuid()
              Data = "2.1"
              Children = [ node2111 ] }

        let node2 =
            { Id = Guid.NewGuid()
              Data = "2"
              Children = [ node21 ] }

        let node3 =
            { Id = Guid.NewGuid()
              Data = "3"
              Children = [] }

        { Id = Guid.NewGuid()
          Data = "Root"
          Children = [ node1; node2; node3 ] }


    type Model =
        { Tree: TreeItem
          SelectedItem: Guid option }

    type Msg = SelectItem of Guid option

    let init () =
        { Tree = buildMockTree ()
          SelectedItem = None },
        Cmd.none

    let update msg model : Model * Cmd<Msg> =
        match msg with
        | SelectItem idOpt ->  { model with SelectedItem = idOpt }, Cmd.none


module Bindings =
    open App

    let rec treeItemBindings () : Binding<TreeItem, Msg> list =
        [ "Data" |> Binding.oneWay ((fun (t: TreeItem) -> t.Data))

          "Children"
          |> Binding.subModelSeq (
              (fun (t: TreeItem) -> t.Children :> seq<TreeItem>),
              (fun (_: TreeItem, child: TreeItem) -> child),
              (fun (child: TreeItem) -> child.Id),
              (fun (_, childMsg: Msg) -> childMsg),
              treeItemBindings
          )

          ]


    let rootBindings () : Binding<Model, Msg> list =
        [ "Tree"
          |> Binding.SubModel.required (treeItemBindings)
          |> Binding.mapModel (fun m -> m.Tree)

          "TopChildren"
          |> Binding.subModelSeq (
              (fun m -> m.Tree.Children :> seq<TreeItem>),
              (fun (_m, child) -> child),
              (fun (child: TreeItem) -> child.Id),
              (fun (_childId, childMsg) -> childMsg),
              (fun () -> treeItemBindings ())
          )

          "SelectedItem"
          |> Binding.subModelSelectedItem (
              "TopChildren",
              (fun m -> m.SelectedItem),
              (fun idOpt _ -> SelectItem idOpt)
          )

          ]


//------------------------------------------------------------------------------------------------

let mainDesignVm =
    let model = App.init () |> fst
    ViewModel.designInstance model (Bindings.rootBindings ())


let main window =

    let logger =
        LoggerConfiguration()
            .MinimumLevel.Override("Elmish.WPF.Update", Serilog.Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Bindings", Serilog.Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Performance", Serilog.Events.LogEventLevel.Verbose)
            .WriteTo.Console()
            .CreateLogger()

    WpfProgram.mkProgram App.init App.update Bindings.rootBindings
    |> WpfProgram.withLogger (new SerilogLoggerFactory(logger))
    |> WpfProgram.startElmishLoop window
