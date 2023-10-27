#r "nuget: OBO.NET, 0.4.1"
#r "nuget: FSharp.Data, 6.3.0"
#r "System.Xml.Linq.dll"

open OBO.NET
open FSharp.Data

// Related to: https://github.com/nfdi4plants/nfdi4plants_ontology/issues/72

/// Urls and file paths
module Literals =

    let private source = __SOURCE_DIRECTORY__

    let GetSpeciesUrl = "https://www.ncbi.nlm.nih.gov/pmc/advanced"
    let RequestResultsFilePath = System.IO.Path.Combine(source, "./SpeciesResults.txt")
    let NCBITaxonOBOUrl = @"http://purl.obolibrary.org/obo/ncbitaxon.obo"
    let NCBITaxonOBOFilePath = System.IO.Path.Combine(source, "./ncbitaxon.obo")
    let NCBITaxonMinOBOFilePath = System.IO.Path.Combine(source, @"./ncbitaxonmin.obo")

module GetSpecies =

    open FSharp.Data.JsonExtensions

    type Species = {
        Position: int
        Name: string
        CountUsed: int
    } with
        member this.ToFormattedString() = $"{this.Position}\t{this.Name}\t{this.CountUsed}"

    type TermsReply = XmlProvider<"""<termsreply>
        <TermList total="891020" ActuallyInReply="200" start="0">
            <Term count="1">'abrahamia littoralis'</Term><Term count="6">'abrahamia'</Term>
        </TermList>
    </termsreply>""">

    // https://www.ncbi.nlm.nih.gov/pmc/advanced
    let request(position : int) = 
        let cmd = "Down" //if position = 1 then "Index" else "Down"
        let body = @"p%24site=pmc&p%24rq=EntrezSystem2.PEntrez.PMC.Pmc_AdvancedSearch.Pmc_Index.Index%3AXmlHttpHandler&Db=pmc&Term=&Field=Organism%20unsynonymized&Position=" + string position + @"&Cmd=" + cmd + "&IndexPath=EntrezSystem2.PEntrez.PMC.Pmc_AdvancedSearch.Pmc_Index&DevDbModeFlag=false"
        let requestResult = 
            Http.RequestString(
                Literals.GetSpeciesUrl,
                httpMethod = "POST",
                body = HttpRequestBody.TextRequest body,
                headers = [ 
                    "accept", "*/*"
                    "accept-language", "en-GB,en-US;q=0.9,en;q=0.8"
                    "content-type", "application/x-www-form-urlencoded"
                    "ncbi-phid", "CE8A2788538CC9110000000002CE0255.m_7.011"
                    "sec-fetch-dest", "empty"
                    "sec-fetch-mode", "cors"
                    "sec-fetch-site", "same-origin"
                    "cookie", @"ncbi_sid=2FD33B71538C5A53_5791SID; pmc-frontend-csrftoken=U6iqGOdoPHyiLKMzMcuFWj2tr1XQkhXaYpegiIWntho8Ts01jUJv04TxXgKWU2iA; _gid=GA1.2.1934089493.1698224343; gdh-data-hub-csrftoken=5dG5gQUrCuKPYObJ460V0bzp0OGUvzZRAH9K9e8zSQqDanc6QglojEXKYV8IGBru; WebEnv=167xWPhZ6h9za2j08o1ojEg2_L9pTqOzWG6oM0vkBezQ1%402FD33B71538C5A53_5791SID; _gat_ncbiSg=1; _gat_dap=1; _ga=GA1.1.377947535.1698224343; _ga_DP2X732JSX=GS1.1.1698232997.2.0.1698233014.0.0.0; ncbi_pinger=N4IgDgTgpgbg+mAFgSwCYgFwgMIFEAcAnACwBMAzMQKzn4Ai2xA7AIwAMHnnxhAgm8Q4A6ALZwmIADQgAxgBtkMgNYA7KAA8ALplBtMIZCrABXTXADuEAIZhJAZwBGcBWslXUMO1BmTDqDQgA9hCaclCabh5wXlYQMohwMoEqmlApkkkpaRGZmlaGUBBSICz6AJIq/uoAylCx8ZIi+Sq52cWk+tnQAF7F5PpgIjLFxPruMFYtUOjSVPrFLKNYYFYA5lBwMMhQ5gsSWABmVnJeC/j6RydQ7XpYeERklDT0jKxc7zz8gmyi4u2lWC6UG6GEGMgw40mMmmGAAcgB5WG4dodLDmdFCFoOZCYuQiTHIRBCVaBGDtQj6FgANlIemk5FuJTY+DpIHIAJKNNZ5FRTJ5fQpWBYTFI7BGjJY+HwTFZxH6WB+5EIQn60mIS1kCmUai0IzmCpGVIux1Oav2ICpbA6avOWEoEjVgpAdkQgXMcD8GmKVEZiE0mjAdgwAHpg+jzJiZNjcfiVITiaTg2Dg5CpqgAMTejne3nVV3mAAEnvUBYUdm0s3lFrl3o1hFIowAvo2gA"
                    "Referer", "https://www.ncbi.nlm.nih.gov/pmc/advanced"
                    "Referrer-Policy", "origin-when-cross-origin"
                ]
            )
            |> JsonValue.Parse
            |> fun json -> json?XmlHttpData
            |> fun json -> json.AsString() 
        let initXml = TermsReply.Parse(requestResult)
        {|
            Total = initXml.TermList.Total
            Next = initXml.TermList.Start
            Terms = initXml.TermList.Terms |> Array.mapi (fun i t -> {Position = position + i ;Name = t.Value; CountUsed = t.Count}) |> List.ofArray
        |}

    let TimeStallMilliseconds = 2000

    let requestAll(start: int option) =
        let start = defaultArg start 1
        let time (total: float) =
            let nCalls = total/200.
            let secondsPerCall = float TimeStallMilliseconds / 1000.
            let minutes = (secondsPerCall*nCalls)/60.
            let hours = minutes/60. 
            printfn "Expected runtime with 200 elements per request and total %i elements: %f minutes (%f hours)" (int total) minutes hours
            ()
        let write (content:string list) = 
            System.IO.File.AppendAllLines(Literals.RequestResultsFilePath, content)
        let rec loop (current:int) =
            let requestResult = request(current)
            let termLines = requestResult.Terms |> List.map (fun t -> t.ToFormattedString())
            write termLines
            if current = 1 then time (float requestResult.Total)
            printfn "Finish %i" requestResult.Next
            if requestResult.Next >= (requestResult.Total-1) then 
            // if requestResult.Next >= 1000 then 
                printfn "Done!"
            else
                System.Threading.Thread.Sleep(TimeStallMilliseconds)
                loop requestResult.Next
        loop start
    

