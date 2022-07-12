module DogControl.DogControlService

open System.Collections.Concurrent
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open MessagePack
open MessagePack.FSharp
open MessagePack.Resolvers
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging


[<MessagePackObject>]
type Message = {
        [<Key("ly")>]
        ly: float
        [<Key("lx")>]
        lx: float
        [<Key("rx")>]
        rx: float
        [<Key("ry")>]
        ry: float
        [<Key("L2")>]
        L2: int
        [<Key("R2")>]
        R2: int
        [<Key("R1")>]
        R1: int
        [<Key("L1")>]
        L1: int
        [<Key("dpady")>]
        dpady: float
        [<Key("dpadx")>]
        dpadx: float
        [<Key("x")>]
        x: float
        [<Key("square")>]
        square: int
        [<Key("circle")>]
        circle: int
        [<Key("triangle")>]
        triangle: int
        [<Key("message_rate")>]
        message_rate: int
        [<Key("do_action_name")>]
        do_action_name: string
}

let serialize options (value: Message) =
    MessagePackSerializer.Serialize(value, options)
    

let resolver =
    Resolvers.CompositeResolver.Create(
        FSharpResolver.Instance,
        StandardResolver.Instance
        )

let options = MessagePackSerializerOptions.Standard.WithResolver(resolver)


let msg = { ly=0.0 // X Velocity (forward, back)
            lx = 0.0 // Y velocity (side to side, left/right)
            rx = 0.0 // Yaw Rate
            ry = 0.0 // adjust pitch
            L2 = 0 // Change dance method, but there is only one right now
            R2 = 0  // toggle the gait mode (trotting, etc, but there is only one)
            R1 = 0 // toggle trotting
            L1 = 0 // start
            dpady = 0.0 // adjust height
            dpadx = 0.0 // adjust roll
            x = 0 // hop
            square = 0 // not used
            circle = 0 // start dancing
            triangle = 0 // Hold to shut down (3 seconds)
            message_rate = 20
            do_action_name = ""}

let sendMessage (client: UdpClient) (m:Message) =
    let b = serialize options m
    client.Send (b, b.Length)
    
let sendNone (client:UdpClient) =
    sendMessage client msg
    
let sendAction (client:UdpClient) (action:string)=
    printfn "Send Action: '%s'" action
    let m = { msg with
                  circle = 1
                  do_action_name = action }
   
    printfn "%i" <| sendMessage client m
    ()
    
let sendActivate (client:UdpClient) =
    let m = { msg with L1 = 1 }
    printfn "Activate"
    sendMessage client m
    
type DogControlQueue(logger: ILogger<DogControlQueue>) =
    
    let q = new ConcurrentQueue<string> ()
    
    member this.Enqueue (action:string) =
        q.Enqueue (action)
        
    member this.Dequeue () =
        let e, action = q.TryDequeue ()
        match e with
        | true -> Some (action)
        | _ -> None

type DogControlService(logger:ILogger<DogControlService>,
                       queue: DogControlQueue) =
        inherit BackgroundService()

        override this.ExecuteAsync(stoppingToken) =
            logger.LogInformation ("Starting up background service...")
            let client = new UdpClient()
            //let destIp = IPAddress.Parse("192.168.2.101")
            //let destIp = IPAddress.Parse("192.168.137.58")
            //let destIp = IPAddress.
            //let destIp = IPAddress.Parse("192.168.1.61")
            client.Connect("dogpi.local", 8830)
            
            logger.LogInformation("Ready")
            
            let doAction (a:string) =
                logger.LogInformation (sprintf "Do action: '%s'" a)
                match a with
                | "activate" -> sendActivate client |> ignore
                | _ -> sendAction client a
                ()
            
            task {
                while not stoppingToken.IsCancellationRequested do
                    let v = queue.Dequeue ()
                    match v with
                    | Some _ -> Option.iter doAction v
                    | None -> sendNone client |> ignore
                    // We can't go faster than 50 milliseconds between messages
                    do! Task.Delay(50)
                ()
            } :> Task