$ErrorActionPreference = 'Continue'

$logPath = Join-Path $PSScriptRoot 'setup-unity-mcp-log.txt'
$script:lines = New-Object System.Collections.ArrayList

function Log([string]$m) {
    $t = "[{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $m
    [void]$script:lines.Add($t)
    Write-Host $t
    try {
        [System.IO.File]::WriteAllLines($logPath, $script:lines, (New-Object System.Text.UTF8Encoding($false)))
    } catch { }
}

Log "=== MCP for Unity server setup ==="
Log ("PSVersion : " + $PSVersionTable.PSVersion.ToString())
Log ("ScriptRoot: " + $PSScriptRoot)

# ---------------------------------------------------------------- 1. locate uv
$uvCandidates = @(
    (Join-Path $env:USERPROFILE '.local\bin\uv.exe'),
    (Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Links\uv.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\uv\uv.exe')
)

$uv = $null
$cmd = Get-Command uv -ErrorAction SilentlyContinue
if ($cmd) { $uv = $cmd.Source }
if (-not $uv) { foreach ($c in $uvCandidates) { if (Test-Path $c) { $uv = $c; break } } }

if ($uv) {
    Log ("uv already installed: " + $uv)
} else {
    Log "uv not found -> downloading installer from https://astral.sh/uv/install.ps1"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $installer = Invoke-RestMethod -Uri 'https://astral.sh/uv/install.ps1' -UseBasicParsing
        Invoke-Expression $installer
        Log "uv installer finished"
    } catch {
        Log ("uv install FAILED: " + $_.Exception.Message)
    }
    foreach ($c in $uvCandidates) { if (Test-Path $c) { $uv = $c; break } }
    if (-not $uv) {
        $cmd = Get-Command uv -ErrorAction SilentlyContinue
        if ($cmd) { $uv = $cmd.Source }
    }
}

if (-not $uv) {
    Log "!! uv still not found after install attempt. ABORTING."
    Log "=== DONE (failed) ==="
    exit 1
}

$uvDir = Split-Path -Parent $uv
$uvx = Join-Path $uvDir 'uvx.exe'
Log ("uv path  : " + $uv)
Log ("uv version: " + ((& $uv --version 2>&1) -join ' '))
Log ("uvx path : " + $uvx + "  exists=" + (Test-Path $uvx))

# --------------------------------------------------- 2. managed Python + server
Log "Installing a managed Python (uv python install 3.12) ..."
$out = & $uv python install 3.12 2>&1
Log ("  -> " + (($out | Out-String).Trim() -replace "`r`n", ' | '))

Log "Pre-fetching the MCP server package (uv tool install mcpforunityserver) ... this can take a minute"
$out = & $uv tool install "mcpforunityserver" --force 2>&1
Log ("  -> " + (($out | Out-String).Trim() -replace "`r`n", ' | '))

# ------------------------------------------- 3. merge Claude Desktop MCP config
$cfgDir = Join-Path $env:APPDATA 'Claude'
$cfgPath = Join-Path $cfgDir 'claude_desktop_config.json'
if (-not (Test-Path $cfgDir)) { New-Item -ItemType Directory -Path $cfgDir -Force | Out-Null }

$cfg = $null
if (Test-Path $cfgPath) {
    Copy-Item -LiteralPath $cfgPath -Destination ($cfgPath + '.bak') -Force
    Log ("existing config backed up -> " + $cfgPath + '.bak')
    $raw = Get-Content -LiteralPath $cfgPath -Raw
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        try { $cfg = $raw | ConvertFrom-Json } catch { Log ("existing config is not valid JSON, starting fresh: " + $_.Exception.Message) }
    }
}
if ($null -eq $cfg) { $cfg = New-Object psobject }

if (-not $cfg.PSObject.Properties['mcpServers']) {
    $cfg | Add-Member -NotePropertyName 'mcpServers' -NotePropertyValue (New-Object psobject)
}

$entry = [pscustomobject]@{
    command = $uvx
    args    = @('--from', 'mcpforunityserver', 'mcp-for-unity', '--transport', 'stdio')
}

if ($cfg.mcpServers.PSObject.Properties['unityMCP']) {
    $cfg.mcpServers.unityMCP = $entry
} else {
    $cfg.mcpServers | Add-Member -NotePropertyName 'unityMCP' -NotePropertyValue $entry
}

$json = $cfg | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($cfgPath, $json, (New-Object System.Text.UTF8Encoding($false)))
Log ("config written -> " + $cfgPath)
foreach ($l in ($json -split "`r?`n")) { Log ("  | " + $l) }

# --------------------------------------------------------------- 4. smoke test
Log "Smoke test: launching the server for 8 seconds to see if it starts ..."
try {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $uvx
    $psi.Arguments = '--from mcpforunityserver mcp-for-unity --transport stdio'
    $psi.RedirectStandardError = $true
    $psi.RedirectStandardOutput = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    Start-Sleep -Seconds 8
    if ($p.HasExited) {
        Log ("  server exited early, code=" + $p.ExitCode)
        Log ("  stderr: " + ($p.StandardError.ReadToEnd() -replace "`r`n", ' | '))
    } else {
        Log "  server is running (good) - stopping it"
        $p.Kill()
    }
} catch {
    Log ("  smoke test error: " + $_.Exception.Message)
}

Log "=== DONE ==="
Log "Next: quit Claude Desktop completely (tray icon -> Quit) and start it again."
