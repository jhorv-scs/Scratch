module Scratch.Core.Scratchpad

open System.IO



type ScratchPadObject =
    | FileRef of string
    | DirectoryRef of string

let getFullNameOfRoot() = @"D:\Scratchpad"
//let getRootDirectoryInfo() = System.IO.
let getNameOfTodayRef() = "today"

//let getWorkspaceItems () =

//let createToday
