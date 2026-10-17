# WTT-R61 Fatman build notes

## Server

The project builds against an SPT server runtime whose assemblies are not
shipped in the final server-only ZIP. A .NET 10 SDK is required. If multiple
.NET installations are present, invoke the .NET 10 host explicitly instead of
relying on whichever `dotnet` appears first on `PATH`.

```powershell
$env:SPT_SERVER_DIR = 'D:\SPT\SPT_Runtime\'
dotnet build `
  .\src\FatmanServer\FatmanServer.csproj -c Release
```

`-p:SptServerDir=...` also works and takes precedence.

The source has been compiled against SPT 4.1.3, 4.1.5, and 4.1.6 references.

## Optional client fallback

The server-only release does not require a BepInEx plugin. The optional
FatmanShaderFix source can be built against an SPT 4.1.x client:

```powershell
$env:TARKOV_DIR = 'D:\SPT'
dotnet build `
  .\src\FatmanShaderFix\FatmanShaderFix.csproj -c Release
```

`-p:TarkovDir=...` also works and takes precedence.

## Bundle patcher

The patcher is intentionally fail-closed. It requires exactly six armor
bundles, requires exactly two Materials per bundle, rewrites one external CAB
per bundle and two Shader PPtrs per bundle, then reopens every output to verify
the result. Python 3 with `UnityPy` is required.

```powershell
python -m pip install -r .\requirements-bundle-tools.txt

python `
  .\tools\patch_fatman_materials.py `
  .\Release\SPT\user\mods\madmanbeavis-wttr61fatman\bundles\FatmanArmor `
  .\work\FatmanArmor `
  .\work\wtt_shaders.bundle `
  --expect-bundles 6
```

The unmodified `bundles.json` is tracked under:

```text
deploy/SPT/user/mods/madmanbeavis-wttr61fatman/bundles.json
```
