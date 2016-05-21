namespace Scratch.Core

type ISymLinkService =
    
    abstract Create : source:string * target:string * overwrite:bool -> unit
    abstract Delete : link:string -> unit
    /// Determines whether the specified path represents a symlink
    abstract Exists : link:string -> bool
    /// Gets the target of the specified symlink.
    abstract GetTarget : junctionPoint:string ->string






