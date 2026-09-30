<#
 ***: Must run pnpm install --frozen-lockfile before running this script, so the standalone build can succeed.
    If you haven't done that yet, run `pnpm install --frozen-lockfile` in the repo root first.
.SYNOPSIS
    Deploys this Next.js (standalone output) app to an IIS + NSSM-Windows-Service-fronted server.

.DESCRIPTION
    1. Installs pnpm dependencies (pnpm install) - run every time, unconditionally, so a
       corrupted/incomplete prior install (e.g. a broken native binary) self-heals rather
       than silently persisting across deploys.
    2. Builds the app (pnpm run build). First hoists NEXT_SERVER_ACTIONS_ENCRYPTION_KEY
       out of the preserved ServiceFolderPath\standalone\.env* into the build
       environment (unless already set) so Server Action IDs stay stable across
       deploys - without it, Next.js generates a fresh key every build (the .next
       key cache is wiped here too) and open browser tabs on the previous build
       break with "Failed to find Server Action". A missing key only warns.
    3. Copies .next/static and public/ into the standalone output so it can serve
       static assets on its own (per Next.js standalone-mode requirements).
    4. Stops the IIS app pool, IIS site, and the NSSM Windows service (10s wait after
       each stop; a "net stop" failure is tolerated, e.g. on a first deploy with no
       service registered yet).
    5. Wipes ONLY the "standalone" subfolder inside the IIS service folder
       (ServiceFolderPath\standalone\), preserving any .env* files already there
       no matter how deeply nested. Everything else directly under the IIS
       service folder - web.config, and anything else IIS-level - is never
       touched.
    6. Copies the freshly-built standalone output into that same "standalone"
       subfolder (ServiceFolderPath\standalone\), which is where the app
       actually runs from.
    7. Stamps RUNTIME_APP_VERSION=<timestamp> into the .env* file(s) left inside
       ServiceFolderPath\standalone\ (searched recursively) - BEFORE the Windows
       service starts the process below, since Next.js only reads .env into
       process.env once at boot, not per-request.
    8. Starts the NSSM Windows service (net start), which launches the freshly
       deployed ServiceFolderPath\standalone\server.js, so it boots against the
       version just stamped above.
    9. Restarts the IIS app pool and IIS site (10s wait after each start; the
       Windows service was already started in step 8).

    This script only WRITES files and orchestrates the above steps; running it requires
    the IIS "WebAdministration" PowerShell module (i.e. it must run on the IIS host, or a
    machine with IIS management tools installed), and the Windows service named
    NodeServiceName must already be registered via NSSM (e.g. `nssm install <name>
    node.exe ...\standalone\server.js`, done once, outside this script) - this script
    only starts/stops it, it never creates or reconfigures the service itself.

.PARAMETER RepoPath
    Path to this repo's checkout on disk (where package.json / next.config.ts live).
    Defaults to the folder this script itself lives in. The script does NOT change its
    own working directory to this path - pnpm install/build (Step 1/2) run with this as
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

.PARAMETER NodeServiceName
    The Windows service name (as registered via NSSM) that runs server.js. Used both
    to stop the existing service (step 4) and to start it again (step 8), via plain
    `net stop` / `net start` - this script does not install or configure the service.
    Example: "NextJsApp"

.EXAMPLE
    .\deploy-nssm.ps1

    Runs with the default config values below.

.EXAMPLE
    .\deploy-nssm.ps1 -RepoPath "D:\GitHub\Minh\next-js-with-claude" -SiteName "MyApp" -AppPoolName "MyAppPool" -ServiceFolderPath "D:\IISApps\MyApp" -NodeServiceName "MyAppService"

    Runs with explicit overrides for every config value.
#>

