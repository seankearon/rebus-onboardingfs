open System
open Microsoft.Extensions.Configuration
open OnboardingProcessor
open Serilog
open Topper

[<EntryPoint>]
let main _ =
    Log.Logger <- LoggerConfiguration()
       .WriteTo.Console()
       .CreateLogger()

    let profile       = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    let isDevelopment = profile = "Development"

    let configurationRoot (builder: IConfigurationBuilder): IConfigurationRoot  =
        // https://andrewlock.net/sharing-appsettings-json-configuration-files-between-projects-in-asp-net-core/
        let builder' =
            builder
                .AddJsonFile("appsettings.json", optional=false)
                .AddEnvironmentVariables()

        if isDevelopment then
            builder'
                .AddUserSecrets(System.Reflection.Assembly.GetEntryAssembly(), optional=true)
                .Build()
        else
            builder'.Build()


    let builder = ConfigurationBuilder ()
    let configuration = builder.SetBasePath(System.Reflection.Assembly.GetExecutingAssembly().Location) |> configurationRoot




    let serviceConfiguration = ServiceConfiguration().Add("OurBackendBus", fun () -> (new Backend(configuration) :> IDisposable))
    ServiceHost.Run(serviceConfiguration);

    0