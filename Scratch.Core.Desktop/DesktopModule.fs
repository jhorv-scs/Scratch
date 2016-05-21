[<AutoOpen>]
module Scratch.Core.DesktopModule

do
    Scratch.Core.IoC.Container.Instance.Register(ScratchpadStateService, Scratch.Core.IoC.Lifetime.Transient)

