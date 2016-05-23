namespace Scratch
open System
open System.IO
open Scratch.Scratchpad

module Program =

    let ensureScratchpadInitialized() =
        Directory.CreateDirectory(rootPath) |> ignore
        Directory.CreateDirectory(stateDirPath) |> ignore

    let executeResetCommand() =
        let isNotStateDirectory (fso: FsObject) =
            stateDirPath.Equals(fso.FullName, StringComparison.OrdinalIgnoreCase) = false

        let enumerateScratchpad () =
            rootPath
            |> FsObject.Enumerate 
            |> Seq.where isNotStateDirectory

        let isScratchpadEmpty =
            enumerateScratchpad()
            |> Seq.isEmpty

        if not isScratchpadEmpty then
            let snapshotPath = createNewSnapshotPath()
            enumerateScratchpad()
            |> Seq.iter (fun fso -> fso.MoveNoThrow snapshotPath)

    let executeListCommand() =
        ScratchpadItem.EnumerateAll()
        |> Seq.iter (fun x -> printfn "%s" x.Name)

    let executeHoistExactCommand (item: ScratchpadItem) =
        raise <| NotImplementedException()

    let executeHoistByNameCommand (name: string) =
        let result =
            ScratchpadItem.EnumerateAll()
            |> Seq.where (fun i -> i.Name = name)
            |> Seq.tryHead

        match result with
        | Some(item) ->
            let instanceCount = Seq.length item.Instances
            if instanceCount > 1 then
                failwith <| sprintf "multiple items found with name \"%s\"" name
            let instance =  Seq.head item.Instances
            //instance.Hoist()
            raise <| NotImplementedException()
        | None ->
            failwith <| sprintf "no items found with name \"%s\"" name
        
    type Command =
        | Reset
        | List
        | HoistExact of ScratchpadItem
        | HoistByName of string


    let execute c =
        match c with
        | Reset -> executeResetCommand()
        | List -> executeListCommand()
    //        executeListCommand()
    //        |> Seq.iter (fun x -> printfn "%s" x.Name)
        | HoistExact(item) -> executeHoistExactCommand item
        | HoistByName(name) -> executeHoistByNameCommand name

    [<EntryPoint>]
    let main argv = 
        printfn "%A" argv
        ensureScratchpadInitialized()

        0 // return an integer exit code
