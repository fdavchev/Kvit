[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Stop,
    [switch]$NoBrowser
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$WebFolder = Join-Path $RepositoryRoot 'src\web'
$ApiPort = 5018
$WebPort = 5173
$ApiHealthUrl = "http://localhost:$ApiPort/health"
$WebUrl = "http://localhost:$WebPort"
$DatabaseContainer = 'kvit-postgres'
$DatabaseTimeoutSeconds = 60
$ApiTimeoutSeconds = 120
$WebTimeoutSeconds = 60
$ApiWindowTitle = 'Kvit API'
$WebWindowTitle = 'Kvit website'
$WindowsFile = Join-Path $env:TEMP 'kvit-local-windows.json'
$ApiExitFile = Join-Path $env:TEMP 'kvit-local-api.exit'
$WebExitFile = Join-Path $env:TEMP 'kvit-local-web.exit'
$ScriptCommand = '.\scripts\start-local.ps1'

function Exit-WithError {
    param([string]$Message)
    Write-Host ''
    Write-Host $Message -ForegroundColor Red
    exit 1
}

function Assert-ValidModes {
    if ($Check -and $Stop) {
        Exit-WithError "Use only one of -Check or -Stop. Without either, the script starts the app."
    }
    if ($NoBrowser -and ($Check -or $Stop)) {
        Exit-WithError "-NoBrowser only goes with starting the app (no -Check or -Stop), because only the start opens the browser."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot 'Kvit.slnx'))) {
        Exit-WithError "Kvit.slnx was not found in $RepositoryRoot. The script must stay in the scripts folder of the Kvit repository."
    }
}

function Assert-CommandAvailable {
    param([string]$Name)
    if ($null -eq (Get-Command -Name $Name -CommandType Application -ErrorAction SilentlyContinue)) {
        Exit-WithError "The program '$Name' was not found. Install it, then open a new terminal so it can be found, and run this again."
    }
}

function Invoke-NativeCommand {
    param([string]$FilePath, [string[]]$ArgumentList, [string]$WorkingDirectory = $RepositoryRoot)
    Assert-CommandAvailable $FilePath
    Push-Location -LiteralPath $WorkingDirectory
    try {
        $ErrorActionPreference = 'Continue'
        $lines = @(& $FilePath @ArgumentList 2>&1 | ForEach-Object { "$_" })
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
    }
    finally {
        Pop-Location
    }
    [pscustomobject]@{ ExitCode = $exitCode; Lines = $lines }
}

function Invoke-NativeCommandShown {
    param([string]$FilePath, [string[]]$ArgumentList, [string]$WorkingDirectory = $RepositoryRoot)
    Assert-CommandAvailable $FilePath
    Push-Location -LiteralPath $WorkingDirectory
    try {
        $ErrorActionPreference = 'Continue'
        $lines = @(& $FilePath @ArgumentList 2>&1 | ForEach-Object {
            $line = "$_" -replace "\x1b\[[0-9;?]*[A-Za-z]", ''
            Write-Host $line
            $line
        })
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
    }
    finally {
        Pop-Location
    }
    [pscustomobject]@{ ExitCode = $exitCode; Lines = $lines }
}

function Test-DockerRunning {
    $result = Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('info', '--format', '{{.ServerVersion}}')
    $result.ExitCode -eq 0
}

function Assert-DockerRunning {
    param([string]$Reason)
    if (-not (Test-DockerRunning)) {
        Exit-WithError "Docker Desktop is not running, and $Reason. Open Docker Desktop from the Start menu, wait until it says ""Engine running"", then run this again."
    }
}

function Get-ProcessName {
    param([int]$ProcessId)
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        return 'a process that has just ended'
    }
    $process.ProcessName
}

function Get-PortListeners {
    param([int[]]$Ports)
    $seen = @{}
    foreach ($connection in @(Get-NetTCPConnection -State Listen | Where-Object { $Ports -contains $_.LocalPort } | Sort-Object LocalPort)) {
        $key = "$($connection.LocalPort)/$($connection.OwningProcess)"
        if ($seen.ContainsKey($key)) {
            continue
        }
        $seen[$key] = $true
        [pscustomobject]@{
            Port = [int]$connection.LocalPort
            ProcessId = [int]$connection.OwningProcess
            ProcessName = Get-ProcessName ([int]$connection.OwningProcess)
        }
    }
}

