module Scratch.Scratchpad
open System
open System.IO
//
//
//let rootPath = @"D:\ScratchpadTest"
//
//module SnapshotDirectoryRelativePath =
//    let getRelativePath (x: DateTimeOffset) =
//        let str = x.ToString("O")
//        let tIndex = str.IndexOf('T')
//        let dateStr = str.Substring(0, tIndex)
//        let timeStr = str.Substring(tIndex + 1)
//        //[| timeStr.Replace(':', '_') |]
//        Seq.singleton (timeStr.Replace(':', '_'))
//        |> Seq.append (dateStr.Split '-')
//        |> Seq.toArray
//        |> Path.Combine
//
//    let getDateTimeOffset (x: string) =
//        match x.Split(Path.DirectorySeparatorChar) with
//        | [| year; month; date; time |] ->
//            let str = sprintf "%s-%s-%sT%s" year month date (time.Replace('_', ':'))
//            match (DateTimeOffset.TryParseExact(str, "O", null, Globalization.DateTimeStyles.None)) with
//            | true, m -> Some m
//            | _ -> None
//        | _ -> None
//
//
//type Snapshot = {
//    Path : string
//    Moment : DateTimeOffset
//    TopLevelObjects : FsObject array
//}
//with
//    static member New (snapshotDirPath: string, p: string) =
//        let relPath = p.Substring(snapshotDirPath.Length + 1)
//        match (SnapshotDirectoryRelativePath.getDateTimeOffset relPath) with
//        | Some m ->
//            Some {
//                Path = p;
//                Moment = m;
//                TopLevelObjects = FsObject.Enumerate p |> Seq.toArray
//            }
//        | None -> None
//
//module SnapshotDirectory =
//    
//    let directoryName = ".scratchpad"
//    let directoryPath = Path.Combine(rootPath, directoryName)
//
//    let createNewSnapshotPath() =
//        let relPath = SnapshotDirectoryRelativePath.getRelativePath DateTimeOffset.UtcNow
//        let path = Path.Combine(directoryPath, relPath)
//        Directory.CreateDirectory(path).FullName
//
//    let enumerateSnapshots() =
//        let isIntOfLength l (s: string) =
//            if s.Length = l then
//                match (Int32.TryParse(s)) with
//                | true, _ -> true
//                | _ -> false
//            else
//                false
//
//        Directory.EnumerateDirectories(directoryPath)
//        |> Seq.where (fun p -> Path.GetFileName(p) |> isIntOfLength 4)
//        |> Seq.collect (fun yearPath ->
//            Directory.EnumerateDirectories yearPath
//            |> Seq.where (fun p -> Path.GetFileName(p) |> isIntOfLength 2)
//            |> Seq.collect (fun monthPath ->
//                Directory.EnumerateDirectories monthPath
//                |> Seq.where (fun p -> Path.GetFileName(p) |> isIntOfLength 2)
//                |> Seq.collect (fun timePath ->
//                    Directory.EnumerateDirectories timePath
//                    |> Seq.map (fun p -> Snapshot.New (directoryPath, p))
//                    |> Seq.where (fun x -> x.IsSome)
//                    |> Seq.map (fun x -> x.Value)
//                    )
//                //|> Seq.map (fun p -> SnapshotInfo.New p)
//                )
//            )
//
//let ensureInitialized () =
//    Directory.CreateDirectory(rootPath) |> ignore
//    Directory.CreateDirectory(SnapshotDirectory.directoryPath) |> ignore
//
//
//type CatalogItem = {
//    Name : string
//    ReferencingSnapshots : Snapshot seq
//}
//with
////    member x.EnumerateHardInstanceRefs () = seq {
////        for ss in x.ReferencingSnapshots
////    }
//        
//    static member EnumerateCatalogItems () =
//        SnapshotDirectory.enumerateSnapshots()
//        |> Seq.collect (fun s ->
//            s.TopLevelObjects
//            |> Seq.where (fun fso -> fso.Type = Scratch.FsObjectType.Directory)
//            |> Seq.map (fun fso -> (Path.GetFileName(fso.FullName), s))
//            )
//        |> Seq.groupBy (fun (directory, snapshots) -> directory)
//        |> Seq.map (
//            fun group ->
//                let itemKey = fst group
//                let instances =
//                    snd group
//                    |> Seq.map snd
//                { Name = itemKey; HardInstances = instances }
//            )
//
