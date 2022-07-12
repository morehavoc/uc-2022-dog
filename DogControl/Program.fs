module DogControl.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open DogControl.DogControlService

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "DogControl" ]
                // <meta name="viewport" content="width=device-width, initial-scale=1">
                meta [_name "viewport"; _content "width=device-width, initial-scale=1"]
                link [ _rel "stylesheet"
                       _href  "https://cdn.jsdelivr.net/npm/bulma@0.9.4/css/bulma.min.css" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] [
                div [ _class "container" ] content
                script [_src "scripts.js"] []
            ]
        ]

    let partial () =
        h1 [] [ encodedText "DogControl" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
            div [ _class "columns" ] [
                div [ _class "column is-flex-direction-column is-one-fifth is-flex is-align-items-center" ] [
                    button [ _class "button is-large is-warning is-fullwidth m-5"; _onclick "sendAction('activate');" ] [
                        encodedText "Activate"
                    ]
                    button [ _class "button is-large is-success is-fullwidth m-5"; _onclick "sendAction('shake');"] [
                        encodedText "Shake"
                    ]
                    button [ _class "button is-large is-success is-fullwidth m-5"; _onclick "sendAction('push-up');" ] [
                        encodedText "Push-ups"
                    ]
                ]
                div [ _class "column" ] [
                    
                ]
                
            ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = "" }
    let view      = Views.index model
    htmlView view
    
    
let someHttpHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let fooBar = ctx.GetService<DogControlQueue>()
            let h, v = ctx.Request.Query.TryGetValue ("action")
            match h with
            | true -> fooBar.Enqueue v
            | _ -> ()
            |> ignore
            //let view = Views.index { Text = v }
            //htmlView view next ctx
            return! ctx.WriteJsonAsync { Text = v }
        }

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
                route "/action" >=> someHttpHandler
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<DogControlQueue> () |> ignore
    services.AddHostedService<DogControlService> () |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0