function Assert-PortsFree {
    param([int[]]$Ports)
    $listeners = @(Get-PortListeners $Ports)
    if ($listeners.Count -eq 0) {
        return
    }
    $lines = @($listeners | ForEach-Object { "Port $($_.Port) is already in use by $($_.ProcessName) (process $($_.ProcessId))." })
    $lines += "Kvit (or another program) is still running from earlier. Run $ScriptCommand -Stop, or close that program's window, then run this again."
    $lines += "A running API also locks its own files on Windows, so nothing can be built while it runs."
    Exit-WithError ($lines -join "`n")
}

function Start-Database {
    Write-Host 'Starting the database (docker compose up -d)...'
    $result = Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('compose', 'up', '-d')
    if ($result.ExitCode -ne 0) {
        Exit-WithError ("'docker compose up -d' failed with exit code $($result.ExitCode):`n" + ($result.Lines -join "`n"))
    }
}

function Wait-DatabaseHealthy {
    Write-Host "Waiting for the database container $DatabaseContainer to be healthy..."
    $deadline = (Get-Date).AddSeconds($DatabaseTimeoutSeconds)
    $status = 'unknown'
    while ((Get-Date) -lt $deadline) {
        $result = Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('inspect', '--format', '{{.State.Health.Status}}', $DatabaseContainer)
        if ($result.ExitCode -ne 0) {
            Exit-WithError ("'docker inspect $DatabaseContainer' failed with exit code $($result.ExitCode):`n" + ($result.Lines -join "`n"))
        }
        $status = ($result.Lines -join '').Trim()
        if ($status -eq 'healthy') {
            return
        }
        Start-Sleep -Seconds 2
    }
    Exit-WithError "The database container $DatabaseContainer was not healthy after $DatabaseTimeoutSeconds seconds; its health status is '$status'. See why with: docker logs $DatabaseContainer"
}

function Install-WebPackagesIfMissing {
    if (Test-Path -LiteralPath (Join-Path $WebFolder 'node_modules')) {
        return
    }
    Write-Host 'Installing the website packages (npm install in src\web), only needed the first time...'
    $result = Invoke-NativeCommandShown -FilePath 'npm.cmd' -ArgumentList @('install') -WorkingDirectory $WebFolder
    if ($result.ExitCode -ne 0) {
        Exit-WithError "'npm install' in src\web failed with exit code $($result.ExitCode). Its output is above."
    }
}

function ConvertTo-QuotedLiteral {
    param([string]$Text)
    "'" + ($Text -replace "'", "''") + "'"
}

function Read-WindowRecords {
    if (-not (Test-Path -LiteralPath $WindowsFile)) {
        return @()
    }
    $records = Get-Content -LiteralPath $WindowsFile -Raw | ConvertFrom-Json
    @($records)
}

function Test-WindowRecordAlive {
    param($Record)
    $process = Get-Process -Id $Record.ProcessId -ErrorAction SilentlyContinue
    ($null -ne $process) -and ($process.StartTime.ToString('o') -eq $Record.StartTime)
}

function Save-WindowRecord {
    param([string]$Title, [System.Diagnostics.Process]$Process)
    $records = @(Read-WindowRecords | Where-Object { Test-WindowRecordAlive $_ })
    $records += [pscustomobject]@{ Title = $Title; ProcessId = $Process.Id; StartTime = $Process.StartTime.ToString('o') }
    ConvertTo-Json -InputObject $records | Set-Content -LiteralPath $WindowsFile -Encoding UTF8
}

