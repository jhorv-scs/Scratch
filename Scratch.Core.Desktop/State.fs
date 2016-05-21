namespace Scratch.Core
open System


type ScratchpadStateService() =

    member val LastScan = DateTimeOffset.MinValue with get, set
    
    interface IScratchpadStateService with
        member x.LastScan
            with get() = x.LastScan
            and set v = x.LastScan <- v

        member x.SaveChanges() = ()
        

