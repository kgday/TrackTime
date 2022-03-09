namespace TrackTime

open System
open System.IO
open System.Runtime.InteropServices
open System.Diagnostics

module SysUtils =
    let osOpen (spec: string) =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            //let start = sprintf "/c start \"%s\"" spec
            //Process.Start(ProcessStartInfo("cmd", start)) |> ignore
            let pi = ProcessStartInfo(spec);
            pi.UseShellExecute <- not (Path.GetExtension(spec).ToLower() = ".exe")
            Process.Start(pi) |> ignore
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            Process.Start("xdg-open", spec) |> ignore
        else if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            Process.Start("open", spec) |> ignore
