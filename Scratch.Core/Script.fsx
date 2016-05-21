﻿// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "PortableLibrary1.fs"
open Scratch.Core

open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Reflection

type Message = string
exception TypeResolutionException of Message * Type
type Lifetime = Singleton | Transient
type AbstractType = Type
type ConcreteType = Type
type private Constructor = Reflected of ConcreteType | Factory of (unit -> obj)
let private (|FunType|_|) t =
    if FSharpType.IsFunction t then FSharpType.GetFunctionElements t |> Some
    else None
let private asOption = function Choice1Of2 x -> Some x | Choice2Of2 _ -> None
/// IoC Container
type Container () as container =
    let catalog = Dictionary<AbstractType, Constructor * Lifetime>()
    let singletons = Dictionary<ConcreteType,obj>()
    let rec tryResolve cs t =
        match catalog.TryGetValue t with
        | true, (Reflected u , lifetime) -> 
            tryObtain u (fun () -> tryReflect cs u) lifetime
        | true, (Factory f, lifetime) -> 
            tryObtain t (fun () -> f() |> Choice1Of2) lifetime
        | false, _ ->  
            tryObtain t (fun () -> tryReflect cs t) Singleton 
    and tryObtain t f lifetime =
        match singletons.TryGetValue t with
        | true, value -> Choice1Of2(value)
        | false, _ ->
            let result = f()
            result |> function Choice1Of2 value -> store t value lifetime | Choice2Of2 _ -> ()
            result
    and store t value = function Singleton -> singletons.Add(t,value) | Transient -> ()
    and tryReflect cs t =
        if cs |> List.exists ((=) t) then Choice2Of2 "Cycle detected" else tryConstructors (t::cs) t
    and tryConstructors cs t =
        let constructors =
            t.GetConstructors()
            |> Array.sortBy (fun c -> c.GetParameters().Length)
            |> Seq.map (tryConstructor cs)
        match constructors |> Seq.tryPick asOption with
        | Some value -> Choice1Of2 value
        | None -> constructorsError t constructors |> Choice2Of2
    and constructorsError t constructors =
        let constructors = constructors |> Seq.map (function Choice1Of2 _ -> "" | Choice2Of2 x -> x)
        "Failed to match constructor from:\r\n" + (constructors |> String.concat "\r\n")
    and tryConstructor cs ci =
        let ps = ci.GetParameters()
        let args = ps |> Array.map (fun p -> tryResolveArgument cs p.ParameterType)
        let args' = args |> Array.choose asOption
        if args'.Length = ps.Length then args' |> ci.Invoke |> Choice1Of2
        else constructorError ci.DeclaringType ps args |> Choice2Of2
    and constructorError t ps args =
        let ps = ps |> Seq.map (fun p -> p.Name + ":" + p.ParameterType.Name)
        let invalidArgs = args |> Seq.choose (function Choice2Of2 s -> Some s | Choice1Of2 _ -> None)
        t.Name + "(" + (String.concat "," ps) + ") -> " + (String.concat "\r\n" invalidArgs)
    and tryResolveArgument cs t =
        match t with
        | FunType(arg,result) when arg = typeof<unit> ->
            FSharpValue.MakeFunction(t,fun args -> container.Resolve(result)) |> Choice1Of2
        | t when t.IsPrimitive -> Choice2Of2 "Primitive arguments not supported"
        | t when t = typeof<string> -> Choice2Of2 "String arguments not supported"
        | t -> tryResolve cs t
    /// Register sequence of abstract types against specified concrete type
    member container.Register(abstractTypes:AbstractType seq, concreteType:ConcreteType) =
        for t in abstractTypes do catalog.Add(t, (Reflected concreteType, Singleton))
    /// Register abstract type against specified type instance
    member container.Register<'TAbstract>(instance:'TAbstract) =
        catalog.Add(typeof<'TAbstract>, (Reflected typeof<'TAbstract>, Singleton))
        singletons.Add(typeof<'TAbstract>, instance)
    /// Register abstract type against specified concrete type with given lifetime
    member container.Register<'TAbstract when 'TAbstract : not struct>
            (concreteType:ConcreteType, lifetime:Lifetime) =
        let abstractType = typeof<'TAbstract>
        if concreteType <> abstractType &&
           not (concreteType.IsSubclassOf(abstractType)) &&
           not (concreteType.GetInterfaces() |> Array.exists ((=) abstractType)) then
            invalidArg "concreteType" "Concrete type must implement abstract type"
        catalog.Add(abstractType, (Reflected concreteType, lifetime))
    /// Register abstract type against specified factory with given lifetime
    member container.Register<'TAbstract when 'TAbstract : not struct>
            (f:unit->'TAbstract, lifetime:Lifetime) = 
        catalog.Add(typeof<'TAbstract>, (Factory(f >> box), lifetime))
    /// Resolve instance of specified abstract type
    member container.Resolve<'TAbstract when 'TAbstract : not struct>() =
        container.Resolve(typeof<'TAbstract>) :?> 'TAbstract
    /// Resolve instsance of specified abstract type
    member container.Resolve(abstractType:AbstractType) =
        match tryResolve [] abstractType with
        | Choice1Of2 value -> value
        | Choice2Of2 message -> TypeResolutionException(message,abstractType) |> raise
    /// Remove instance reference from container
    member container.Release(instance:obj) =
        singletons |> Seq.filter (fun pair -> pair.Value = instance) |> Seq.toList
        |> List.iter (fun pair -> singletons.Remove(pair.Key) |> ignore)

//open NUnit.Framework
//
//[<TestFixture>]
//module ``Container Register, Resolve, Release Tests`` =
//    
//    [<AbstractClass>]
//    type AbstractType () = do ()
//
//    type ConcreteType () = inherit AbstractType()
//
//    type IMarkerInterface = interface end
//
//    type MarkedType () = interface IMarkerInterface
//    
//    let [<Test>] ``registering 2 instances of an abstract type in a single container should throw`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<AbstractType>, Singleton)
//        Assert.Throws<System.ArgumentException>(fun () ->
//            container.Register<AbstractType>(typeof<AbstractType>, Singleton) |> ignore
//        ) |> ignore
//
//    let [<Test>] ``registering a concrete type that does not implement the abstract type should throw`` () =
//        let container = Container()
//        Assert.Throws<System.ArgumentException>(fun () ->
//            container.Register<MarkedType>(typeof<AbstractType>, Singleton)
//        ) |> ignore
//
//    let [<Test>] ``attempting to resolve an unregistered type should throw`` () =
//        let container = Container()
//        Assert.Throws<TypeResolutionException>(fun () ->  
//            container.Resolve<AbstractType>() |> ignore
//        ) |> ignore
//
//    let [<Test>] ``resolving a registered abstract type should return an instance of the specified concrete type`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<ConcreteType>, Singleton)
//        let instance = container.Resolve<AbstractType>()
//        Assert.True(instance :? ConcreteType)
//
//    let [<Test>] ``resolving a type with a singleton lifetime should always return the same instance`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<ConcreteType>, Singleton)
//        let a = container.Resolve<AbstractType>()
//        let b = container.Resolve<AbstractType>()
//        Assert.True( Object.ReferenceEquals(a,b) )
//        
//    let [<Test>] ``resolving a type with a transient lifetime should a new instance each time`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<ConcreteType>, Transient)
//        let a = container.Resolve<AbstractType>()
//        let b = container.Resolve<AbstractType>()
//        Assert.AreNotSame(a,b)
//
//    let [<Test>] ``resolving a registered instance of a type should return that instance`` () =
//        let container = Container()
//        let this = ConcreteType()
//        container.Register<AbstractType>(this)
//        let that = container.Resolve<AbstractType>()
//        Assert.AreSame(this, that)
//
//    let [<Test>] ``resolving a type registered as a factory should call the specified factory`` () =
//        let called = ref false
//        let factory = fun () -> called := true; ConcreteType() :> AbstractType
//        let container = Container()
//        container.Register<AbstractType>(factory, Singleton)
//        container.Resolve<AbstractType>() |> ignore
//        Assert.True( called.Value )
//
//    let [<Test>] ``releasing a registered concrete instance then resolving the type should return a new concrete instance`` () =
//        let container = Container()
//        let this = ConcreteType()
//        container.Register<ConcreteType>(this)
//        container.Release(this)
//        let that = container.Resolve<ConcreteType>()
//        Assert.True( not <| Object.ReferenceEquals(this, that) )
//
//    do
//        ``registering 2 instances of an abstract type in a single container should throw`` ()
//        ``attempting to resolve an unregistered type should throw`` ()
//        ``resolving a registered abstract type should return an instance of the specified concrete type``  ()
//        ``resolving a type with a singleton lifetime should always return the same instance`` ()
//        ``resolving a type with a transient lifetime should a new instance each time`` ()
//        ``resolving a registered instance of a type should return that instance`` ()
//        ``resolving a type registered as a factory should call the specified factory`` ()
//        ``releasing a registered concrete instance then resolving the type should return a new concrete instance`` ()
//
//[<TestFixture>]
//module ``Constructor Tests`` =
//    
//    [<AbstractClass>]
//    type AbstractType () = do ()
//   
//    type ConstructorWithValueTypeArg (arg:int) = inherit AbstractType()
//
//    let [<Test>] ``resolving type with value type dependency in constructor should throw`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<ConstructorWithValueTypeArg>, Singleton)
//        Assert.Throws<TypeResolutionException>(fun () ->
//            container.Resolve<AbstractType>() |> ignore
//        ) |> ignore
//
//    type ReferenceType() = do ()
//    type ConstructorWithReferenceTypeArg (arg:ReferenceType) = inherit AbstractType()
//
//    let [<Test>] ``resolving type with reference type dependency in constructor should inject reference`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<ConstructorWithReferenceTypeArg>, Singleton)
//        let instance = container.Resolve<AbstractType>()
//        Assert.NotNull(instance)
//
//    type ConstructorWithSelfReferenceArg (arg:AbstractType) = inherit AbstractType()
//
//    let [<Test>] ``resolving type with self type dependency in constructor should fail`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<ConstructorWithSelfReferenceArg>, Singleton)
//        Assert.Throws<TypeResolutionException>(fun () ->
//                container.Resolve<AbstractType>() |> ignore
//        ) |> ignore
//
//    type Cyclic(arg:ConstructorWithCyclicReferenceArg) = do ()
//    and  ConstructorWithCyclicReferenceArg (arg:Cyclic) = do ()
//
//    let [<Test>] ``resolving type with cyclic type dependency in constructor should fail`` () =
//        let container = Container()
//        container.Register<ConstructorWithCyclicReferenceArg>(typeof<ConstructorWithCyclicReferenceArg>, Singleton)
//        Assert.Throws<TypeResolutionException>(fun () ->
//                container.Resolve<AbstractType>() |> ignore
//        ) |> ignore
//
//    type ConstructorWithFunArg (arg:unit -> ReferenceType) = 
//        inherit AbstractType()
//        member this.Factory () = arg()
//
//    let [<Test>] ``resolving type with fun type argument in constructor should inject factory`` () =
//        let container = Container()
//        container.Register<AbstractType>(typeof<ConstructorWithFunArg>, Singleton)
//        let instance = container.Resolve<AbstractType>() :?> ConstructorWithFunArg
//        let refValue = instance.Factory()
//        Assert.NotNull(refValue)
//
//    do  ``resolving type with value type dependency in constructor should throw`` ()
//        ``resolving type with reference type dependency in constructor should inject reference`` ()
//        ``resolving type with self type dependency in constructor should fail`` ()
//        ``resolving type with cyclic type dependency in constructor should fail`` ()
//        ``resolving type with fun type argument in constructor should inject factory`` ()

module Usage =

    type ICalculate =
        abstract member Incr : int -> int

    type Calculator () =
        interface ICalculate with
            member this.Incr(x:int) = x + 1
    
    let container = Container()

    container.Register<ICalculate>(typeof<Calculator>, Singleton)

    let calc = container.Resolve<ICalculate>()
    printfn "%d" (calc.Incr 1)

    container.Release(calc)