#load "Scripts/load-project-debug.fsx"
open System
open System.IO

let rootPath = @"D:\ScratchpadTest"
let stateDirName = ".scratchpad"
let stateDirPath = Path.Combine(rootPath, stateDirName)

let logException (ex: Exception) =
    System.Console.Error.WriteLine(ex)

type System.DateTimeOffset with
    member x.ToFileSystemFriendlyName () =
        x.ToString("O").Replace(':', '_');
    
    static member FromFileSystemFriendlyName (x: string) =
        DateTimeOffset.ParseExact(x.Replace('_', ':'), "O", null)

type FsObjectType =
    | File
    | FileLink
    | Directory
    | DirectoryLink

type FsObject = {
    FullName : string
    Type : FsObjectType
}
with
    member x.MoveNoThrow (d: string) =
        try
            //Directory.Move(x.FullName, Path.Combine(d, Path.GetFileName(x.FullName))
            let destName = Path.Combine(d, Path.GetFileName(x.FullName))
            match x.Type with
            | File
            | FileLink -> File.Move(x.FullName, destName)
            | Directory
            | DirectoryLink -> Directory.Move(x.FullName, destName)
        with
            | ex -> logException ex

    static member FromDirectoryInfo (x: DirectoryInfo) =
        {
            FullName = x.FullName;
            Type = if (x.Attributes &&& FileAttributes.ReparsePoint = FileAttributes.ReparsePoint)
                        then DirectoryLink
                        else Directory
        }

    static member FromFileInfo (x: FileInfo) =
        {
            FullName = x.FullName;
            Type = if (x.Attributes &&& FileAttributes.ReparsePoint = FileAttributes.ReparsePoint)
                        then FileLink
                        else File
        }

    static member Enumerate (p: string) = 
        let dirs = 
            Directory.EnumerateDirectories(p)
            |> Seq.map DirectoryInfo
            |> Seq.map FsObject.FromDirectoryInfo
        let files =
            Directory.EnumerateFiles(p)
            |> Seq.map FileInfo
            |> Seq.map FsObject.FromFileInfo
        Seq.append dirs files


//type SnapshotPath = {
//    FullName : string
//    Moment : DateTimeOffset
//}
//with


let getMomentRelativeSnapshotPath (m: DateTimeOffset) =
    let str = m.ToString("O")
    let tIndex = str.IndexOf('T')
    let dateStr = str.Substring(0, tIndex)
    let timeStr = str.Substring(tIndex + 1)
    //[| timeStr.Replace(':', '_') |]
    Seq.singleton (timeStr.Replace(':', '_'))
    |> Seq.append (dateStr.Split '-')
    |> Seq.toArray
    |> Path.Combine

let getRelativeSnapshotPathMoment (p: string) =
    match p.Split(Path.DirectorySeparatorChar) with
    | [| year; month; date; time |] ->
        let str = sprintf "%s-%s-%sT%s" year month date (time.Replace('_', ':'))
        match (DateTimeOffset.TryParseExact(str, "O", null, Globalization.DateTimeStyles.None)) with
        | true, m -> Some m
        | _ -> None
    | _ -> None

type ScratchpadInstance = {
    Path : string
    Moment : DateTimeOffset
    Objects : FsObject array
}
with
    static member New (p: string) =
        let relPath = p.Substring(stateDirPath.Length + 1)
        match (getRelativeSnapshotPathMoment relPath) with
        | Some m ->
            Some {
                Path = p;
                Moment = m;
                Objects = FsObject.Enumerate p |> Seq.toArray
            }
        | None -> None
    static member EnumerateAll () =
        let isIntOfLength l (s: string) =
            if s.Length = l then
                match (Int32.TryParse(s)) with
                | true, _ -> true
                | _ -> false
            else
                false

        Directory.EnumerateDirectories(stateDirPath)
        |> Seq.where (fun p -> Path.GetFileName(p) |> isIntOfLength 4)
        |> Seq.collect (fun yearPath ->
            Directory.EnumerateDirectories yearPath
            |> Seq.where (fun p -> Path.GetFileName(p) |> isIntOfLength 2)
            |> Seq.collect (fun monthPath ->
                Directory.EnumerateDirectories monthPath
                |> Seq.where (fun p -> Path.GetFileName(p) |> isIntOfLength 2)
                |> Seq.collect (fun timePath ->
                    Directory.EnumerateDirectories timePath
                    |> Seq.map (fun p -> ScratchpadInstance.New p)
                    |> Seq.where (fun x -> x.IsSome)
                    |> Seq.map (fun x -> x.Value)
                    )
                //|> Seq.map (fun p -> SnapshotInfo.New p)
                )
            )

type ScratchpadItem = {
    Name : string
    Instances : ScratchpadInstance seq
}
with
    member x.HasMultipleInstances with get() = (Seq. length x.Instances) > 1
    member x.Hoist() =
        ()
    static member EnumerateAll () =
        ScratchpadInstance.EnumerateAll()
        |> Seq.collect (fun s ->
            s.Objects
            |> Seq.where (fun fso -> fso.Type = Directory)
            |> Seq.map (fun fso -> (Path.GetFileName(fso.FullName), s))
            )
        |> Seq.groupBy (fun (directory, snapshots) -> directory)
        |> Seq.map (
            fun group ->
                let itemKey = fst group
                let instances =
                    snd group
                    |> Seq.map snd
                { Name = itemKey; Instances = instances }
            )



let createNewSnapshotPath() =
    let relPath = getMomentRelativeSnapshotPath DateTimeOffset.UtcNow
    let path = Path.Combine(stateDirPath, relPath)
    Directory.CreateDirectory(path).FullName

//type ScratchpadState() =
//    member val LastReset = DateTimeOffset.MinValue with get, set
//    member x.Save() =
//        use file = File.CreateText(Path.Combine(stateDirPath, "state"))
//        Newtonsoft.Json.JsonSerializer().Serialize(file, x)
//    static member Load() =
//        use file = File.OpenText(Path.Combine(stateDirPath, "state"))
//        Newtonsoft.Json.JsonSerializer().Deserialize(file, typeof<ScratchpadState>) :?> ScratchpadState

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
    | Some(i) ->
        if i.HasMultipleInstances then
            failwith <| sprintf "multiple items found with name \"%s\"" name
        else
            i.Hoist()
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

ensureScratchpadInitialized()
//execute List

