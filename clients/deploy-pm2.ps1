<#
.SYNOPSIS
    Deploys this Next.js (standalone output) app to an IIS + PM2-fronted Windows server.

.DESCRIPTION
    1. Installs npm dependencies (npm install) - run every time, unconditionally, so a
       corrupted/incomplete prior install (e.g. a broken native binary) self-heals rather
       than silently persisting across deploys.
    2. Builds the app (npm run build).
    3. Copies .next/static and public/ into the standalone output so it can serve
       static assets on its own (per Next.js standalone-mode requirements).
    4. Stops the IIS app pool, IIS site, and pm2 process (10s wait after each stop;
       pm2 stop failures are tolerated, e.g. on a first deploy with no process yet).
    5. Wipes ONLY the "standalone" subfolder inside the IIS service folder
       (ServiceFolderPath\standalone\), preserving any .env* files already there
       no matter how deeply nested. Everything else directly under the IIS
       service folder - web.config, and anything else IIS-level - is never
       touched.
    6. Copies the freshly-built standalone output into that same "standalone"
       subfolder (ServiceFolderPath\standalone\), which is where the app
       actually runs from.
    7. Stamps RUNTIME_APP_VERSION=<timestamp> into the .env* file(s) left inside
       ServiceFolderPath\standalone\ (searched recursively) - BEFORE pm2 starts
       the process below, since Next.js only reads .env into process.env once
       at boot, not per-request.
    8. Starts (or creates, with --update-env) the pm2 process directly from the
       freshly deployed ServiceFolderPath\standalone\server.js, named/identified
       by Pm2Id, so it boots against the version just stamped above.
    9. Restarts the IIS app pool and IIS site (10s wait after each start; pm2 was
       already started in step 8).

    This script only WRITES files and orchestrates the above steps; running it requires
    the IIS "WebAdministration" PowerShell module (i.e. it must run on the IIS host, or a
    machine with IIS management tools installed) and pm2 available on PATH.

.PARAMETER RepoPath
    Path to this repo's checkout on disk (where package.json / next.config.ts live).
    Defaults to the folder this script itself lives in. The script does NOT change its
    own working directory to this path - npm install/build (Step 1/2) run with this as
    their working directory via Push-Location, everything else is addressed by full
    path. Example: "D:\GitHub\Minh\next-js-with-claude"

.PARAMETER SiteName
    IIS site name (as shown in IIS Manager). Example: "MyApp"

.PARAMETER AppPoolName
    IIS application pool name. Example: "MyAppPool"

.PARAMETER ServiceFolderPath
    The deployed/live folder on the server that IIS serves from (NOT this repo's own
    build output). The actual app runs from a "standalone" subfolder inside this path
    (ServiceFolderPath\standalone\server.js) - not the folder root itself. Only that
    "standalone" subfolder is ever wiped/rewritten by this script; web.config and
    anything else directly under ServiceFolderPath is left untouched.
    Example: "D:\IISApps\MyApp"

.PARAMETER Pm2Id
    The pm2 process id (numeric, e.g. "0") or process name (e.g. "my-app"). Used both
    to stop the existing process (step 4) and as the --name given to `pm2 start
    ...\standalone\server.js` when (re)creating it (step 8).

.EXAMPLE
    .\deploy.ps1

    Runs with the default config values below.

.EXAMPLE
    .\deploy.ps1 -RepoPath "D:\GitHub\Minh\next-js-with-claude" -SiteName "MyApp" -AppPoolName "MyAppPool" -ServiceFolderPath "D:\IISApps\MyApp" -Pm2Id "my-app"

    Runs with explicit overrides for every config value.
#>

