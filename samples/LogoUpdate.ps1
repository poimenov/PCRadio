$ErrorActionPreference = "Stop"

# Путь к базе данных рядом со скриптом
$dbPath = Join-Path $PSScriptRoot "PCRadio.db"

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

# Подключаем модуль
Import-Module PSSQLite

# Выполняем SQL-запрос
$sql = @"
UPDATE Stations
SET Logo = replace(Logo, 'pcradio.ru', 'pcradio.app')
WHERE Logo LIKE '%pcradio.ru%';
"@

Invoke-SqliteQuery -DataSource $dbPath -Query $sql

Write-Host "Обновление базы данных завершено." -ForegroundColor Green