<#
.SYNOPSIS
    Creates the Google Play upload keystore for Ghode Ki Chaal.

.DESCRIPTION
    Finds keytool (preferring Unity's embedded OpenJDK) and runs it
    INTERACTIVELY, so your passwords are typed straight into keytool and are
    never stored in this script, in shell history, or in the repo.

    Run this ONCE per studio (not per developer). Afterwards:
      1. Back the .keystore file up in TWO places (password manager + offline).
      2. In Unity: Project Settings > Player > Android > Publishing Settings >
         Custom Keystore -> select the file, enter passwords, and tick
         "Keystore Manager..." remember-for-session only. Do NOT commit
         ProjectSettings changes that embed the keystore path with passwords.
      3. Enroll in Play App Signing on the Play Console (Google keeps the app
         signing key; this file is only the UPLOAD key, and can be reset by
         Google support if lost — but treat it as unlosable anyway).

    The keystore lands OUTSIDE the repo by default (never committed;
    *.keystore is also git-ignored as a second line of defense).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools/create-release-keystore.ps1
#>

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# 1. Find keytool — Unity's embedded OpenJDK first, then PATH.
# ---------------------------------------------------------------------------
$keytool = $null
$unityJdkGlobs = @(
    "$env:ProgramFiles\Unity\Hub\Editor\*\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe",
    "$env:ProgramFiles\Unity*\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
)
foreach ($glob in $unityJdkGlobs) {
    $found = Get-ChildItem -Path $glob -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($found) { $keytool = $found.FullName; break }
}
if (-not $keytool) {
    $cmd = Get-Command keytool -ErrorAction SilentlyContinue
    if ($cmd) { $keytool = $cmd.Source }
}
if (-not $keytool) {
    Write-Error ("keytool not found. Install Unity's Android Build Support " +
        "(includes OpenJDK) or any JDK, then re-run.")
}
Write-Host "Using keytool: $keytool"

# ---------------------------------------------------------------------------
# 2. Pick an output location OUTSIDE the repo.
# ---------------------------------------------------------------------------
$keyDir = Join-Path $env:USERPROFILE 'GhodeKiChaal-keys'
if (-not (Test-Path $keyDir)) { New-Item -ItemType Directory -Path $keyDir | Out-Null }
$keystorePath = Join-Path $keyDir 'ghodekichaal-upload.keystore'

if (Test-Path $keystorePath) {
    Write-Error ("A keystore already exists at $keystorePath - refusing to " +
        "overwrite. Move it away first if you REALLY mean to recreate it.")
}

# ---------------------------------------------------------------------------
# 3. Run keytool interactively — it prompts for passwords + identity itself.
#    RSA 2048, 30-year validity (Play requires validity past Oct 2033).
# ---------------------------------------------------------------------------
Write-Host ''
Write-Host 'keytool will now ask for a keystore password and identity details.'
Write-Host 'Use a strong password and save it in the studio password manager'
Write-Host 'IMMEDIATELY - an upload keystore with a lost password is scrap.'
Write-Host ''

& $keytool -genkeypair `
    -keystore $keystorePath `
    -alias ghodekichaal `
    -keyalg RSA -keysize 2048 `
    -validity 10950

if ($LASTEXITCODE -ne 0) {
    Write-Error "keytool failed (exit $LASTEXITCODE). No keystore was created."
}

Write-Host ''
Write-Host "Done. Keystore: $keystorePath (alias: ghodekichaal)"
Write-Host 'NOW: back it up twice + store the password in the password manager.'
