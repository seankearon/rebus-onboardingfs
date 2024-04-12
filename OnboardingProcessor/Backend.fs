module rec OnboardingProcessor

open System
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open OnboardingMessages
open Rebus.Bus
open Rebus.Config
open Rebus.Auditing.Messages
open Rebus.Retry.Simple
open Rebus.Routing.TypeBased
open Rebus.ServiceProvider

let configureRebus (config: IConfiguration) (rebus: RebusConfigurer) =
    let asbConnection = config.GetConnectionString("AzureServiceBusConnectionString")
    let sqlConnection = config.GetConnectionString("MsSqlConnectionString")

    rebus.Logging       (fun l -> l.Serilog())                                                                             |> ignore
    rebus.Routing       (fun r -> r.TypeBased().MapAssemblyOf<OnboardNewCustomer>("MainQueue") |> ignore)                  |> ignore
    rebus.Transport     (fun t -> t.UseAzureServiceBus(asbConnection, "MainQueue").AutomaticallyRenewPeekLock() |> ignore) |> ignore
    rebus.Options       (fun t -> t.RetryStrategy(errorQueueName = "ErrorQueue"))                                          |> ignore
    rebus.Options       (fun t -> t.EnableMessageAuditing(auditQueue = "AuditQueue"))                                      |> ignore
    rebus.Sagas         (fun s -> s.StoreInSqlServer(sqlConnection, "Sagas", "SagaIndexes"))                               |> ignore
    rebus

type Backend(configuration: IConfiguration) =
    let mutable provider: ServiceProvider  = null
    let mutable bus: IBus  = null
    do
        // This will be called by Rebus when the bus is created. 
        let rebusOnCreated (x: IBus) =
            task {
                bus <- x
            }
            :> Task
        
        let services = ServiceCollection()
        services.AddRebus (configure=(configureRebus configuration), onCreated=rebusOnCreated) |> ignore
        services.AutoRegisterHandlersFromAssemblyOf<Backend>() |> ignore

        provider <- services.BuildServiceProvider()
        provider.StartRebus() |> ignore

    interface IDisposable with
        member this.Dispose() =
            printfn "Disposing - tchau!"
            if bus <> null then bus.Dispose()
            if provider <> null then provider.Dispose()

    member this.Bus with get (): IBus = bus
