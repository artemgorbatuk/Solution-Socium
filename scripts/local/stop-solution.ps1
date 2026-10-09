# Stops local WebApi and Angular (ports 7026, 4200).
# For the port owner, its dotnet.exe ancestors ("dotnet watch" chain) are stopped too,
# otherwise "dotnet watch" restarts WebApi on the next file change.
#
# Run from repo root:
#   powershell -ExecutionPolicy Bypass -File scripts/local/stop-solution.ps1

$ErrorActionPreference = 'Continue'

$ports = @(7026, 4200)

function Get-ProcessChain([int] $ProcId) {
    $chain = @()
    $proc = Get-CimInstance Win32_Process -Filter "ProcessId = $ProcId" -ErrorAction SilentlyContinue
    if (-not $proc) {
        return $chain
    }
    $chain += $proc
    while ($true) {
        $parent = Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.ParentProcessId)" -ErrorAction SilentlyContinue
        if (-not $parent -or $parent.Name -ne 'dotnet.exe') {
            break
        }
        $chain += $parent
        $proc = $parent
    }
    return $chain
}

foreach ($port in $ports) {
    $connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if (-not $connections) {
        Write-Host ("Port {0} - nothing listening." -f $port)
        continue
    }

    $procIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($procId in $procIds) {
        $chain = Get-ProcessChain $procId
        [array]::Reverse($chain)
        foreach ($proc in $chain) {
            Write-Host ("Port {0} - stopping PID {1} ({2})..." -f $port, $proc.ProcessId, $proc.Name)
            Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        }
    }
}

Start-Sleep -Seconds 1

$left = @($ports | Where-Object { Get-NetTCPConnection -LocalPort $_ -State Listen -ErrorAction SilentlyContinue })

if ($left.Count -eq 0) {
    Write-Host ("Done. Ports {0} are free." -f ($ports -join ', '))
} else {
    Write-Host ("Still in use: {0}. Close the console windows manually if needed." -f ($left -join ', '))
}
