// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2014 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

/// This is the first entry point of the build system.
module WebSharper.Bootstrap.Main

open System
open System.Diagnostics
open System.Collections
open System.Collections.Specialized
open System.IO

/// Finds a command in path.
let FindCommand cmd =
    Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator)
    |> Seq.map (fun dir -> Path.Combine(dir, cmd))
    |> Seq.tryFind File.Exists

let private IsMono =
    Type.GetType("Mono.Runtime") <> null

/// Executes a command.
let Exec env (command: string) (arguments: string) =
    let psi = ProcessStartInfo()
    let filename =
        if File.Exists(command) then command else
            match FindCommand command with
            | Some command -> command
            | None -> failwithf "Unknown command: %s" command
    if IsMono then
        psi.FileName <- "mono"
        psi.Arguments <- sprintf "\"%s\" %s" filename arguments
    else
        psi.FileName <- filename
        psi.Arguments <- arguments
    psi.CreateNoWindow <- true
    psi.UseShellExecute <- false
    psi.RedirectStandardError <- true
    psi.RedirectStandardOutput <- true
    for (k, v) in env do
        if psi.EnvironmentVariables.ContainsKey(k) |> not then
            psi.EnvironmentVariables.Add(k, v)
    use proc = new Process()
    proc.StartInfo <- psi
    proc.EnableRaisingEvents <- true
    proc.ErrorDataReceived.Add(fun d -> if String.IsNullOrEmpty(d.Data) |> not then eprintfn "%s" d.Data)
    proc.OutputDataReceived.Add(fun d -> if String.IsNullOrEmpty(d.Data) |> not then printfn "%s" d.Data)
    let ok = proc.Start()
    proc.BeginErrorReadLine()
    proc.BeginOutputReadLine()
    proc.WaitForExit()
    if proc.ExitCode <> 0 then
        failwithf "ERROR in %s %s" command arguments

/// Restores NuGet packages.
let RestorePackages () =
    if Directory.Exists("packages/FSharp.Compiler.Service") |> not then
        let env = ["EnableNuGetPackageRestore", "true"]
        let nuget = Exec env "tools/NuGet/NuGet.exe"
        nuget "install NuGet.Core -version 2.12.0 -o packages -excludeVersion"
        nuget "install IntelliFactory.Core -pre -o packages -excludeVersion -nocache"
        nuget "install IntelliFactory.Build -pre -o packages -excludeVersion -nocache"
        nuget "install FSharp.Core -version 3.0.2 -o packages"
        nuget "install FSharp.Core -version 4.1.17 -o packages"
        nuget "install Mono.Cecil -pre -version 0.10.0-beta6 -o packages -excludeVersion"
        nuget "install AjaxMin -version 5.14.5506.26202 -o packages -excludeVersion"
        nuget "install FsNuGet -o packages -excludeVersion -nocache"
        nuget "install FSharp.Compiler.Service -version 13.0.0 -o packages -excludeVersion -nocache"
        nuget "install Microsoft.CodeAnalysis.CSharp -version 2.3.0 -o packages -excludeVersion"

[<EntryPoint>]
let Start args =
    RestorePackages ()
    0
