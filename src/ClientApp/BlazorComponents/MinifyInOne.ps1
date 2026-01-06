Write-Host "=== Running Blazor Minification ==="

# -------------------------------
# Please install Node.js at first and run
#    'npm install uglify-js'
#    'npm install clean-css-cli'
# -------------------------------

# -------------------------------
# 1. Project root (assumes PS1 is in project root)
# -------------------------------
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# -------------------------------
# 2. Node executable full path
# -------------------------------
$nodeExe = "C:\Program Files\nodejs\node.exe"  # <-- Adjust if Node installed elsewhere
if (-not (Test-Path $nodeExe)) {
    Write-Error "Node.exe not found at $nodeExe. Please install Node.js."
    exit 1
}

# Restore npm packages if needed
if (-not (Test-Path "$root/node_modules")) {
    Write-Host "Restoring npm packages..."
    npm install
}

Write-Host "=== Pre-build ready ==="

# -------------------------------
# 3. Local Node tools
# -------------------------------
$uglifyJsPath = Join-Path $root "node_modules/uglify-js/bin/uglifyjs"
$cleanCssPath = Join-Path $root "node_modules/clean-css-cli/bin/cleancss"

if (-not (Test-Path $uglifyJsPath)) {
    Write-Error "UglifyJS not found at $uglifyJsPath — run 'npm install uglify-js'"
    exit 1
}
if (-not (Test-Path $cleanCssPath)) {
    Write-Error "clean-css not found at $cleanCssPath — run 'npm install clean-css-cli'"
    exit 1
}

# -------------------------------
# 4. Input folders
# -------------------------------
$jsSource = Join-Path $root "npm/js"
$cssSource = Join-Path $root "npm/css"

if (-not (Test-Path $jsSource)) { Write-Error "JS source folder '$jsSource' not found"; exit 1 }
if (-not (Test-Path $cssSource)) { Write-Error "CSS source folder '$cssSource' not found"; exit 1 }

# -------------------------------
# 5. Output files
# -------------------------------
$jsOutput = Join-Path $root "wwwroot/site.min.js"
$cssOutput = Join-Path $root "wwwroot/site.min.css"

New-Item -ItemType Directory -Force -Path (Split-Path $jsOutput) | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $cssOutput) | Out-Null

# -------------------------------
# 6. Minify JS
# -------------------------------
$jsFiles = Get-ChildItem $jsSource -Filter *.js | Where-Object { $_.Name -notlike "*.min.js" }
if ($jsFiles.Count -gt 0) {
    $jsArgs = $jsFiles | ForEach-Object { $_.FullName }
    Write-Host "Minifying JS -> $jsOutput"
    & $nodeExe $uglifyJsPath $jsArgs -o $jsOutput --compress --mangle
    Write-Host "JS minified -> $jsOutput"
} else {
    Write-Host "No JS files found to minify."
}

# -------------------------------
# 7. Minify CSS
# -------------------------------
$cssFiles = Get-ChildItem $cssSource -Filter *.css | Where-Object { $_.Name -notlike "*.min.css" }
if ($cssFiles.Count -gt 0) {
    $cssArgs = $cssFiles | ForEach-Object { "`"$($_.FullName)`"" }
    $cssArgsString = [string]::Join(' ', $cssArgs)
    Write-Host "Minifying CSS -> $cssOutput"
    & $nodeExe $cleanCssPath -o $cssOutput $cssArgsString
    Write-Host "CSS minified -> $cssOutput"
} else {
    Write-Host "No CSS files found to minify."
}

Write-Host "=== Minification complete ==="