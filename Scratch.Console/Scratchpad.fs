module Scratch.Scratchpad
open System
open System.IO

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
    //member x.HasMultipleInstances with get() = (Seq. length x.Instances) > 1

    static member EnumerateAll () =
        ScratchpadInstance.EnumerateAll()
        |> Seq.collect (fun s ->
            s.Objects
            |> Seq.where (fun fso -> fso.Type = Scratch.FsObjectType.Directory)
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