[CmdletBinding()]
param(
    # Path to this repo's checkout (contains package.json / next.config.ts).
    # Defaults to the folder this script lives in.
    [string]$RepoPath = "C:\IT\Source\GitHub\modular-monolith\clients\admin",

    # IIS site name, e.g. "MyApp"
    [string]$SiteName = "NextJsApp",

    # IIS application pool name, e.g. "MyAppPool"
    [string]$AppPoolName = "NextJsApp",

    # Deployed/live IIS service folder on the server (distinct from this repo's build output)
    [string]$ServiceFolderPath = "C:\IT\IIS_Services\UAT\NextJs2026",

    # Windows service name (registered via NSSM) that runs server.js
    [string]$NodeServiceName = "NextJs-UAT"
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
    # A stopped IIS app pool/site or a just-stopped Windows service doesn't
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
                throw "Could not delete '$Path' after $MaxAttempts attempts - it may still be locked by the Windows service/IIS process. Original error: $($_.Exception.Message)"
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

        $isReparsePoint =
            ($child.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0

        if ($isReparsePoint) {
            Write-Host "    Removing link $($child.FullName)"
            Remove-ItemWithRetry -Path $child.FullName
        }
        elseif ($child.PSIsContainer) {
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
        & $FilePath @ArgumentList 2>&1 | ForEach-Object {
            Write-Host $_
        }
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

function Get-ServerActionsEncryptionKey {
    # Next.js salts Server Action IDs with (and encrypts action closure
    # variables using) an AES key. If NEXT_SERVER_ACTIONS_ENCRYPTION_KEY is not
    # set at BUILD time, Next.js generates a fresh random key per build - and
    # since Step 2 wipes .next, the 14-day on-disk key cache never survives
    # either. Every deploy would then rotate all action IDs, breaking any
    # browser tab still running the previous build with "Failed to find Server
    # Action" until it reloads.
    #
    # The key is stored once in the deployed standalone\.env (preserved across
    # deploys by Step 6) and hoisted into the build environment so it stays
    # constant. Returns $null when no .env* / key line is found (e.g. first
    # deploy) - the caller warns and continues.
    param(
        [Parameter(Mandatory)]
        [string]$StandaloneDir
    )

    if (-not (Test-Path -LiteralPath $StandaloneDir)) {
        return $null
    }

    $envFiles = @(Get-ChildItem -LiteralPath $StandaloneDir -Recurse -Force -Filter '.env*' -File)
    foreach ($envFile in $envFiles) {
        $match = Select-String -LiteralPath $envFile.FullName `
            -Pattern '^\s*NEXT_SERVER_ACTIONS_ENCRYPTION_KEY\s*=\s*(\S.*?)\s*$' -List
        if ($match) {
            return $match.Matches[0].Groups[1].Value
        }
    }
    return $null
}

function Test-WindowsServiceRunning {
    # Returns $true/$false. A service that can't be found (not yet
    # registered via NSSM, e.g. on a first deploy) is treated as "not
    # running" rather than an error - callers should tolerate that.
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $service) {
        return $false
    }
    return ($service.Status -eq 'Running')
}

try {
    # -------------------------------------------------------------------
    # Resolve the repo path - this script's OWN working directory is left
    # alone (no global Set-Location) so it can be invoked from anywhere,
    # e.g. after being moved/copied to a folder separate from the repo it
    # deploys. Every path derived from $RepoRoot below is built with
    # Join-Path/-LiteralPath, and pnpm (Step 1/2) is scoped to $RepoRoot via
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
    # Step 1: Install pnpm dependencies
    # -------------------------------------------------------------------
    Write-Host "==> [1/10] Installing pnpm dependencies..."

    Push-Location $RepoRoot
    try {
        Invoke-NativeCommand `
            -FilePath "pnpm" `
            -ArgumentList @("install", "--prod=false", "--frozen-lockfile") `
            -FailureMessage "pnpm install failed"

        $nextCommandPath = Join-Path $RepoRoot "node_modules\.bin\next.cmd"

        if (-not (Test-Path -LiteralPath $nextCommandPath)) {
            Write-Warning "next.cmd not found, forcing reinstall..."

            Invoke-NativeCommand `
                -FilePath "pnpm" `
                -ArgumentList @("install", "--prod=false", "--frozen-lockfile", "--force") `
                -FailureMessage "pnpm forced install failed"
        }

        if (-not (Test-Path -LiteralPath $nextCommandPath)) {
            throw "Next.js executable not found: $nextCommandPath"
        }
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

    # Pin the Server Actions encryption key BEFORE the build so action IDs stay
    # stable across deploys (see Get-ServerActionsEncryptionKey for why). `next
    # build` reads .env files via next/env, which never overrides a value
    # already present in the environment - so setting $env:* here wins.
    Write-Host "==> [2/10] Resolving NEXT_SERVER_ACTIONS_ENCRYPTION_KEY for the build..."
    if (-not [string]::IsNullOrWhiteSpace($env:NEXT_SERVER_ACTIONS_ENCRYPTION_KEY)) {
        Write-Host "    Already set in this environment - using it as-is."
    }
    else {
        $actionsKey = Get-ServerActionsEncryptionKey -StandaloneDir $ServiceStandaloneDir
        if ([string]::IsNullOrWhiteSpace($actionsKey)) {
            Write-Warning ("NEXT_SERVER_ACTIONS_ENCRYPTION_KEY not found under $ServiceStandaloneDir (.env*) " +
                "and not set in the environment. This build gets a random key, so Server Action IDs will " +
                "differ from the previous deploy and open tabs will hit 'Failed to find Server Action' until " +
                "reloaded. Add the key to the deployed standalone\.env to make it stable.")
        }
        else {
            $env:NEXT_SERVER_ACTIONS_ENCRYPTION_KEY = $actionsKey
            Write-Host "    Pinned from the deployed standalone\.env (value hidden)."
        }
    }

    Write-Host "==> [2/10] Building Next.js app (pnpm run build)..."
    Push-Location $RepoRoot
    try {
        Invoke-NativeCommand -FilePath "pnpm" -ArgumentList @("run", "build") -FailureMessage "pnpm run build failed"
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

    Write-Host "==> [5/10] Stopping Windows service '$NodeServiceName'..."
    $serviceRunning = Test-WindowsServiceRunning -Name $NodeServiceName
    if ($serviceRunning -eq $false) {
        Write-Host "    Already stopped or not registered, skipping."
    }
    else {
        try {
            Invoke-NativeCommand -FilePath "net" -ArgumentList @("stop", $NodeServiceName) -FailureMessage "net stop $NodeServiceName failed"
        }
        catch {
            Write-Host "    net stop failed (service may not exist yet) - continuing anyway: $($_.Exception.Message)"
        }
        # Longer wait than the other stops - gives the Node process time to
        # fully release its file handles on the deployed folder before the
        # wipe step below deletes it, avoiding "file in use" delete errors.
        Wait-Seconds10
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
    # Must happen BEFORE the Windows service starts the process below:
    # Next.js's own env loading (next/env) reads .env files into process.env
    # once at server boot, not per-request - starting the process first
    # would have it boot against the OLD timestamp, so the "new" version
    # would never actually show until the following deploy.
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
    # Step 9: Start the Windows service running the freshly deployed server.js
    # -------------------------------------------------------------------
    # This script only starts an already-registered NSSM service - it does
    # not install/reconfigure it. A sanity check that server.js actually
    # landed where the service expects it (rather than trusting the copy
    # silently succeeded) is still worth doing before starting it.
    $serverJsPath = Join-Path $ServiceStandaloneDir "server.js"
    if (-not (Test-Path -LiteralPath $serverJsPath)) {
        throw "Expected server.js not found at $serverJsPath after copy - check the build output."
    }
    Write-Host "==> [9/10] Starting Windows service '$NodeServiceName'..."
    Invoke-NativeCommand -FilePath "net" -ArgumentList @("start", $NodeServiceName) -FailureMessage "net start $NodeServiceName failed"
    Wait-Seconds10

    # -------------------------------------------------------------------
    # Step 10: Start remaining services (the Windows service was already started in step 9)
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
