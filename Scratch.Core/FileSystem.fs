namespace Scratch.Core
open System

//module FileSystem =

    type public ArchiveDirectoryPath = {
        Year : int
        Month : int
        Day : int
        Path : string
    }

    type FsObjectType = 
        | File
        | FileLink
        | Directory
        | DirectoryLink

    type FsObject = {
        Type : FsObjectType
        FullName : string
        Name : string
        CreationTime : DateTimeOffset
        ModifiedTime : DateTimeOffset
    }


    //type IFileSystemService =

