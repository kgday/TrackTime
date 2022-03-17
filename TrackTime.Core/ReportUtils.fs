namespace TrackTime
module ReportUtils =
    open System
    let private tempReportsDir =
        let d = IO.Path.GetTempPath()
        IO.Path.Combine(d, "TrackTimeReports")

    let private reportImageCacheDir =
        IO.Path.Combine( tempReportsDir, "PageImageCache");

    let private tempRptPagesImagesDir () =
        let d = DateTime.Now.ToString("yyyyMMddHHmmssfff")
        IO.Path.Combine(reportImageCacheDir, d)

    let reportTempOutputFileName id =
        let ts = DateTime.Now.ToString("yyyyMMddHHmmssfff")
        let f = sprintf "%s%s.fpx" id ts
        IO.Path.Combine(tempReportsDir, ts)

    let private createDir dir =
        IO.Directory.CreateDirectory(dir) |> ignore 
            
    let cleanTempReportsDir () =
        let dir = tempReportsDir
        try
            if IO.Directory.Exists dir then
                IO.Directory.Delete(dir,true)
        with
        | e -> () //ignore it
        createDir dir

         
    let createRptPagesImagesDir () =
        let dir = tempRptPagesImagesDir ()
        createDir dir
        dir
     
        
