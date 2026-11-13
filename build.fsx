// Скрипт для сборки и публикации .NET проектов с поддержкой нескольких платформ

// Использование:
//dotnet fsi build.fsx [путь-к-проекту] [путь-для-артефактов] [runtime-identifiers]

// Примеры:
//# Сборка проекта PCRadio с настройками по умолчанию
//dotnet fsi build.fsx

//# С указанием пути к проекту
//dotnet fsi build.fsx ./MyProject/MyProject.csproj

//# С указанием пути для артефактов
//dotnet fsi build.fsx ./MyProject/MyProject.csproj ./dist

//# С указанием платформ
//dotnet fsi build.fsx ./MyProject/MyProject.csproj ./artifacts "win-x64,linux-x64,osx-x64"

#r "System.Formats.Tar"

open System
open System.Formats.Tar
open System.IO
open System.IO.Compression
open System.Xml.Linq

// Параметры скрипта
let projectPath =
    fsi.CommandLineArgs
    |> Array.tryItem 1
    |> Option.defaultValue "./src/PCRadio.csproj"

let outputPathParam =
    fsi.CommandLineArgs |> Array.tryItem 2 |> Option.defaultValue "./artifacts"

let runtimeIdentifiersParam =
    match fsi.CommandLineArgs |> Array.tryItem 3 with
    | Some rids -> rids.Split(',') |> Array.toList
    | None -> [ "win-x64"; "linux-x64" ]

// Проверка существования файла проекта
if not (File.Exists projectPath) then
    eprintfn $"Файл проекта не найден: {projectPath}"
    exit 1

// Конфигурация проекта
let projectName = Path.GetFileNameWithoutExtension projectPath
let publishDir = Path.Combine(outputPathParam, "publish")

// Цветной вывод в консоль
let writeColor color message =
    let originalColor = Console.ForegroundColor
    Console.ForegroundColor <- color
    printfn $"{message}"
    Console.ForegroundColor <- originalColor

let writeInfo = writeColor ConsoleColor.Cyan
let writeSuccess = writeColor ConsoleColor.Green
let writeWarning = writeColor ConsoleColor.Yellow
let writeError = writeColor ConsoleColor.Red

// 1. Очистка решения
writeInfo "Очистка решения..."

let currentDir = Directory.GetCurrentDirectory()

let rec deleteDirectories pattern =
    Directory.GetDirectories(currentDir, pattern, SearchOption.AllDirectories)
    |> Array.iter (fun dir ->
        try
            Directory.Delete(dir, true)
        with ex ->
            writeWarning $"Не удалось удалить {dir}: {ex.Message}")

[ "bin"; "obj" ] |> List.iter deleteDirectories

if Directory.Exists outputPathParam then
    Directory.Delete(outputPathParam, true)

// Создание директорий
Directory.CreateDirectory outputPathParam |> ignore
Directory.CreateDirectory publishDir |> ignore

// 2. Получение версии приложения
writeInfo "\nПолучение версии приложения..."

let getVersionFromProject (projectPath: string) =
    try
        let doc = XDocument.Load projectPath

        let versionNode =
            doc.Descendants() |> Seq.tryFind (fun n -> n.Name.LocalName = "Version")

        match versionNode with
        | Some node ->
            let version = node.Value.Trim()
            writeSuccess $"Версия найдена в файле проекта: {version}"
            version
        | None ->
            writeWarning "Версия не найдена в файле проекта, используется версия по умолчанию: 1.0.0"
            "1.0.0"
    with ex ->
        writeWarning $"Ошибка при чтении версии: {ex.Message}. Используется версия по умолчанию: 1.0.0"
        "1.0.0"

let version = getVersionFromProject projectPath

// 3. Сборка и публикация для каждой платформы
let publishProject (rid: string) (isSelfContained: bool) =
    let sc =
        if isSelfContained then
            "self-contained"
        else
            "framework-dependent"

    let platformPublishDir = Path.Combine(publishDir, rid)
    let archiveExtension =
        if rid.StartsWith("linux", StringComparison.OrdinalIgnoreCase) then
            ".tar.gz"
        else
            ".zip"

    let archiveName =
        if isSelfContained then
            $"{projectName}-self-contained-{rid}-{version}{archiveExtension}"
        else
            $"{projectName}-{rid}-{version}{archiveExtension}"

    let archivePath = Path.Combine(outputPathParam, archiveName)

    let args =
        [ "publish"
          projectPath
          "-c"
          "Release"
          "-r"
          rid
          "-o"
          platformPublishDir
          if isSelfContained then
              "--self-contained"
          else
              "--no-self-contained"
          "/p:DebugType=None"
          "/p:DebugSymbols=false" ]

    writeInfo $"Публикация для {sc} {rid}..."

    let psi = System.Diagnostics.ProcessStartInfo()
    psi.FileName <- "dotnet"
    psi.Arguments <- String.concat " " args
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true

    use proc = System.Diagnostics.Process.Start psi
    proc.WaitForExit()

    if proc.ExitCode <> 0 then
        let error = proc.StandardError.ReadToEnd()
        writeError $"Ошибка публикации: {error}"
        false
    else
        // Создание архива
        writeInfo $"Создание архива для {sc} {rid}..."

        if Directory.Exists platformPublishDir then
            if archiveExtension = ".tar.gz" then
                use archive = File.Create archivePath
                use gzip = new GZipStream(archive, CompressionLevel.Optimal)
                TarFile.CreateFromDirectory(platformPublishDir, gzip, false)
            else
                ZipFile.CreateFromDirectory(platformPublishDir, archivePath, CompressionLevel.Optimal, false)

            writeSuccess $"Архив создан: {archivePath}"

        // Очистка временной папки
        if Directory.Exists platformPublishDir then
            writeInfo "Очистка временной папки..."
            Directory.Delete(platformPublishDir, true)

        true

writeInfo "\nНачало публикации..."

let results =
    runtimeIdentifiersParam
    |> List.collect (fun rid ->
        [ rid, false // framework-dependent
          rid, true ] // self-contained
    )
    |> List.map (fun (rid, isSelfContained) ->
        let success = publishProject rid isSelfContained

        if not success then
            writeError $"Ошибка при публикации {rid} (Self-contained: {isSelfContained})"

        success)

// 4. Финальный отчет
if results |> List.forall id then
    // Очистка bin и obj после успешной сборки
    [ "bin"; "obj" ] |> List.iter deleteDirectories
    writeSuccess "\nСборка успешно завершена!"
    writeSuccess $"Артефакты находятся в: {Path.GetFullPath outputPathParam}"
else
    let failedCount = results |> List.filter not |> List.length
    writeError $"\nСборка завершена с ошибками. Неудачных публикаций: {failedCount}"
    exit 1
