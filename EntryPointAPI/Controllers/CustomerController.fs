namespace EntryPointAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

[<ApiController>]
type CustomerController (logger : ILogger<CustomerController>) =
    inherit ControllerBase()
    let _logger = logger

    [<HttpPost>]
    [<Route("newcustomer")>]
    member _.Get() =
        Ok()