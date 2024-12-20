﻿module Program

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

    [<AutoOpen>]
    module TreeItem =

        let create data =
            { Id = Guid.NewGuid()
              Data = data
              Children = [] }

        let moveItemUp (id: Guid) (children: TreeItem list) : TreeItem list =
            let rec aux acc =
                function
                | [] -> List.rev acc
                | x :: xs when x.Id = id ->
                    match acc with
                    | [] -> x :: xs // Can't move up, already at top
                    | y :: rest -> List.rev rest @ (x :: y :: xs)
                | x :: xs -> aux (x :: acc) xs

            aux [] children |> List.toArray |> Array.toList

        let moveItemDown (id: Guid) (children: TreeItem list) : TreeItem list =
            let rec aux acc =
                function
                | [] -> List.rev acc
                | x :: (y :: ys) when x.Id = id -> List.rev acc @ (y :: x :: ys)
                | x :: xs -> aux (x :: acc) xs

            aux [] children |> List.toArray |> Array.toList

   
        let rec findItemById (id: Guid) (t: TreeItem) : TreeItem option =
            if t.Id = id then Some t else t.Children |> List.tryPick (findItemById id)

        let rec updateTreeItemById id f tree =
            if tree.Id = id then
                printfn "Updating tree item with ID: %O" id
                f tree
            else
                { tree with
                    Children =
                        tree.Children
                        |> List.map (updateTreeItemById id f)
                        |> List.toArray // Ensure new instance
                        |> Array.toList }


        let rec updateParentOfItemById
            (id: Guid)
            (f: TreeItem list -> TreeItem list)
            (tree: TreeItem)
            : TreeItem =
            if tree.Children |> List.exists (fun child -> child.Id = id) then
                { tree with Children = f tree.Children }
            else
                { tree with Children = tree.Children |> List.map (updateParentOfItemById id f) }


        module Mock =

            let buildMockTree () =
                let node111 = { Id = Guid.NewGuid(); Data = "1.1.1"; Children = [] }
                let node112 = { Id = Guid.NewGuid(); Data = "1.1.2"; Children = [] }
                let node113 = { Id = Guid.NewGuid(); Data = "1.1.3"; Children = [] }
                let node11 = { Id = Guid.NewGuid(); Data = "1.1"; Children = [ node111; node112; node113 ] }
                
                let node12 = { Id = Guid.NewGuid(); Data = "1.2"; Children = [] }
        
                let node1 = { Id = Guid.NewGuid(); Data = "1"; Children = [ node11; node12 ] }
                
                let node2111 = { Id = Guid.NewGuid(); Data = "2.1.1.1"; Children = [] }
                let node21 = { Id = Guid.NewGuid(); Data = "2.1"; Children = [ node2111 ] }
                let node2 = { Id = Guid.NewGuid(); Data = "2"; Children = [ node21 ] }
                
                let node3 = { Id = Guid.NewGuid(); Data = "3"; Children = [] }
                
                { Id = Guid.NewGuid()
                  Data = "Root"
                  Children = [ node1; node2; node3 ] }
        

                 

    type Model =
        { Tree: TreeItem
          SelectedItem: Guid option
          SelectedItemLogMessage: string }

    type Msg =
        | SelectItem of Guid option
        | UpdateSelectedItemData of string
        | MoveUp
        | MoveDown

    let init () =
        { Tree = Mock.buildMockTree ()
          SelectedItem = None
          SelectedItemLogMessage = "No Item Selected" },
        Cmd.none


    let selectItem idOpt model =
        let logMsg =
            match idOpt with
            | Some id -> sprintf "Selected Item: %O" id
            | None -> "No Item Selected"

        printfn "Updated SelectedItem: %O" idOpt

        { model with
            SelectedItem = idOpt
            SelectedItemLogMessage = logMsg }

    let update msg model : Model * Cmd<Msg> =
        match msg with
        | SelectItem idOpt ->
            selectItem idOpt model, Cmd.none

        | UpdateSelectedItemData newData ->
            match model.SelectedItem with
            | None -> model, Cmd.none
            | Some id ->
                { model with
                    Tree =
                        TreeItem.updateTreeItemById
                            id
                            (fun item -> { item with Data = newData })
                            model.Tree },
                Cmd.none

        | MoveUp ->
            match model.SelectedItem with
            | None -> model, Cmd.none
            | Some id ->
                let updatedTree =
                    TreeItem.updateParentOfItemById
                        id
                        (fun children -> TreeItem.moveItemUp id children)
                        model.Tree

                { model with
                    Tree = updatedTree}, // Focus item in the model
                Cmd.ofMsg (SelectItem(Some id)) // Ensure item is re-selected

        | MoveDown ->
            match model.SelectedItem with
            | None -> model, Cmd.none
            | Some id ->
                let updatedTree =
                    TreeItem.updateParentOfItemById
                        id
                        (fun children -> TreeItem.moveItemDown id children)
                        model.Tree

                { model with
                    Tree = updatedTree}, // Focus item in the model
                Cmd.ofMsg (SelectItem(Some id)) // Ensure item is re-selected




module Bindings =
    open App


    let twoWayDataBinding =
        Binding.twoWay (
            (fun (t: TreeItem) -> t.Data),
            (fun (newData: string) (_t: TreeItem) -> UpdateSelectedItemData newData)
        )


    let rec treeItemBindings () : Binding<TreeItem, Msg> list =
        [ "Data"
          |> Binding.twoWay (
              (fun (t: TreeItem) -> t.Data),
              (fun (newData: string) _ -> UpdateSelectedItemData newData)
          )

          "Children"
          |> Binding.subModelSeq (
              (fun (t: TreeItem) -> t.Children :> seq<TreeItem>),
              (fun (_: TreeItem, child: TreeItem) -> child),
              (fun (child: TreeItem) -> child.Id),
              (fun (_, childMsg: Msg) -> childMsg),
              treeItemBindings
          )

          ]

    let getSelectedItemData (m: Model) =
        match m.SelectedItem with
        | Some sid ->
            match App.TreeItem.findItemById sid m.Tree with
            | Some item -> item.Data
            | None -> ""
        | None -> ""

    let setSelectedItemData (newData: string) (m: Model) = UpdateSelectedItemData newData

    //• Root bindings
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


          "SelectedItemData"
          |> Binding.twoWay (
              (fun m -> getSelectedItemData m),
              (fun newData m -> setSelectedItemData newData m)
          )

          "SelectedItemLogMessage" |> Binding.oneWay (fun m -> m.SelectedItemLogMessage)

          "MoveUp" |> Binding.cmdIf (fun m -> if m.SelectedItem.IsSome then Some MoveUp else None)
          "MoveDown"
          |> Binding.cmdIf (fun m -> if m.SelectedItem.IsSome then Some MoveDown else None) ]


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
