module rec OnboardingProcessor

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open OnboardingMessages
open Rebus.Bus
open Rebus.Config
open Rebus.Auditing.Messages
open Rebus.Retry.Simple
open Rebus.Routing.TypeBased
open Rebus.ServiceProvider

let configureRebus (rebus: RebusConfigurer) (config: IConfiguration) =
    let asbConnection = config.GetConnectionString("AzureServiceBusConnectionString")
    let sqlConnection = config.GetConnectionString("MsSqlConnectionString")

    rebus.Logging       (fun l -> l.Serilog())                                                                             |> ignore
    rebus.Routing       (fun r -> r.TypeBased().MapAssemblyOf<OnboardNewCustomer>("MainQueue") |> ignore)                  |> ignore
    rebus.Transport     (fun t -> t.UseAzureServiceBus(asbConnection, "MainQueue").AutomaticallyRenewPeekLock() |> ignore) |> ignore
    rebus.Options       (fun t -> t.SimpleRetryStrategy(errorQueueAddress = "ErrorQueue"))                                 |> ignore
    rebus.Options       (fun t -> t.EnableMessageAuditing(auditQueue = "AuditQueue"))                                      |> ignore
    rebus.Sagas         (fun s -> s.StoreInSqlServer(sqlConnection, "Sagas", "SagaIndexes"))                               |> ignore
    rebus

type Backend(configuration: IConfiguration) =
    let mutable provider: ServiceProvider  = null
    let mutable bus: IBus  = null
    do
        let services = ServiceCollection()
        services.AddRebus (fun x -> configureRebus x configuration) |> ignore
        services.AutoRegisterHandlersFromAssemblyOf<Backend>() |> ignore

        provider <- services.BuildServiceProvider()
        provider.UseRebus(Action<IBus>(fun x -> bus <- x)) |> ignore

    interface IDisposable with
        member this.Dispose() =
            printfn "Disposing - tchau!"
            if bus <> null then bus.Dispose()
            if provider <> null then provider.Dispose()

    member this.Bus with get (): IBus = bus
