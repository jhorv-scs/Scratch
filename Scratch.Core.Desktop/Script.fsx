// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

//#load "Library1.fs"
#load "Scripts/load-project-debug.fsx"
open System
open System.IO
open Scratch.Core

// Define your library scripting code here


let rootPath = @"D:\Scratchpad"
let archivePath = Path.Combine(rootPath, ".scratchpad")


let ensureScratchpadInitialized() =
    Directory.CreateDirectory(rootPath) |> ignore
    Directory.CreateDirectory(archivePath) |> ignore

ensureScratchpadInitialized()


let scanArchive() =

    let archiveDirPaths = 
        Directory.EnumerateDirectories(archivePath)
        |> Seq.map (fun p -> { Year = Int32.Parse(Path.GetFileName(p)); Month = 0; Day = 0; Path = p })
        |> Seq.map (fun adp ->
            Directory.EnumerateDirectories(adp.Path)
            |> Seq.map (fun p -> { adp with Month = Int32.Parse(Path.GetFileName(p)); Day = 0; Path = p })
            |> Seq.map (fun adp ->
                Directory.EnumerateDirectories(adp.Path)
                |> Seq.map (fun p -> { adp with Day = Int32.Parse(Path.GetFileName(p)); Path = p })
                )
            )
//    archiveDirPaths
//    |> Seq.map()

    Directory.EnumerateDirectories(archivePath)
    |> Seq.map (fun p -> { Year = Int32.Parse(Path.GetFileName(p)); Month = 0; Day = 0; Path = p })


//Directory.EnumerateFiles(rootPath)
////|> Seq.where (fun p -> !dailyDirectoryName.Equals(Path.GetFileName(p), StringComparison.OrdinalIgnoreCase))
//|> Seq.map FileInfo



