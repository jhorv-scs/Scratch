namespace Scratch

open System
open Eto.Forms
open Eto.Drawing
open Eto.Serialization.Xaml

type SettingsDialog () as this =
    inherit Dialog ()

    do
        XamlReader.Load(this, "SettingsDialog.xeto")

    member this.DefaultButton_Click(sender: obj, e: EventArgs) =
        this.Close()

    member this.AbortButton_Click(sender: obj, e: EventArgs) =
        this.Close()