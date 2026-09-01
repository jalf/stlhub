# macOS packaging

The release workflow publishes STLHub for `osx-x64` and `osx-arm64` and then runs
[`package-macos.ps1`](package-macos.ps1) to turn each raw publish folder into a
native distribution:

| Output | What it is |
|---|---|
| `STLHub.app` | The application bundle — real Dock/Finder icon, `Info.plist`, ad-hoc code signature. |
| `STLHub-osx-<arch>.dmg` | A compressed disk image with a drag-to-`/Applications` layout. |
| `STLHub-osx-<arch>.tar.gz` | The same `.app`, tarred, for people who prefer the command line. |

## Files here

| File | Purpose |
|---|---|
| `Info.plist` | Bundle manifest template. `__VERSION__` is replaced with the release version. |
| `STLHub.entitlements` | JIT entitlements the .NET runtime needs; without them the Apple Silicon kernel kills an ad-hoc-signed build on launch. |
| `STLHub.icns` | App icon, consumed as-is by the packaging script. |
| `AppIcon.png` | 1024×1024 master the `.icns` is generated from. Keep it in sync when the logo changes. |
| `package-macos.ps1` | Assembles the `.app`, signs it, and builds the `.dmg`. macOS only. |

## Running it locally

```powershell
dotnet publish src/STLHub/STLHub.csproj -c Release -r osx-arm64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:Version=1.3.0 -o publish/STLHub-osx-arm64

./build/macos/package-macos.ps1 `
  -PublishDir publish/STLHub-osx-arm64 `
  -Version 1.3.0 `
  -ArtifactName STLHub-osx-arm64 `
  -OutputDir dist
```

## Regenerating the icon

The icon is derived from `src/STLHub/Assets/logo.png` (the symbol above the
wordmark). To rebuild `AppIcon.png` and `STLHub.icns` after the logo changes,
run a throwaway [ImageSharp](https://github.com/SixLabors/ImageSharp) program
that crops the logo to the hexagon, pads it to a square with ~12% margin, and
emits the `.iconset`, then:

```bash
iconutil -c icns path/to/STLHub.iconset -o build/macos/STLHub.icns
```

The generator source used for the current icon is kept in the PR that introduced
this folder; it is not part of the build because the icon rarely changes.

## Code signing

The bundle is only **ad-hoc** signed (`codesign --sign -`). That is enough for
the app to launch, but Gatekeeper still treats it as unidentified because it is
not signed with an Apple Developer ID and not notarized. First-launch
instructions for users are in the top-level `README.md`. If a Developer ID
becomes available, replace the `--sign -` calls with the identity and add a
`xcrun notarytool submit` / `xcrun stapler staple` step after the `.dmg` is
built.
