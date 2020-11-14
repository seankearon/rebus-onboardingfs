module rec OnboardingProcessor

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open OnboardingMessages
open Rebus.Bus
open Rebus.Config
open Rebus.Persistence.FileSystem
open Rebus.Retry.Simple
open Rebus.Routing.TypeBased
open Rebus.Sagas
open Rebus.ServiceProvider
open Rebus.Transport.FileSystem
open FSharp.Control.Tasks.V2.ContextInsensitive

let configureRebus (rebus: RebusConfigurer) =
    rebus.Logging       (fun l -> l.Serilog())                                                            |> ignore
    rebus.Routing       (fun r -> r.TypeBased().MapAssemblyOf<OnboardNewCustomer>("MainQueue") |> ignore) |> ignore
    rebus.Transport     (fun t -> t.UseFileSystem("c:/rebus-advent", "MainQueue") |> ignore)              |> ignore
    rebus.Options       (fun t -> t.SimpleRetryStrategy(errorQueueAddress = "ErrorQueue"))                |> ignore
    rebus.Sagas         (fun s -> s.UseFilesystem("c:/rebus-advent/sagas"))                               |> ignore
    rebus

type Backend() =
    let mutable provider: ServiceProvider  = null
    let mutable bus: IBus  = null
    do
        let services = ServiceCollection()
        services.AddRebus configureRebus |> ignore
        services.AutoRegisterHandlersFromAssemblyOf<Backend>() |> ignore

        provider <- services.BuildServiceProvider()
        provider.UseRebus(Action<IBus>(fun x -> bus <- x)) |> ignore

    interface IDisposable with
        member this.Dispose() =
            printfn "Disposing - tchau!"
            if bus <> null then bus.Dispose()
            if provider <> null then provider.Dispose()

    member this.Bus with get (): IBus = bus


type OnboardingSagaData() =
    interface ISagaData with
        member this.Id
            with get () = this.Id
            and set (value) = this.Id <- value
        member this.Revision
            with get () = this.Revision
            and set (value) = this.Revision <- value

    member val Id       = Guid.Empty with get, set
    member val Revision = 0          with get, set

    member val CustomerName  = "" with get, set
    member val CustomerEmail = "" with get, set
    member val AccountId     = 0  with get, set

    member val AccountCreated     = false with get, set
    member val WelcomeEmailSent   = false with get, set
    member val SalesCallScheduled = false with get, set

    member this.Completed() = this.AccountCreated && this.WelcomeEmailSent && this.SalesCallScheduled

let CustomerEmail =
    // https://github.com/fsharp/fslang-design/blob/master/preview/FS-1003-nameof-operator.md#names-of-instance-membersabove
    let x = OnboardingSagaData()
    nameof x.CustomerEmail

let AccountId =
    let x = OnboardingSagaData()
    nameof x.AccountId

type OnboardingSaga(b: IBus) =
    inherit Saga<OnboardingSagaData>()
    let bus = b

    override this.CorrelateMessages(config: ICorrelationConfig<OnboardingSagaData>) =
        config.Correlate<OnboardNewCustomer>     (Func<OnboardNewCustomer,     obj>(fun m -> m.Email     :> obj), CustomerEmail)
        config.Correlate<CustomerAccountCreated> (Func<CustomerAccountCreated, obj>(fun m -> m.Email     :> obj), CustomerEmail)
        config.Correlate<WelcomeEmailSent>       (Func<WelcomeEmailSent,       obj>(fun m -> m.AccountId :> obj), AccountId)
        config.Correlate<SalesCallScheduled>     (Func<SalesCallScheduled,     obj>(fun m -> m.AccountId :> obj), AccountId)

    member this.TryComplete() =
        if this.Data.Completed() then this.MarkAsComplete()

    // This is to allows access to IsNew from inside the interface sections below.
    member this.IsNew = base.IsNew

    interface IAmInitiatedBy<OnboardNewCustomer> with
        member this.Handle(m: OnboardNewCustomer) =
            task {
                if not this.IsNew then return ()

                this.Data.CustomerName  <- m.Name
                this.Data.CustomerEmail <- m.Email

                do! bus.Send(CreateCustomerAccount.For m.Name m.Email)

                this.TryComplete()
            } :> Task

    interface IAmInitiatedBy<CustomerAccountCreated> with
        member this.Handle(m: CustomerAccountCreated) =
            task {
                this.Data.AccountId      <- m.AccountId
                this.Data.AccountCreated <- true

                do! bus.Send( SendWelcomeEmail.For  m.AccountId)
                do! bus.Send( ScheduleSalesCall.For m.AccountId)

                this.TryComplete()
            } :> Task

    interface IAmInitiatedBy<WelcomeEmailSent> with
        member this.Handle(_: WelcomeEmailSent) =
            task {
                this.Data.WelcomeEmailSent <- true
                this.TryComplete()
            } :> Task

    interface IAmInitiatedBy<SalesCallScheduled> with
        member this.Handle(_: SalesCallScheduled) =
            task {
                this.Data.SalesCallScheduled <- true
                this.TryComplete()
            } :> Task
