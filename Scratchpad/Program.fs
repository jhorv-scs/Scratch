// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.





// Current Command-line arg processor:  https://github.com/gsscoder/commandline/tree/master
open CommandLine


[<Verb("list", HelpText="List scratchpad items.")>]
type ListOptions = {
    //[<Option>]
    Search : string
}
with
    member this.RunAndReturnExitCode() = 0

[<Verb("echo", HelpText="Testing!")>]
type EchoOptions = {
    [<Option>]
    Value : string
}
with
    member this.RunAndReturnExitCode() = 0

[<Verb("alpha", HelpText="Alpha options.")>]
type AlphaOptions = {
    [<Option>]Value : string
}
with member this.RunAndReturnExitCode() = 0


let run args =
    let parseResult =
        try
            CommandLine.Parser.Default.ParseArguments<ListOptions, EchoOptions> args
        with
            | :? System.NullReferenceException ->
                failwith <| sprintf "Programming error - the <Verb> specified is not properly defined."

    match parseResult with
    | :? CommandLine.Parsed<obj> as verb ->
        match verb.Value with
        | :? ListOptions as opts -> opts.RunAndReturnExitCode()
        | :? EchoOptions as opts -> opts.RunAndReturnExitCode()
        | x -> failwith <| sprintf "Programming error - unhandled <Verb> type %O" (x.GetType())
    | :? CommandLine.NotParsed<obj> as notParsed ->
        notParsed.Errors |> Seq.iter (fun e -> e.ToString() |> printfn "%s")
        1
    | _ -> failwith "Programming error - unexpected response from ParseArguments<>"

[<EntryPoint>]
let main args =
    let returnValue =
        try
            run args
        with x -> printfn "%O" x; 1
    returnValue