function Start-ServerWindow {
    param([string]$Title, [string]$WorkingDirectory, [string[]]$ServerCommands, [string]$ExitFile)
    if (Test-Path -LiteralPath $ExitFile) {
        Remove-Item -LiteralPath $ExitFile
    }
    $windowScript = @(
        "`$Host.UI.RawUI.WindowTitle = $(ConvertTo-QuotedLiteral $Title)"
        "Set-Location -LiteralPath $(ConvertTo-QuotedLiteral $WorkingDirectory)"
    ) + $ServerCommands + @(
        "`$serverExitCode = `$LASTEXITCODE"
        "Set-Content -LiteralPath $(ConvertTo-QuotedLiteral $ExitFile) -Value `$serverExitCode"
        "Write-Host ''"
        "Write-Host ($(ConvertTo-QuotedLiteral "$Title stopped with exit code ") + `$serverExitCode + $(ConvertTo-QuotedLiteral ". Read the error above, then run $ScriptCommand -Stop to close this window.")) -ForegroundColor Red"
    )
    $encodedScript = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($windowScript -join "`n"))
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoExit', '-NoProfile', '-EncodedCommand', $encodedScript) -PassThru
    Save-WindowRecord -Title $Title -Process $process
    $process
}

function Wait-UrlAnswers {
    param([string]$Url, [int]$TimeoutSeconds, [System.Diagnostics.Process]$Window, [string]$Title, [string]$ExitFile)
    Write-Host "Waiting for $Url to answer (up to $TimeoutSeconds seconds)..."
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastResult = 'no answer yet'
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $ExitFile) {
            $exitCode = (Get-Content -LiteralPath $ExitFile -Raw).Trim()
            Exit-WithError "$Title stopped with exit code $exitCode before it answered at $Url. Read the error in the '$Title' window, then run $ScriptCommand -Stop and start again."
        }
        if ($Window.HasExited) {
            Exit-WithError "The '$Title' window was closed before $Url answered. Run $ScriptCommand -Stop, then start again."
        }
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                return
            }
            $lastResult = "status $($response.StatusCode)"
        }
        catch [System.Net.WebException] {
            $lastResult = $_.Exception.Message
        }
        Start-Sleep -Seconds 2
    }
    Exit-WithError "$Url did not answer within $TimeoutSeconds seconds (last result: $lastResult). Look for the error in the '$Title' window, then run $ScriptCommand -Stop."
}

function Start-Api {
    Write-Host "Starting the API in its own window ($ApiWindowTitle)..."
    $window = Start-ServerWindow -Title $ApiWindowTitle -WorkingDirectory $RepositoryRoot -ExitFile $ApiExitFile -ServerCommands @(
        "`$env:ASPNETCORE_ENVIRONMENT = 'Development'"
        'dotnet run --project src/api/Kvit.Api'
    )
    Wait-UrlAnswers -Url $ApiHealthUrl -TimeoutSeconds $ApiTimeoutSeconds -Window $window -Title $ApiWindowTitle -ExitFile $ApiExitFile
}

function Start-Website {
    Write-Host "Starting the website in its own window ($WebWindowTitle)..."
    $window = Start-ServerWindow -Title $WebWindowTitle -WorkingDirectory $WebFolder -ExitFile $WebExitFile -ServerCommands @(
        'npm.cmd run dev'
    )
    Wait-UrlAnswers -Url $WebUrl -TimeoutSeconds $WebTimeoutSeconds -Window $window -Title $WebWindowTitle -ExitFile $WebExitFile
}

function Open-Website {
    Start-Process -FilePath $WebUrl
}

function Write-StartSummary {
    Write-Host ''
    Write-Host 'Kvit is running.' -ForegroundColor Green
    Write-Host "  Website: $WebUrl"
    Write-Host "  API:     http://localhost:$ApiPort (health check: $ApiHealthUrl)"
    Write-Host "Stop everything with: $ScriptCommand -Stop"
}

function Start-LocalApp {
    Assert-DockerRunning 'the database runs in Docker'
    Assert-PortsFree @($ApiPort, $WebPort)
    Start-Database
    Wait-DatabaseHealthy
    Install-WebPackagesIfMissing
    Start-Api
    Start-Website
    if (-not $NoBrowser) {
        Open-Website
    }
    Write-StartSummary
}

function Test-ProcessRunning {
    param([int]$ProcessId)
    $null -ne (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)
}

