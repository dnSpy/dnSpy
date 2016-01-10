module FSharpUsingPatterns

open System
open System.IO

let sample1() = 
    use fs = File.Create("x.txt")
    fs.WriteByte(byte 1)

let sample2() = 
    Console.WriteLine("some text")
    use fs = File.Create("x.txt")
    fs.WriteByte(byte 2)
    Console.WriteLine("some text")

let sample3() = 
    Console.WriteLine("some text")
    do use fs = File.Create("x.txt")
       fs.WriteByte(byte 3)
    Console.WriteLine("some text")

let sample4() = 
    Console.WriteLine("some text")
    let firstByte = 
        use fs = File.OpenRead("x.txt")
        fs.ReadByte()
    Console.WriteLine("read:" + firstByte.ToString())

let sample5() =
    Console.WriteLine("some text")
    let firstByte = 
        use fs = File.OpenRead("x.txt")
        fs.ReadByte()
    let secondByte =
        use fs = File.OpenRead("x.txt")
        fs.ReadByte() |> ignore
        fs.ReadByte()
    Console.WriteLine("read: {0}, {1}", firstByte, secondByte)
