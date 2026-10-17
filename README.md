# WTT-R61 Fatman for SPT 4.1.x

Server-only adaptation of WTT-R61 Fatman 1.1.1 for SPT 4.1.x.

The original mod was built for SPT 4.0.13. This repository contains the adapted
server source, an optional client fallback, and the tooling used to rewrite the
Fatman armor material references for the 4.1.x asset layout.

## Download

The ready-to-install server-only package is published as a GitHub Release
attachment. It contains the Fatman armor models and the compatibility Shader
bundle.

Do not merge the Release ZIP into a server until the server has been stopped.

## What the server-only build does

- Registers all six R61 Fatman armor variants.
- Adds the armor variants to Ragman.
- Adds the Killa variant to the `bosskilla` `ArmorVest` pool.
- Rewrites the Material Shader PPtr in all six armor bundles.
- Rewrites the Unity external CAB reference to the supplied Shader bundle.
- Requires no BepInEx client plugin.

The final package was tested with SPT 4.1.5 and produced:

```text
[Fatman] registered 6/6 armor items
[Fatman] Killa armor variant added to bosskilla ArmorVest pool
[Fatman] Ragman assort updated (5 armor variants, 800k roubles, loyalty 4)
```

## Compatibility

| Component | Status |
| --- | --- |
| SPT 4.1.3 reference build | Compiles with 0 warnings / 0 errors |
| SPT 4.1.5 reference build | Runtime tested |
| SPT 4.1.6 reference build | Compiles with 0 warnings / 0 errors |
| SPT 4.1.4 | Not separately runtime tested |
| Client | EFT 0.16.9.40743 generation |

The mod metadata declares `~4.1.0`, so SPT 4.2.x is outside the compatibility
range.

## Repository layout

```text
src/FatmanServer/       Server-side SPT mod
src/FatmanShaderFix/    Optional client fallback; not required
tools/                  Bundle inspection and material-patching tools
BUILDING.md             Build and bundle-patch instructions
```

No prebuilt DLL, armor bundle, or third-party Shader binary is committed to the
source repository.

## Build

### Server

A .NET 10 SDK and an SPT 4.1.x server runtime are required.

```powershell
$env:SPT_SERVER_DIR = 'D:\SPT\SPT_Runtime\'
dotnet build `
  .\src\FatmanServer\FatmanServer.csproj -c Release
```

`-p:SptServerDir=...` can be used instead of the environment variable.

### Optional client fallback

```powershell
$env:TARKOV_DIR = 'D:\SPT'
dotnet build `
  .\src\FatmanShaderFix\FatmanShaderFix.csproj -c Release
```

The server-only Release does not require this plugin.

### Bundle patcher

Python 3 and `UnityPy` are required. See `BUILDING.md` for the complete command.

The patcher is fail-closed:

- Exactly six armor bundles must be processed.
- Each bundle must contain exactly two Materials.
- Every Material must start from the expected old Shader PathID.
- The Shader FileID must match the real external CAB index.
- Exactly one external Shader CAB must be rewritten per bundle.
- The target Shader bundle must contain the expected CAB, PathID, and name.
- Every output is reopened and verified before success is reported.

## Release binary contents

The Release ZIP contains the server-side DLL, `bundles.json`, six patched armor
bundles, the compatibility Shader bundle, and the original MIT license.

The Shader asset is included only in the binary Release. Its upstream
distribution does not contain a separate license notice; see
`THIRD-PARTY-NOTICES.md`.

## Credits

- Original WTT-R61 Fatman: `madmanbeavisx`
- SPT 4.1.x server-only adaptation: `M0rt1s0114`
- c11 / True North Shader asset: `c11gunworks-lab`

This project is not affiliated with Battlestate Games, SPT, c11gunworks-lab, or
the original WTT-R61 Fatman author.
