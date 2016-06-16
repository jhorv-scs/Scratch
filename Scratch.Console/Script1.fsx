//#load "Scripts/load-project-debug.fsx"
#load "Scripts/load-references-debug.fsx"
#load "AssemblyInfo.fs"
      "SymbolicLink.fs"

open System
open System.Collections.Generic
open System.IO
//open Scratch
//open Scratch.Scratchpad

[<AutoOpen>]
module FileSystem =
//    let enumerateTopLevelNonLinkDirectoryFullNames (p: string) =
//        Directory.EnumerateDirectories p
//
//    let enumerateTopLevelNonLinkDirectoryNames (p: string) =
//        enumerateTopLevelNonLinkDirectoryFullNames p
//        |> Seq.map Path.GetFileName

    let enumerateTopLevelNonLinkDirectoryInfos (p: string) =
        Directory.EnumerateDirectories p
        |> Seq.map DirectoryInfo
        |> Seq.where (fun di -> di.Attributes &&& FileAttributes.ReparsePoint <> FileAttributes.ReparsePoint)

type System.DateTimeOffset with
    member x.ToString() =
        x.ToString("O").Replace(':', '_');



[<AutoOpen>]
module Time =
    let tryParseExactDateTimeOffset (format: string) (input: string) =
        match DateTimeOffset.TryParseExact(input, format, null, Globalization.DateTimeStyles.None) with
        | true, r -> Some r
        | _ -> None

    let tryParseYear input = tryParseExactDateTimeOffset "yyyy" input
    let tryParseYearMonth input = tryParseExactDateTimeOffset "yyyyMM" input
    let tryParseYearMonthDate input = tryParseExactDateTimeOffset "yyyyMMdd" input
    

type Scratchpad = {
    Moment : DateTimeOffset
    Path : string
    Items : HashSet<string>
}
with
    override x.ToString() =
        sprintf "%s;%s;%O" (x.Moment.ToString("yyyyMMdd")) x.Path x.Items

    static member EnumerateScratchpads (workingDir: string) =
        seq {
            for di in enumerateTopLevelNonLinkDirectoryInfos workingDir do
                match tryParseYearMonthDate di.Name with
                | Some moment ->
                    let items =
                        enumerateTopLevelNonLinkDirectoryInfos di.FullName
                        |> Seq.map (fun di -> di.Name)
                    yield {
                        Moment = moment;
                        Path = di.FullName;
                        Items = HashSet<string>(items, StringComparer.OrdinalIgnoreCase)
                    }
                | _ -> ()
        }
           
type ScratchpadItem = {
    Name : string
    Scratchpads : Scratchpad seq
}
with
    //static member EnumerateScratchpadItems (workingDir: string) =
    //    Scratchpad.EnumerateScratchpads workingDir
    static member EnumerateItems (scratchpads : Scratchpad seq) =
        scratchpads
        |> Seq.collect (fun sp ->
                sp.Items
                |> Seq.map (fun i -> (sp, i))
            )
        |> Seq.groupBy snd
        |> Seq.map (fun group ->
            {
                Name = fst group;
                Scratchpads = (snd group |> Seq.map fst)
            })

type ScratchpadItemKey = {
    Name : string
    WhenId : string
}
//with
//    static member EnumerateScratchpadItemKeys (spi : ScratchpadItem seq) =
        

fsi.AddPrinter (fun (x:Scratchpad) -> x.ToString())

let workingDir = @"D:\Scratchpad"

Scratchpad.EnumerateScratchpads workingDir
|> ScratchpadItem.EnumerateItems


