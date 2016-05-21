namespace Scratch.Core
open System

type public IScratchpadStateService =
    abstract LastScan : DateTimeOffset with get, set
    abstract SaveChanges : unit -> unit


