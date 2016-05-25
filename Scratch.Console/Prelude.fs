[<AutoOpen>]
module Scratch.Prelude
open System
open System.IO

let logException (ex: Exception) =
    System.Console.Error.WriteLine(ex)

type System.DateTimeOffset with
    member x.ToFileSystemFriendlyName () =
        x.ToString("O").Replace(':', '_');
    
    static member FromFileSystemFriendlyName (x: string) =
        DateTimeOffset.ParseExact(x.Replace('_', ':'), "O", null)


