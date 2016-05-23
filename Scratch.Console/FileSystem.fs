namespace Scratch
open System.IO

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



