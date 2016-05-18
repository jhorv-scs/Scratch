namespace Scratch.Desktop
module Program =

    open System
    open Scratch

    [<EntryPoint>]
    [<STAThread>]
    let Main(args) = 
        let app = new Eto.Forms.Application(Eto.Platform.Detect)
        app.Run(new MainForm())
        0