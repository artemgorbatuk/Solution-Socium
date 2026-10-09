# Starts WebApi (dotnet watch) and Angular (ng serve) for local development
# in separate console windows. Skips a side that is already listening on its port.
#
# Run from repo root:
#   powershell -ExecutionPolicy Bypass -File scripts/local/start-solution.ps1

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$webApiDir = Join-Path $repoRoot 'src\backend\SociumApp\WebApi'
$frontendDir = Join-Path $repoRoot 'src\frontend\SociumWeb'
$backendPort = 7026
$frontendPort = 4200

function Test-PortListening([int] $Port) {
    return [bool](Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function Start-Console([string] $Title, [string] $WorkingDir, [string] $Command) {
    $script = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location '$WorkingDir'; $Command"
    $proc = Start-Process -FilePath 'powershell.exe' -PassThru -ArgumentList @(
        '-NoExit',
        '-ExecutionPolicy', 'Bypass',
        '-Command', $script
    )
    Write-Host ("{0} window PID {1}" -f $Title, $proc.Id)
}

foreach ($dir in @($webApiDir, $frontendDir)) {
    if (-not (Test-Path $dir)) {
        throw "Project not found: $dir"
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet not found. Install .NET SDK first.'
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'npm not found. Install Node.js first.'
}

if (Test-PortListening $backendPort) {
    Write-Host ("Backend already running on port {0} - skip." -f $backendPort)
} else {
    Start-Console 'Socium WebApi' $webApiDir 'dotnet watch run --launch-profile https'
}

if (Test-PortListening $frontendPort) {
    Write-Host ("Frontend already running on port {0} - skip." -f $frontendPort)
} else {
    # Explicit IPv4: "localhost" may resolve only to ::1, which is unreachable under VPN.
    Start-Console 'Socium Angular' $frontendDir "if (-not (Test-Path node_modules)) { npm install }; npm start -- --host 127.0.0.1 --port $frontendPort"
}

Write-Host 'Done.'
Write-Host ("  WebApi:   https://localhost:{0}" -f $backendPort)
Write-Host ("  Angular:  http://127.0.0.1:{0}" -f $frontendPort)
