module LidarTest.Lidar

open System
open System.IO.Ports

type LidarPoint = {
    distance: float
    angle: float
    x: float
    y: float
    confidence: float
}
let connect (p: string) =
    let sp = new SerialPort (p, 230400, Parity.None, 8, StopBits.One)
    sp.ReadTimeout <- 5000
    //sp.WriteTimeout <- 1500
    sp.Open ()
    sp.DtrEnable <- true
    sp.RtsEnable <- true
    //sp.NewLine <- "\r"
    sp
    
    
let polarToCart d a =
    let x = d * -Math.Cos (a)
    let y = d * Math.Sin (a)
    (x,y)
    

let getValuePairFromMsb(d: string array) (msb: int): float =
    let fs = d.[msb] + d.[msb-1]
    let i = Convert.ToUInt32(fs, 16)
    Convert.ToDouble(i)
    
let getValueAt (d: string array) (a: int): float =
    Convert.ToDouble (Convert.ToUInt32 ( d.[a], 16))
    
let getStartAngle (d: string array): float =
    // get position 5 and 4 (5 is MSB)
    // divide by 100 to get degrees
    (getValuePairFromMsb d 5)  / 100.0
    
let getEndAngle (d: string array): float =
    // get position 43 and 42
    // divide by 100 to get degrees
    (getValuePairFromMsb d 43) / 100.0
    
let parse (d: string array) =
    let s = getStartAngle d
    let e = getEndAngle d
    // get the delta for each reading by getting the difference between the
    // start and end and dividing by 11 (12 readings)
    let diff = if e > s then (e - s) / 11.0 else ((e + 360.0) - s) / 11.0
    // read each set of three bytes (three bytes per reading)
    // in the form [Distance LSB] [Distance MSB] [Confidence (only one byte)]
    // this starts at position 6 (that will be the first LSB for the first distance)
    // so our for loop should go 6 .. 9 .. 12 to read 12 items
    let data = [|
        for i in 6 .. 3 .. 41 do
            // the distance will be read from MSB at i + 1
            let dist = (getValuePairFromMsb d <| i + 1) / 100.0 // divide by 100.0 to get the dist in meters
            let conf = getValueAt d (i + 2)
            let angle = (float s) + (float (i - 6)) / 3.0 * diff
            let angle' = if angle >= 360.0 then angle - 360.0 else angle
            let angle'' = angle' *Math.PI / 180.0
            let x, y = polarToCart dist angle''
            { distance = dist; angle = angle''; x = x; y = y; confidence = conf }
        |]
    data