module ResultParsing =
    /// Prepare request organism information for comparison with ncbitaxon.
    /// 1. Update names to follow ontology term case pattern (first letter capital, all others lower case)
    /// 2. only keep unique names
    /// 3. Sort descending by count times used in research papers.
    let filteredSpecies (speciesResults: string []) =
        speciesResults
        |> Array.map (fun line -> line.Split([|"\t"|], System.StringSplitOptions.RemoveEmptyEntries))
        |> Array.filter (fun lineArr -> lineArr <> [||])
        |> Array.map (fun lineArr -> 
            try
                int lineArr.[2], lineArr.[1].[0].ToString().ToUpper() + lineArr.[1].[1..]
            with
                | :? System.IndexOutOfRangeException as exn -> failwithf "%A" ("IndexOutOfRange: ", lineArr)
                | exn -> failwithf "%A" (exn.Message, lineArr)
        )
        |> Array.distinctBy snd
        |> Array.sortByDescending fst

    /// Filter terms requests by:
    /// 1. Must be findable in ncbitaxon
    /// 2. Must have PropertyValue = "has_rank NCBITaxon:species"
    /// 3. Keep only the top ``numberOfAnnotated`` terms.
    /// 4. Replace existing is_a relationships with one is_a towards "OBI:0100026 ! organism"
    let annotateSpeciesTerms(numberOfAnnotated:int) (ncbitaxon: OboOntology) (filteredSpecies: (int * string) []) = 
        let isSpecies (l: list<string>) = l |> List.contains "has_rank NCBITaxon:species"
        let termCount = filteredSpecies.Length
        let rec loop (index: int) (results: OboTerm list) =
            if results.Length >= numberOfAnnotated || index >= (termCount-1) then
                results 
            else
                if results.Length % 50 = 0 then printfn "%A" (results.Length, numberOfAnnotated) 
                let _,speciesName = filteredSpecies.[index]
                let at = ncbitaxon.TryGetTermByName(speciesName)
                let next = loop (index + 1)  
                match at with
                | Some t when isSpecies t.PropertyValues -> 
                    next (t::results)
                | _ -> 
                    next results 
        let terms =
            loop 0 []
            |> List.rev
            |> List.map (fun x -> {x with IsA = ["OBI:0100026 ! organism"]})
        terms
        |> Array.ofList

/// 1. -- START -- Prepare NCBITaxon obo ontology -- //
let downloadNCBITaxon() = 
    let c = Http.RequestString(Literals.NCBITaxonOBOUrl)
    System.IO.File.WriteAllText(Literals.NCBITaxonOBOFilePath, c)
// downloadNCBITaxon() // This can take some time, ncbitaxon.obo is ~500mb

let ncbitaxon = OboOntology.fromFile false Literals.NCBITaxonOBOFilePath

/// 2. -- Crawl and download organism information from ``Literals.GetSpeciesUrl`` ---- //
/// 
/// This will start downloading all species, will print expected time required.
let getSpecies() =
    let start = None
    GetSpecies.requestAll(start) 

// getSpecies()

/// 3. -- READ the file produced by ``GetSpecies.requestAll(start)`` -- //
let read = System.IO.File.ReadAllLines(Literals.RequestResultsFilePath)

/// 4. -- Assign ncbitaxon terms to organism results -- //
let annotatedSpeciesTerms =
    ResultParsing.filteredSpecies read
    |> ResultParsing.annotateSpeciesTerms 20000 ncbitaxon

// annotatedSpeciesTerms |> Array.map (fun x -> x.Name)

/// 5. -- WRITE ncbitaxonmin.obo -- //
let write() =
    let ncbitaxonMin = OboOntology.create(List.ofArray annotatedSpeciesTerms) []
    let lines = ncbitaxonMin.ToLines() |> List.ofSeq
    let metadata = 
        [
            "format-version", "1.2"
            "data-version", "1.0.0"
            "ontology", "ncbitaxonmin"
            "date", System.DateTime.UtcNow.ToString("dd:MM:yyyy HH:mm")
            "saved-by", "Kevin Frey"
            "auto-generated-by", "https://github.com/nfdi4plants/nfdi4plants_ontology/blob/main/ncbitaxonmin.fsx"
            "import", Literals.NCBITaxonOBOUrl
            "remark", "This file was created to offer a subset of ncbitaxon, featuring the most popular species in research papers with a focus on plants and microorganisms."
        ]   
        |> List.map (fun (k,v) -> sprintf "%s: %s" k v)
    let content = metadata@[""]@lines
    let p = Literals.NCBITaxonMinOBOFilePath
    System.IO.File.WriteAllLines(p, content)

write()