[CmdletBinding()]
param(
    # Path to this repo's checkout (contains package.json / next.config.ts).
    # Defaults to the folder this script lives in.
    [string]$RepoPath = $PSScriptRoot,

    # IIS site name, e.g. "MyApp"
    [string]$SiteName = "NextJsApp",

    # IIS application pool name, e.g. "MyAppPool"
    [string]$AppPoolName = "NextJsApp",

    # Deployed/live IIS service folder on the server (distinct from this repo's build output)
    [string]$ServiceFolderPath = "C:\IT\IIS_Services\UAT\NextJs2026",

    # pm2 process id (numeric) or name, e.g. "0" or "my-app"
    [string]$Pm2Id = "0"
)


Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# WebAdministration provides Stop-WebAppPool / Start-WebAppPool / Stop-Website / Start-Website.
# It ships with IIS Management Tools and is typically only available on the IIS host itself.
try {
    Import-Module WebAdministration

    if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
        # Forward every parameter the caller explicitly passed (e.g. -RepoPath,
        # which no longer defaults to this script's own folder now that it can
        # live in a different folder than the repo it deploys) so the elevated
        # relaunch doesn't silently fall back to parameter defaults and lose them.
        $elevatedArgList = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"")
        foreach ($paramName in $PSBoundParameters.Keys) {
            $elevatedArgList += "-$paramName"
            $elevatedArgList += "`"$($PSBoundParameters[$paramName])`""
        }
        $elevatedArgs = $elevatedArgList -join ' '
        Start-Process powershell.exe $elevatedArgs -Verb RunAs
        exit
    }
}
catch {
    Write-Error "The 'WebAdministration' PowerShell module could not be loaded. This script must be run on a host with IIS Management Tools installed. Original error: $($_.Exception.Message)"
    exit 1
}

function Wait-Seconds10 {
    Write-Host "Waiting 10s..."
    Start-Sleep -Seconds 10
}

function Wait-Seconds30 {
    Write-Host "Waiting 30s..."
    Start-Sleep -Seconds 30
}

function Remove-ItemWithRetry {
    # A stopped IIS app pool/site or a just-stopped pm2 process doesn't
    # always release its file handles instantly - IIS app pool shutdown in
    # particular can be gracefully asynchronous. Retrying with backoff is
    # more robust than any single fixed wait, since the actual release time
    # is unpredictable.
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [int]$MaxAttempts = 6,

        [int]$DelaySeconds = 5
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            if ($attempt -eq $MaxAttempts) {
                throw "Could not delete '$Path' after $MaxAttempts attempts - it may still be locked by the pm2/IIS process. Original error: $($_.Exception.Message)"
            }
            Write-Host "    '$Path' is still locked (attempt $attempt/$MaxAttempts) - retrying in ${DelaySeconds}s..."
            Start-Sleep -Seconds $DelaySeconds
        }
    }
}

function Clear-DirectoryExceptEnv {
    # Recursively deletes everything under $Path except files matching
    # .env* - wherever they're nested (e.g. a legacy .env sitting inside a
    # "standalone" subfolder), not just at the top level. A name-only
    # top-level filter would delete a whole subfolder (and any .env* file
    # nested inside it) as long as the subfolder itself isn't named
    # ".env*". A folder that still contains a preserved .env* file (directly
    # or in a deeper subfolder) is kept; a folder left empty after removing
    # its non-.env* content is deleted too, so no stray empty folders pile
    # up across deploys.
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $children = Get-ChildItem -LiteralPath $Path -Force
    foreach ($child in $children) {
        if ($child.PSIsContainer) {
            Clear-DirectoryExceptEnv -Path $child.FullName
            $remaining = @(Get-ChildItem -LiteralPath $child.FullName -Force)
            if ($remaining.Count -eq 0) {
                Write-Host "    Removing $($child.FullName)"
                Remove-ItemWithRetry -Path $child.FullName
            }
            else {
                Write-Host "    Keeping $($child.FullName) (still contains preserved .env* file(s))"
            }
        }
        elseif ($child.Name -notlike '.env*') {
            Write-Host "    Removing $($child.FullName)"
            Remove-ItemWithRetry -Path $child.FullName
        }
        else {
            Write-Host "    Keeping $($child.FullName)"
        }
    }
}

function Invoke-NativeCommand {
    # Windows PowerShell 5.1 wraps every line a native process writes to
    # stderr in a terminating ErrorRecord when $ErrorActionPreference is
    # 'Stop' - true even with no explicit 2>&1 redirection. A harmless
    # warning on stderr (e.g. Next.js's own "falling back to wasm" notice)
    # would otherwise abort this script before $LASTEXITCODE is ever
    # checked. Run natives under 'Continue' and rely on the exit code
    # instead, which is the signal that actually matters.
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$ArgumentList,

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $FilePath @ArgumentList
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage (exit code $LASTEXITCODE)"
    }
}

function Invoke-Robocopy {
    # Copies $Source into $Destination preserving directory symlinks/junctions
    # as links (via /SL) rather than following/expanding them. Needed because
    # the Next.js standalone build's node_modules is a pnpm store: top-level
    # packages (next, react, react-dom, ...) are directory symlinks pointing
    # back into the repo's own node_modules/.pnpm - and Copy-Item -Recurse on
    # Windows PowerShell 5.1 silently turns those into empty real directories
    # instead of copying the link, which is what actually broke `next start`
    # with "Cannot find module 'next'" (confirmed root cause). Robocopy /SL
    # preserves the link itself; since RepoPath and ServiceFolderPath are
    # both on this same host, the absolute symlink targets stay resolvable.
    param(
        [Parameter(Mandatory)]
        [string]$Source,

        [Parameter(Mandatory)]
        [string]$Destination,

        [string[]]$ExcludeFiles = @(),

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    $roboArgs = @($Source, $Destination, "/E", "/SL", "/NFL", "/NDL", "/NJH", "/NJS", "/NP")
    if ($ExcludeFiles.Count -gt 0) {
        $roboArgs += "/XF"
        $roboArgs += $ExcludeFiles
    }

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & robocopy @roboArgs
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    # Robocopy exit codes 0-7 are success (bitmask of files copied/skipped/
    # extra); 8+ means at least one failure actually occurred.
    if ($LASTEXITCODE -ge 8) {
        throw "$FailureMessage (robocopy exit code $LASTEXITCODE)"
    }
}

function Test-Pm2ProcessOnline {
    # Returns $true/$false, or $null if status couldn't be determined (e.g.
    # pm2 not reachable, unexpected output) - callers should attempt the
    # stop/start anyway when $null, rather than assume either state.
    param(
        [Parameter(Mandatory)]
        [string]$Id
    )

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $json = & pm2 jlist
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "    Could not query pm2 status (exit code $LASTEXITCODE)."
        return $null
    }

    try {
        $processes = $json | ConvertFrom-Json
    }
    catch {
        Write-Host "    Could not parse pm2 status output."
        return $null
    }

    $proc = $processes | Where-Object { "$($_.pm_id)" -eq $Id -or $_.name -eq $Id } | Select-Object -First 1
    if (-not $proc) {
        return $false
    }
    return ($proc.pm2_env.status -eq 'online')
}

try {
    # -------------------------------------------------------------------
    # Resolve the repo path - this script's OWN working directory is left
    # alone (no global Set-Location) so it can be invoked from anywhere,
    # e.g. after being moved/copied to a folder separate from the repo it
    # deploys. Every path derived from $RepoRoot below is built with
    # Join-Path/-LiteralPath, and npm (Step 1/2) is scoped to $RepoRoot via
    # its own Push-Location/Pop-Location pair, so nothing here depends on
    # the process's current directory.
    # -------------------------------------------------------------------
    if ([string]::IsNullOrWhiteSpace($RepoPath)) {
        throw "RepoPath is empty. Pass -RepoPath, or run this script from within the repo so `$PSScriptRoot resolves it."
    }
    if (-not (Test-Path -LiteralPath $RepoPath -PathType Container)) {
        throw "RepoPath does not exist or is not a folder: $RepoPath"
    }

    $RepoRoot = (Resolve-Path -LiteralPath $RepoPath).Path
    Write-Host "==> Using repo path: $RepoRoot"
    $NextDir = Join-Path $RepoRoot ".next"
    $StandaloneDir = Join-Path $RepoRoot ".next\standalone"
    $StaticSourceDir = Join-Path $RepoRoot ".next\static"
    $StaticDestDir = Join-Path $StandaloneDir ".next\static"
    $PublicSourceDir = Join-Path $RepoRoot "public"
    $PublicDestDir = Join-Path $StandaloneDir "public"
    # The app actually runs from a "standalone" subfolder inside the live
    # IIS service folder (not the service folder root) - server.js and the
    # deployed .env live at $ServiceFolderPath\standalone\.
    $ServiceStandaloneDir = Join-Path $ServiceFolderPath "standalone"

    if (-not (Test-Path (Join-Path $RepoRoot "package.json"))) {
        throw "No package.json found under RepoPath ($RepoRoot) is this the right folder?"
    }

    # -------------------------------------------------------------------
    # Step 1: Install npm dependencies
    # -------------------------------------------------------------------
    Write-Host "==> [1/10] Installing npm dependencies (npm install)..."
    Push-Location $RepoRoot
    try {
        Invoke-NativeCommand -FilePath "npm" -ArgumentList @("install") -FailureMessage "npm install failed"
    }
    finally {
        Pop-Location
    }

    # -------------------------------------------------------------------
    # Step 2: Build the Next.js app
    # -------------------------------------------------------------------
    # Clear .next first. A stale .next - especially leftover .next/dev/*
    # generated-type artifacts from a prior `next dev` run, possibly mixed
    # with a different installed Next.js version than produced them - can
    # make `next build`'s typed-routes type-check fail on corrupted
    # generated content that isn't a real code error (confirmed root cause
    # of a prior failure). Building from a pristine .next avoids that class
    # of failure recurring on this host.
    if (Test-Path -LiteralPath $NextDir) {
        Write-Host "==> [2/10] Clearing stale .next build cache..."
        Get-ChildItem -LiteralPath $NextDir -Force | ForEach-Object {
            Remove-ItemWithRetry -Path $_.FullName
        }
    }

    Write-Host "==> [2/10] Building Next.js app (npm run build)..."
    Push-Location $RepoRoot
    try {
        Invoke-NativeCommand -FilePath "npm" -ArgumentList @("run", "build") -FailureMessage "npm run build failed"
    }
    finally {
        Pop-Location
    }

    # -------------------------------------------------------------------
    # Step 3: Copy .next/static into the standalone output
    # -------------------------------------------------------------------
    Write-Host "==> [3/10] Copying .next/static into standalone output..."
    if (-not (Test-Path $StaticSourceDir)) {
        throw "Expected build output not found: $StaticSourceDir"
    }
    New-Item -ItemType Directory -Force -Path $StaticDestDir | Out-Null
    Copy-Item -Path (Join-Path $StaticSourceDir '*') -Destination $StaticDestDir -Recurse -Force

    # -------------------------------------------------------------------
    # Step 4: Copy public/ into the standalone output
    # -------------------------------------------------------------------
    Write-Host "==> [4/10] Copying public/ into standalone output..."
    if (Test-Path $PublicSourceDir) {
        New-Item -ItemType Directory -Force -Path $PublicDestDir | Out-Null
        Copy-Item -Path (Join-Path $PublicSourceDir '*') -Destination $PublicDestDir -Recurse -Force
    }
    else {
        Write-Host "    (no public/ folder found, skipping)"
    }

    # -------------------------------------------------------------------
    # Step 5: Stop services (skip any that are already stopped)
    # -------------------------------------------------------------------
    Write-Host "==> [5/10] Stopping IIS app pool '$AppPoolName'..."
    $poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
    if ($poolState -eq 'Stopped') {
        Write-Host "    Already stopped, skipping."
    }
    else {
        Stop-WebAppPool -Name $AppPoolName
        Wait-Seconds10
    }

    Write-Host "==> [5/10] Stopping IIS site '$SiteName'..."
    $siteState = (Get-WebsiteState -Name $SiteName).Value
    if ($siteState -eq 'Stopped') {
        Write-Host "    Already stopped, skipping."
    }
    else {
        Stop-Website -Name $SiteName
        Wait-Seconds10
    }

    Write-Host "==> [5/10] Stopping pm2 process '$Pm2Id'..."
    $pm2Online = Test-Pm2ProcessOnline -Id $Pm2Id
    if ($pm2Online -eq $false) {
        Write-Host "    Already stopped or not registered, skipping."
    }
    else {
        try {
            Invoke-NativeCommand -FilePath "pm2" -ArgumentList @("stop", $Pm2Id) -FailureMessage "pm2 stop $Pm2Id failed"
        }
        catch {
            Write-Host "    pm2 stop failed (process may not exist yet) - continuing anyway: $($_.Exception.Message)"
        }
        # Longer wait than the other stops - gives the Node process time to
        # fully release its file handles on the deployed folder before the
        # wipe step below deletes it, avoiding "file in use" delete errors.
        Wait-Seconds30
    }

    # -------------------------------------------------------------------
    # Step 6: Wipe the standalone subfolder contents (preserving .env* files)
    # -------------------------------------------------------------------
    # Scoped to ServiceFolderPath\standalone only - everything else directly
    # under ServiceFolderPath (e.g. web.config, IIS's own site config) must
    # never be touched by this deploy.
    Write-Host "==> [6/10] Clearing standalone subfolder (preserving .env* files): $ServiceStandaloneDir"

    if ([string]::IsNullOrWhiteSpace($ServiceFolderPath) -or ($ServiceFolderPath -match '^[A-Za-z]:\\?$')) {
        throw "Refusing to proceed: ServiceFolderPath ('$ServiceFolderPath') is empty, null, or looks like a drive root. Aborting to avoid a destructive delete against an unintended path."
    }

    if (-not (Test-Path $ServiceFolderPath)) {
        throw "ServiceFolderPath does not exist: $ServiceFolderPath"
    }

    if (Test-Path -LiteralPath $ServiceStandaloneDir) {
        Clear-DirectoryExceptEnv -Path $ServiceStandaloneDir
    }
    else {
        Write-Host "    $ServiceStandaloneDir does not exist yet (first deploy?), nothing to clear."
    }

    # -------------------------------------------------------------------
    # Step 7: Copy standalone build output into the IIS service folder
    # -------------------------------------------------------------------
    # Next.js's standalone build automatically bundles a root-level .env /
    # .env.production into .next/standalone if either exists in the repo
    # (see next/dist/build/index.js's writeStandaloneDirectory). Excluding
    # .env* here too (on top of the wipe step above) guarantees the deployed
    # server's own preserved .env* files are never overwritten by this copy,
    # even if the repo grows one of those files in the future.
    Write-Host "==> [7/10] Copying standalone build output into IIS service folder..."
    New-Item -ItemType Directory -Force -Path $ServiceStandaloneDir | Out-Null
    Invoke-Robocopy -Source $StandaloneDir -Destination $ServiceStandaloneDir -ExcludeFiles @('.env*') -FailureMessage "Copying standalone build output failed"

    # -------------------------------------------------------------------
    # Step 8: Stamp RUNTIME_APP_VERSION into the .env* file(s) left in place
    # -------------------------------------------------------------------
    # Must happen BEFORE pm2 starts the process below: Next.js's own env
    # loading (next/env) reads .env files into process.env once at server
    # boot, not per-request - starting the process first would have it boot
    # against the OLD timestamp, so the "new" version would never actually
    # show until the following deploy.
    Write-Host "==> [8/10] Updating RUNTIME_APP_VERSION in .env* files..."
    $timestamp = Get-Date -Format "yyyyMMdd-HHmm"
    $envFiles = @(Get-ChildItem -LiteralPath $ServiceStandaloneDir -Recurse -Force -Filter '.env*' -File)

    if ($envFiles.Count -eq 0) {
        Write-Host "    No .env* files found under $ServiceStandaloneDir, nothing to stamp."
    }

    foreach ($envFile in $envFiles) {
        Write-Host "    Stamping $($envFile.FullName) -> RUNTIME_APP_VERSION=$timestamp"
        $lines = Get-Content -LiteralPath $envFile.FullName -Encoding UTF8
        $found = $false
        $newLines = $lines | ForEach-Object {
            if ($_ -match '^RUNTIME_APP_VERSION=.*$') {
                $found = $true
                $_ -replace '^RUNTIME_APP_VERSION=.*$', "RUNTIME_APP_VERSION=$timestamp"
            }
            else {
                $_
            }
        }
        if (-not $found) {
            $newLines = $newLines + "RUNTIME_APP_VERSION=$timestamp"
        }
        Set-Content -LiteralPath $envFile.FullName -Value $newLines -Encoding UTF8
    }

    # -------------------------------------------------------------------
    # Step 9: Start (or create) the pm2 process from the freshly deployed server.js
    # -------------------------------------------------------------------
    $serverJsPath = Join-Path $ServiceStandaloneDir "server.js"
    Write-Host "==> [9/10] Starting pm2 process '$Pm2Id' from $serverJsPath..."
    if (-not (Test-Path -LiteralPath $serverJsPath)) {
        throw "Expected server.js not found at $serverJsPath after copy - check the build output."
    }
    # --update-env: force pm2 to refresh the process's environment (rather
    # than reuse anything it may have cached from a prior run of this name)
    # so the just-stamped .env is what the new process actually boots with.
    Invoke-NativeCommand -FilePath "pm2" -ArgumentList @("start", $serverJsPath, "--name", $Pm2Id, "--update-env") -FailureMessage "pm2 start $serverJsPath --name $Pm2Id failed"
    Wait-Seconds10

    # -------------------------------------------------------------------
    # Step 10: Start remaining services (pm2 was already started in step 9)
    # -------------------------------------------------------------------
    Write-Host "==> [10/10] Starting IIS app pool '$AppPoolName'..."
    Start-WebAppPool -Name $AppPoolName
    Wait-Seconds10

    Write-Host "==> [10/10] Starting IIS site '$SiteName'..."
    Start-Website -Name $SiteName
    Wait-Seconds10

    Write-Host "==> Deploy complete."
}
catch {
    Write-Error "Deploy failed: $($_.Exception.Message)"
    exit 1
}
