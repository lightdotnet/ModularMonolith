<#
.SYNOPSIS
    Registers this Next.js (standalone output) app as a Windows service via NSSM.

.DESCRIPTION
    1. Installs the Windows service (nssm install) pointing at node.exe.
    2. Sets the service's working directory to the deployed standalone output folder.
    3. Sets the app's startup parameters (the entry script, e.g. server.js).
    4. Sets the "run as" account (NSSM's ObjectName) if RunAsUser is supplied;
       skipped entirely when RunAsUser is empty, leaving the service installed
       to run as NSSM's default (LocalSystem).
    5. Sets the environment variables the app needs at boot (PORT, NODE_ENV).
    6. Sets the stdout log file path.
    7. Sets the stderr log file path.
    8. Sets the exit action so the service restarts by default if the process exits.
    9. Starts the service (net start).

    This is a one-time, manual, out-of-band script an operator runs to register the
    service before `deploy-nssm.ps1` takes over managing it (via net stop/net start)
    on every subsequent deploy. Running it again against an already-registered service
    name simply re-applies the same nssm set commands.

.PARAMETER NssmFolderPath
    Folder containing nssm.exe. Defaults to "." (the current directory), so nssm
    is invoked as ".\nssm.exe" - matching today's behavior of running nssm.exe
    from wherever this script is invoked, just now via an explicit relative path
    instead of relying on nssm.exe being resolvable via PATH.
    Example: "C:\Tools\nssm\win64"

.PARAMETER NodeServiceName
    The Windows service name to register via NSSM. Same name deploy-nssm.ps1's
    NodeServiceName parameter must be pointed at, so it can stop/start this service.
    Example: "NextJsApp"

.PARAMETER ServiceFolderPath
    The deployed/live standalone output folder that server.js runs from (i.e.
    ServiceFolderPath\standalone from deploy-nssm.ps1's own naming). Used as the
    service's AppDirectory. Example: "D:\IISApps\MyApp\standalone"

.PARAMETER AppParameters
    The startup parameters passed to node.exe, i.e. the entry script to run.
    Example: "server.js"

.PARAMETER RunAsUser
    The Windows account NSSM should run the service as (NSSM's ObjectName), e.g. a
    domain/local service account instead of the NSSM default (LocalSystem). Defaults
    to "" (empty), which skips setting ObjectName entirely, leaving the service to
    run as LocalSystem - today's exact behavior when not supplied. Only takes effect
    when non-empty. Example: ".\svc-nextjs" or "DOMAIN\svc-nextjs"

.PARAMETER RunAsPassword
    The password for RunAsUser, as a SecureString. Only read/used when RunAsUser is
    non-empty; decrypted to plaintext only at the point of passing it to nssm.exe
    (NSSM's CLI requires a plaintext argument - there's no way around that), never
    stored back in plaintext otherwise. Ignored when RunAsUser is empty.
    Example: (Read-Host -AsSecureString "Enter service account password")

.PARAMETER Port
    The port the app listens on, passed via the PORT environment variable.
    Example: "3000"

.PARAMETER NodeEnv
    The Node environment, passed via the NODE_ENV environment variable.
    Example: "production"

.PARAMETER NodeOptions
    Extra flags passed via the NODE_OPTIONS environment variable. Defaults to ""
    (empty), which omits NODE_OPTIONS from AppEnvironmentExtra entirely - today's
    behavior when not supplied. Example: "--use-system-ca"

.PARAMETER StdoutLogPath
    File path the service's stdout is redirected to. Example: "D:\Logs\MyApp\stdout.log"

.PARAMETER StderrLogPath
    File path the service's stderr is redirected to. Example: "D:\Logs\MyApp\stderr.log"

.EXAMPLE
    .\init-nssm.ps1

    Runs with the default config values below.

.EXAMPLE
    .\init-nssm.ps1 -NssmFolderPath "C:\Tools\nssm\win64" -NodeServiceName "MyAppService" -ServiceFolderPath "D:\IISApps\MyApp\standalone" -AppParameters "server.js" -Port "3000" -NodeEnv "production" -StdoutLogPath "D:\Logs\MyApp\stdout.log" -StderrLogPath "D:\Logs\MyApp\stderr.log"

    Runs with explicit overrides for every config value.

.EXAMPLE
    .\init-nssm.ps1 -RunAsUser ".\svc-nextjs" -RunAsPassword (Read-Host -AsSecureString "Enter service account password")

    Registers the service to run as a specific Windows account instead of the
    NSSM default (LocalSystem), prompting for the password without ever
    displaying or storing it in plaintext.
#>

[CmdletBinding()]
param(
    # Folder containing nssm.exe; "." = invoke it as ".\nssm.exe"
    [string]$NssmFolderPath = ".",

    # Windows service name to register via NSSM
    [string]$NodeServiceName = "NextJs-Portal",

    # Deployed standalone output folder (AppDirectory) that server.js runs from
    [string]$ServiceFolderPath = "C:\IT\IIS_Services\NextJsApp\standalone",

    # Startup parameters passed to node.exe (the entry script)
    [string]$AppParameters = "server.js",

    # Windows account to run the service as (NSSM's ObjectName); "" = skip, keep LocalSystem
    [string]$RunAsUser = "",

    # Password for RunAsUser, as a SecureString; ignored when RunAsUser is empty
    [securestring]$RunAsPassword,

    # Port the app listens on, passed via the PORT environment variable
    [string]$Port = "3000",

    # Node environment, passed via the NODE_ENV environment variable
    [string]$NodeEnv = "production",

    # Extra flags passed via the NODE_OPTIONS environment variable; "" = omit it
    [string]$NodeOptions = "",

    # File path the service's stdout is redirected to
    [string]$StdoutLogPath = "C:\IT\IIS_Services\NextJsApp\logs\stdout.log",

    # File path the service's stderr is redirected to
    [string]$StderrLogPath = "C:\IT\IIS_Services\NextJsApp\logs\stderr.log"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$NssmExe = Join-Path $NssmFolderPath "nssm.exe"

& $NssmExe install $NodeServiceName "C:\Program Files\nodejs\node.exe"

& $NssmExe set $NodeServiceName AppDirectory $ServiceFolderPath

& $NssmExe set $NodeServiceName AppParameters $AppParameters

if ($RunAsUser) {
    # NSSM's CLI requires the password as a plaintext argument - there is no
    # way around that. Decrypt only right here, right before the call, rather
    # than ever holding/passing it around as plaintext otherwise.
    $RunAsPasswordPtr = [System.Runtime.InteropServices.Marshal]::SecureStringToGlobalAllocUnicode($RunAsPassword)
    try {
        $RunAsPasswordPlainText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($RunAsPasswordPtr)
        & $NssmExe set $NodeServiceName ObjectName $RunAsUser $RunAsPasswordPlainText
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeGlobalAllocUnicode($RunAsPasswordPtr)
    }
}

$envExtra = @("PORT=$Port", "NODE_ENV=$NodeEnv")
if ($NodeOptions) { $envExtra += "NODE_OPTIONS=$NodeOptions" }
& $NssmExe set $NodeServiceName AppEnvironmentExtra @envExtra

& $NssmExe set $NodeServiceName AppStdout $StdoutLogPath

& $NssmExe set $NodeServiceName AppStderr $StderrLogPath

& $NssmExe set $NodeServiceName AppExit Default Restart

net start $NodeServiceName
