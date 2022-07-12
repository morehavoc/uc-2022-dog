

// For more information see https://aka.ms/fsharp-console-apps

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open MessagePack
open MessagePack.FSharp
open MessagePack.Resolvers


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
    
let sendActivate (client:UdpClient) =
    let m = { msg with L1 = 1 }
    printfn "Activate"
    sendMessage client m
    
let sendPushUp (client:UdpClient) =
    //let m = { msg with circle = 1 }
    let m = { msg with circle = 1; do_action_name = "push-up" }
    printfn "Dance"
    sendMessage client m
    
    
let sendShake (client:UdpClient) =
    //let m = { msg with circle = 1 }
    let m = { msg with circle = 1; do_action_name = "shake" }
    printfn "Dance"
    sendMessage client m
    
    
let sendHop (client:UdpClient) =
    let m = { msg with x = 1 }
    printfn "Hop"
    sendMessage client m
    
let sendTrot (client:UdpClient) =
    let m = { msg with R1 = 1 }
    printfn "Trot"
    sendMessage client m
    
let sendGait (client:UdpClient) =
    let m = { msg with R2 = 1 }
    printfn "Gait"
    sendMessage client m
let sendForward (client:UdpClient) =
    let m = { msg with ly = 1 }
    printfn "Forward"
    sendMessage client m
    
let sendBackward (client:UdpClient) =
    let m = { msg with ly = -1 }
    printfn "Backward"
    sendMessage client m
    
let sendHeight (client:UdpClient) h =
    let m = { msg with dpady = h }
    printfn "Height"
    sendMessage client m
    
let sendRoll (client:UdpClient) r =
    let m = {msg with dpadx = r}
    printfn "Roll"
    sendMessage client m
   
let sendPitch (client:UdpClient) p =
    let m = {msg with ry = p}
    printfn "Pitch"
    sendMessage client m
let sendYaw (client:UdpClient) y =
    let m = {msg with rx = y}
    printfn "yaw"
    sendMessage client m
    
let client = new UdpClient()
let destIp = IPAddress.Parse("192.168.2.101")
//let destIp = IPAddress.Parse("192.168.1.61")
client.Connect(destIp, 8830)
//let sendBytes = Encoding.ASCII.GetBytes("Is anybody there?")

// create a loop and listen for key presses
while true do
    // ignore no key press
    while not Console.KeyAvailable do
        Thread.Sleep (50)
        sendNone client |> ignore
        
    match Console.ReadKey(true).Key with
        | ConsoleKey.D1 -> sendActivate client
        | ConsoleKey.D2 -> sendTrot client
        | ConsoleKey.D3 -> sendGait client
        | ConsoleKey.D4 -> sendShake client
        | ConsoleKey.D5 -> sendPushUp client
        //| ConsoleKey.D5 -> sendHop client
        | ConsoleKey.W -> sendForward client
        | ConsoleKey.S -> sendBackward client
        
        | ConsoleKey.I -> sendHeight client 1
        | ConsoleKey.K -> sendHeight client -1
        | ConsoleKey.J -> sendRoll client 1
        | ConsoleKey.L -> sendRoll client -1
        | ConsoleKey.U -> sendPitch client 1
        | ConsoleKey.O -> sendPitch client -1
        | ConsoleKey.A -> sendYaw client 1
        | ConsoleKey.D -> sendYaw client -1
        | _ -> sendNone client
    |> printfn "%A"
        
