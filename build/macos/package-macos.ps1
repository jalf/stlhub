#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Wraps a macOS `dotnet publish` output in a native STLHub.app bundle and a
    drag-to-install .dmg.

.DESCRIPTION
    Runs on macOS only (uses `codesign`, `hdiutil` and, best-effort, `osascript`).
    Produces:
      <OutputDir>/STLHub.app          - the application bundle
      <OutputDir>/<ArtifactName>.dmg  - a compressed disk image with an
                                        /Applications shortcut

    The bundle is ad-hoc code-signed with the JIT entitlements the .NET runtime
    needs; without a signature the Apple Silicon kernel refuses to launch it.

.PARAMETER PublishDir
    Folder containing the self-contained `dotnet publish` output (the STLHub
    executable and its runtime files).

.PARAMETER Version
    Marketing version, e.g. "1.3.0". Written into CFBundleShortVersionString and
    CFBundleVersion.

.PARAMETER ArtifactName
    Base name for the .dmg, e.g. "STLHub-osx-arm64".

.PARAMETER OutputDir
    Where the .app and .dmg are written. Created if missing.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PublishDir,
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $ArtifactName,
    [string] $OutputDir = "dist"
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true   # a failing codesign/hdiutil aborts the build
Set-StrictMode -Version Latest

if ($IsWindows) { throw "package-macos.ps1 must run on macOS." }

$scriptDir   = Split-Path -Parent $PSCommandPath
$appName     = 'STLHub'
$executable  = 'STLHub'

$PublishDir = (Resolve-Path $PublishDir).Path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$OutputDir = (Resolve-Path $OutputDir).Path

$appBundle = Join-Path $OutputDir "$appName.app"
$contents  = Join-Path $appBundle 'Contents'
$macOS     = Join-Path $contents  'MacOS'
$resources = Join-Path $contents  'Resources'

Write-Host "==> Building $appName.app ($Version) from $PublishDir"
if (Test-Path $appBundle) { Remove-Item -Recurse -Force $appBundle }
New-Item -ItemType Directory -Force -Path $macOS, $resources | Out-Null

# Payload -------------------------------------------------------------------
Copy-Item -Recurse -Force (Join-Path $PublishDir '*') $macOS
# Debug symbols must not ship: they also break codesign's resource sealing.
Get-ChildItem -Recurse -Path $macOS -Include *.pdb, *.db, settings.json, *.user, *.env |
    Remove-Item -Force -ErrorAction SilentlyContinue
$mainExe = Join-Path $macOS $executable
if (-not (Test-Path $mainExe)) { throw "Executable '$executable' not found in $PublishDir" }
chmod +x $mainExe

# Icon --------------------------------------------------------------------------
Copy-Item -Force (Join-Path $scriptDir 'STLHub.icns') (Join-Path $resources 'STLHub.icns')

# Info.plist (with the version substituted in) ---------------------------------
$plist = Get-Content -Raw (Join-Path $scriptDir 'Info.plist')
$plist = $plist.Replace('__VERSION__', $Version)
Set-Content -NoNewline -Path (Join-Path $contents 'Info.plist') -Value $plist

# PkgInfo (optional but expected by Finder) ------------------------------------
Set-Content -NoNewline -Path (Join-Path $contents 'PkgInfo') -Value 'APPL????'

# Ad-hoc code signing --------------------------------------------------------
$entitlements = Join-Path $scriptDir 'STLHub.entitlements'
Write-Host "==> Ad-hoc signing nested libraries"
Get-ChildItem -Recurse -Path $macOS -Include *.dylib, *.so |
    ForEach-Object { codesign --force --timestamp=none --sign - $_.FullName }

Write-Host "==> Ad-hoc signing the bundle"
codesign --force --timestamp=none --entitlements $entitlements --sign - $appBundle
codesign --verify --deep --strict --verbose=2 $appBundle

# Disk image --------------------------------------------------------------------
$dmgPath  = Join-Path $OutputDir "$ArtifactName.dmg"
$volName  = "$appName $Version"
if (Test-Path $dmgPath) { Remove-Item -Force $dmgPath }

$staging = Join-Path ([System.IO.Path]::GetTempPath()) "stlhub-dmg-$([guid]::NewGuid())"
$rwDmg   = Join-Path ([System.IO.Path]::GetTempPath()) "stlhub-rw-$([guid]::NewGuid()).dmg"
New-Item -ItemType Directory -Force -Path $staging | Out-Null
try {
    Copy-Item -Recurse -Force $appBundle (Join-Path $staging "$appName.app")
    ln -s /Applications (Join-Path $staging 'Applications')

    Write-Host "==> Laying out the disk image window"
    hdiutil create -volname $volName -srcfolder $staging -fs HFS+ -format UDRW -ov $rwDmg | Out-Null
    $mountPoint = "/Volumes/$volName"
    hdiutil attach $rwDmg -nobrowse -noautoopen -mountpoint $mountPoint | Out-Null
    try {
        # Arrange the icons the way a hand-made macOS installer looks: the app
        # on the left, the Applications drop target on the right, no chrome.
        # Finder scripting needs a GUI session and occasionally hangs, so it runs
        # under a watchdog and a failure just leaves the default grid layout.
        $osaScript = @"
tell application "Finder"
    tell disk "$volName"
        open
        set current view of container window to icon view
        set toolbar visible of container window to false
        set statusbar visible of container window to false
        set the bounds of container window to {200, 120, 740, 460}
        set theViewOptions to the icon view options of container window
        set arrangement of theViewOptions to not arranged
        set icon size of theViewOptions to 128
        set position of item "$appName.app" of container window to {140, 170}
        set position of item "Applications" of container window to {400, 170}
        update without registering applications
        delay 1
        close
    end tell
end tell
"@
        $osaFile = Join-Path ([System.IO.Path]::GetTempPath()) "stlhub-layout-$([guid]::NewGuid()).applescript"
        Set-Content -Path $osaFile -Value $osaScript
        $proc = Start-Process -FilePath 'osascript' -ArgumentList $osaFile -PassThru -NoNewWindow
        if (-not $proc.WaitForExit(90000)) {
            Write-Warning "DMG window layout timed out; shipping the default grid layout."
            $proc.Kill()
        }
        Remove-Item -Force $osaFile -ErrorAction SilentlyContinue
        sync
    }
    catch {
        Write-Warning "DMG window layout skipped: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path $mountPoint) {
            hdiutil detach $mountPoint -force | Out-Null
        }
    }

    Write-Host "==> Creating $dmgPath"
    hdiutil convert $rwDmg -format UDZO -imagekey zlib-level=9 -ov -o $dmgPath | Out-Null
}
finally {
    Remove-Item -Recurse -Force $staging -ErrorAction SilentlyContinue
    Remove-Item -Force $rwDmg -ErrorAction SilentlyContinue
}

Write-Host "==> Done"
Write-Host "    $appBundle"
Write-Host "    $dmgPath"