function Stop-ProcessTree {
    param([int]$ProcessId, [string]$Description)
    $result = Invoke-NativeCommand -FilePath 'taskkill.exe' -ArgumentList @('/PID', "$ProcessId", '/T', '/F')
    if ($result.ExitCode -ne 0 -and (Test-ProcessRunning $ProcessId)) {
        Exit-WithError ("Could not stop $Description (process $ProcessId); taskkill exited with code $($result.ExitCode):`n" + ($result.Lines -join "`n") + "`nClose it by hand, then run $ScriptCommand -Stop again.")
    }
}

function Stop-ServerWindows {
    foreach ($record in @(Read-WindowRecords)) {
        if (-not (Test-WindowRecordAlive $record)) {
            continue
        }
        Stop-ProcessTree -ProcessId $record.ProcessId -Description "the '$($record.Title)' window"
        "Closed the '$($record.Title)' window and everything running in it (process $($record.ProcessId))."
    }
    if (Test-Path -LiteralPath $WindowsFile) {
        Remove-Item -LiteralPath $WindowsFile
    }
}

function Stop-PortListeners {
    foreach ($listener in @(Get-PortListeners @($ApiPort, $WebPort))) {
        if (-not (Test-ProcessRunning $listener.ProcessId)) {
            continue
        }
        Stop-ProcessTree -ProcessId $listener.ProcessId -Description "$($listener.ProcessName) on port $($listener.Port)"
        "Stopped $($listener.ProcessName) (process $($listener.ProcessId)), which was listening on port $($listener.Port)."
    }
}

function Wait-PortsFree {
    $deadline = (Get-Date).AddSeconds(10)
    while ((Get-Date) -lt $deadline) {
        $listeners = @(Get-PortListeners @($ApiPort, $WebPort))
        if ($listeners.Count -eq 0) {
            return
        }
        Start-Sleep -Milliseconds 500
    }
    $lines = @($listeners | ForEach-Object { "Port $($_.Port) is still in use by $($_.ProcessName) (process $($_.ProcessId)) 10 seconds after it was stopped." })
    $lines += "Close that program by hand (Task Manager, Details tab, find the process number), then run $ScriptCommand -Stop again."
    Exit-WithError ($lines -join "`n")
}

function Stop-Database {
    if (-not (Test-DockerRunning)) {
        return
    }
    $running = Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('compose', 'ps', '--status', 'running', '--quiet')
    if ($running.ExitCode -ne 0) {
        Exit-WithError ("'docker compose ps' failed with exit code $($running.ExitCode):`n" + ($running.Lines -join "`n"))
    }
    if (@($running.Lines | Where-Object { $_.Trim() -ne '' }).Count -eq 0) {
        return
    }
    $result = Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('compose', 'stop')
    if ($result.ExitCode -ne 0) {
        Exit-WithError ("'docker compose stop' failed with exit code $($result.ExitCode):`n" + ($result.Lines -join "`n"))
    }
    "Stopped the database container $DatabaseContainer (its data is kept)."
}

function Stop-LocalApp {
    $stopped = @()
    $stopped += @(Stop-ServerWindows)
    $stopped += @(Stop-PortListeners)
    Wait-PortsFree
    $stopped += @(Stop-Database)
    if ($stopped.Count -eq 0) {
        Write-Host "Nothing was running: no Kvit windows, nothing on ports $ApiPort and $WebPort, and the database was not running."
        return
    }
    $stopped | ForEach-Object { Write-Host $_ }
    Write-Host "Ports $ApiPort and $WebPort are free." -ForegroundColor Green
}

function Invoke-CheckCommand {
    param([string]$Title, [string]$FilePath, [string[]]$ArgumentList, [string]$WorkingDirectory)
    Write-Host ''
    Write-Host "==> $Title" -ForegroundColor Cyan
    $result = Invoke-NativeCommandShown -FilePath $FilePath -ArgumentList $ArgumentList -WorkingDirectory $WorkingDirectory
    if ($result.ExitCode -ne 0) {
        Exit-WithError "FAIL  $Title (exit code $($result.ExitCode)). Its output is above. The later checks were not run."
    }
    $result.Lines -join "`n"
}

