namespace Dockerfile.Tugboat

open System
open System.IO
open Dockerfile.Tugboat.Domain
open Dockerfile.Tugboat.String

[<AutoOpen>]
module DSL =
    let render df = 
        let rec renderInstruction instr =
            match instr with
            | Simple s -> print s.Name s.Value

            | SimpleQuoted s -> print s.Name (quote s.Value)

            | List l -> print l.Name (printList l.Elements)

            | KeyValue kv -> print kv.Name (printKV kv.Key kv.Value)

            | KeyValueList l ->
                str {
                    l.Name
                    " "
                    l.Elements 
                    |> Seq.map (fun e -> 
                        let (key, value) = e
                        printKV key value)
                    |> String.concat eol_slash
                }

            | From f ->
                str {
                    sprintf "%sFROM" eol
                    printParameterQ "platform" f.Platform
                    sprintf " %s" f.Image
                    if f.Name.IsSome then 
                        sprintf " AS %s" f.Name.Value
                }

            | Run r -> 
                r.Commands
                |> String.concat ", "
                |> sprintf "RUN %s"

            | Add a -> 
                str {
                    "ADD"
                    printFlag "link" a.Link
                    if a.KeepGitDir then
                        sprintf " --keep-git-dir=true"
                    printParameter "chmod" a.Chmod
                    printParameter "chown" a.Chown
                    printParameter "checksum" a.Checksum
                    " "
                    printList a.Elements
                }

            | Copy cp -> 
                str {
                    "COPY"
                    printFlag "link" cp.Link
                    printParameter "from" cp.From
                    printParameter "chmod" cp.Chmod
                    printParameter "chown" cp.Chown
                    " "
                    printList cp.Elements
                }

            | Healthcheck hc -> 
                str {
                    "HEALTCHECK"
                    printParameter "interval" hc.Interval
                    printParameter "timeout" hc.Timeout
                    printParameter "start-period" hc.StartPeriod
                    printParameter "retries" hc.Retries
                    sprintf "%sCMD %s" eol_slash (printList hc.Instructions)
                }

            | Onbuild onb -> 
                seq { onb.Instruction }
                |> r
                |> sprintf "ONBUILD %s"

        and r sub =
            sub
            |> Seq.map (fun instr ->
                match instr with
                | Plain t -> t
                | Instruction i -> renderInstruction i
                | Subpart s -> r s |> trim
            )
            |> String.concat eol
        
        r df
        |> trim

    let toFile path text =
        use stream = FileInfo(path).Create()
        stream.Write text