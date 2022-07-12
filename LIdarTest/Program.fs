module LidarTest.Program

open System
open System.IO.Ports
open System.Threading
open LidarTest.Lidar
open Plotly.NET


printfn "Starting Lidar Server"
let port = "/dev/tty.usbserial-0001"

let connectToSerial () =
    Lidar.connect port
    
let plotData (d: LidarPoint list) =
    let xData = List.map (fun i -> i.x) d
    let yData = List.map (fun i -> i.y) d
    let myFirstChart = Chart.Point(xData,yData)
    myFirstChart |> Chart.show
    

    
let mainLoop (sp:SerialPort) =
    let mutable pointsToPlot: Lidar.LidarPoint list = []
    while true do
        let d = sp.ReadByte ()
        // an example line: 54 2C 11 0E 05 26 BF 01 E0 C3 01 DF C5 01 DF CD 01 E2 D1 01 E2 DA 01 E1 DF 01 DE E1 01 DD E6 01 DC D7 01 DD CF 01 DA CB 01 DB 6F 29 CD 5C 21
        // starts with 0x54 or decimal 84.
        // read 47 items for every 0x54 we see.
        // this only works b/c we are limited such that the number of readings is always 12.


        // TODO: read each list
        // for each group of 40 lists, convert to XY's and plot them?
        if d = 84 then
            let data =[|
                "54"
                for i=1 to 46 do
                    (sp.ReadByte ()).ToString("X2")
            |]
            printfn "%A" data
            printfn "%i" data.Length
            let points = Lidar.parse data
            pointsToPlot <- List.append pointsToPlot (Array.toList points)
            
        if List.length pointsToPlot > 500 then
            printfn "Plotting"
            plotData pointsToPlot
            pointsToPlot <- []
    

while true do
    try
        use sp = connectToSerial ()
        mainLoop sp
    with
        | e -> printfn "%A" e
    printfn "App Stopped, waiting a second and starting again"
    Thread.Sleep (1000)