$ErrorActionPreference = "Stop"

# Путь к базе данных рядом со скриптом
$dbPath = Join-Path $PSScriptRoot "PCRadio.db"
$outputPath = Join-Path $PSScriptRoot "PCRadio.m3u"

if (-not (Test-Path $dbPath)) {
    throw "База данных не найдена: $dbPath"
}

# Проверяем наличие модуля PSSQLite
if (-not (Get-Module -ListAvailable -Name PSSQLite)) {
    Write-Host "Модуль PSSQLite не найден. Устанавливаем..."

    # Проверяем наличие провайдера NuGet
    if (-not (Get-PackageProvider -Name NuGet -ListAvailable -ErrorAction SilentlyContinue)) {
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
    }

    # Устанавливаем модуль для текущего пользователя
    Install-Module -Name PSSQLite -Scope CurrentUser -Force -AllowClobber
}

Import-Module PSSQLite

$stations = Invoke-SqliteQuery -DataSource $dbPath -Query "SELECT Name, Stream FROM Stations ORDER BY Name;"
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("#EXTM3U")

foreach ($station in $stations) {
    $name = ([string]$station.Name -replace "[\r\n]+", " ").Trim()
    $stream = ([string]$station.Stream -replace "[\r\n]+", "").Trim()
    $lines.Add("#EXTINF:-1,$name")
    $lines.Add($stream)
}

[System.IO.File]::WriteAllLines($outputPath, $lines, [System.Text.UTF8Encoding]::new($false))

Write-Host "Экспортировано станций: $($stations.Count)"
Write-Host "Файл сохранен: $outputPath" -ForegroundColor Green
