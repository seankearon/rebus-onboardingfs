module rec Handlers

open System
open System.Threading.Tasks
open OnboardingMessages
open Rebus.Bus
open Rebus.Handlers
open FSharp.Control.Tasks.V2.ContextInsensitive

type CreateCustomerAccountHandler(b: IBus) =
    let bus = b

    interface IHandleMessages<CreateCustomerAccount> with
        member x.Handle(m: CreateCustomerAccount) =
            task
                {
                    do! Task.Delay(500) // Pretend we're doing something!
                    do! bus.Reply(CustomerAccountCreated.For m.Email (Random().Next()))
                } :> Task

type SendWelcomeEmailHandler(b: IBus) =
    let bus = b

    interface IHandleMessages<SendWelcomeEmail> with
        member x.Handle(m: SendWelcomeEmail) =
            task
                {
                    do! Task.Delay(500) // Pretend we're doing something!
                    do! bus.Reply(WelcomeEmailSent.For m.AccountId)
                } :> Task

type ScheduleSalesCallHandler(b: IBus) =
    let bus = b

    interface IHandleMessages<ScheduleSalesCall> with
        member x.Handle(m: ScheduleSalesCall) =
            task
                {
                    do! Task.Delay(500) // Pretend we're doing something!
                    do! bus.Reply(SalesCallScheduled.For m.AccountId)
                } :> Task