function Read-LastNumber {
    param([string]$Output, [string]$Pattern, [string]$Title, [string]$What)
    $found = [regex]::Matches($Output, $Pattern)
    if ($found.Count -eq 0) {
        Exit-WithError "FAIL  $Title finished, but its output has no $What (looked for /$Pattern/), so the result cannot be confirmed. Its output is above."
    }
    [int]$found[$found.Count - 1].Groups[1].Value
}

function Invoke-BackendBuild {
    $title = 'dotnet build Kvit.slnx'
    $output = Invoke-CheckCommand -Title $title -FilePath 'dotnet' -ArgumentList @('build', 'Kvit.slnx') -WorkingDirectory $RepositoryRoot
    $warnings = Read-LastNumber -Output $output -Pattern '(\d+) Warning\(s\)' -Title $title -What 'warning count'
    $errors = Read-LastNumber -Output $output -Pattern '(\d+) Error\(s\)' -Title $title -What 'error count'
    if ($warnings -ne 0) {
        Exit-WithError "FAIL  $title finished with $warnings warnings; it must show 0. They are listed above. The later checks were not run."
    }
    "PASS  {0,-24} {1} warnings, {2} errors" -f $title, $warnings, $errors
}

function Invoke-BackendTests {
    $title = 'dotnet test Kvit.slnx'
    $output = Invoke-CheckCommand -Title $title -FilePath 'dotnet' -ArgumentList @('test', 'Kvit.slnx') -WorkingDirectory $RepositoryRoot
    $total = Read-LastNumber -Output $output -Pattern 'total:\s*(\d+)' -Title $title -What 'total test count'
    $failed = Read-LastNumber -Output $output -Pattern 'failed:\s*(\d+)' -Title $title -What 'failed test count'
    $succeeded = Read-LastNumber -Output $output -Pattern 'succeeded:\s*(\d+)' -Title $title -What 'succeeded test count'
    "PASS  {0,-24} {1} tests: {2} succeeded, {3} failed" -f $title, $total, $succeeded, $failed
}

function Invoke-WebLint {
    $title = 'npm run lint'
    $output = Invoke-CheckCommand -Title $title -FilePath 'npm.cmd' -ArgumentList @('run', 'lint', '--', '--format=default') -WorkingDirectory $WebFolder
    $warnings = Read-LastNumber -Output $output -Pattern 'Found (\d+) warnings?' -Title $title -What 'warning count'
    $errors = Read-LastNumber -Output $output -Pattern 'and (\d+) errors?' -Title $title -What 'error count'
    "PASS  {0,-24} {1} warnings, {2} errors" -f $title, $warnings, $errors
}

function Invoke-WebBuild {
    $title = 'npm run build'
    $output = Invoke-CheckCommand -Title $title -FilePath 'npm.cmd' -ArgumentList @('run', 'build') -WorkingDirectory $WebFolder
    $modules = Read-LastNumber -Output $output -Pattern '(\d+) modules transformed' -Title $title -What 'module count'
    "PASS  {0,-24} 0 type errors, {1} modules built" -f $title, $modules
}

function Invoke-WebTests {
    $title = 'npm test'
    $output = Invoke-CheckCommand -Title $title -FilePath 'npm.cmd' -ArgumentList @('test') -WorkingDirectory $WebFolder
    $files = Read-LastNumber -Output $output -Pattern 'Test Files\s+(\d+) passed' -Title $title -What 'test file count'
    $tests = Read-LastNumber -Output $output -Pattern 'Tests\s+(\d+) passed' -Title $title -What 'passed test count'
    "PASS  {0,-24} {1} tests passed in {2} files" -f $title, $tests, $files
}

function Invoke-Checks {
    Assert-DockerRunning 'the backend tests start their own test database in Docker'
    Assert-PortsFree @($ApiPort)
    Install-WebPackagesIfMissing
    $summary = @()
    $summary += Invoke-BackendBuild
    $summary += Invoke-BackendTests
    $summary += Invoke-WebLint
    $summary += Invoke-WebBuild
    $summary += Invoke-WebTests
    Write-Host ''
    $summary | ForEach-Object { Write-Host $_ -ForegroundColor Green }
}

Assert-ValidModes
if ($Stop) {
    Stop-LocalApp
}
elseif ($Check) {
    Invoke-Checks
}
else {
    Start-LocalApp
}
