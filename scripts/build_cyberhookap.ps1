[CmdletBinding()]
param(
    [switch]$Install,
    [string]$OutputName = "CyberHookAP.dll",
    [string]$OutputDir = "",
    [switch]$IncludeAuxVisual,
    [switch]$SkipVendorBuild
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = Split-Path -Parent $projectRoot
$toolsRoot = Join-Path $projectRoot "tools\Microsoft.Net.Compilers.Toolset.4.10.0"
$compiler = Join-Path $toolsRoot "tasks\net472\csc.exe"

if (!(Test-Path $compiler))
{
    throw "Missing Roslyn compiler at $compiler"
}

function Invoke-CSharpCompile {
    param(
        [string]$OutputAssembly,
        [string[]]$SourceFiles,
        [string[]]$References,
        [string[]]$Defines = @(),
        [string[]]$Resources = @()
    )

    $responseFile = Join-Path $env:TEMP ([System.IO.Path]::GetFileNameWithoutExtension($OutputAssembly) + "_" + [System.Guid]::NewGuid().ToString("N") + ".rsp")
    $lines = @(
        "/nologo",
        "/target:library",
        "/langversion:7.3",
        "/debug:portable",
        "/out:`"$OutputAssembly`""
    )

    if ($Defines.Count -gt 0)
    {
        $lines += "/define:$([string]::Join(';', $Defines))"
    }

    foreach ($reference in $References)
    {
        $lines += "/reference:`"$reference`""
    }

    foreach ($sourceFile in $SourceFiles)
    {
        $lines += "`"$sourceFile`""
    }

    foreach ($resource in $Resources)
    {
        $resourceParts = $resource -split '\|', 2
        if ($resourceParts.Count -ne 2)
        {
            throw "Invalid resource mapping: $resource"
        }

        $resourcePath = $resourceParts[0]
        $resourceName = $resourceParts[1]
        $lines += "/resource:`"$resourcePath`",`"$resourceName`""
    }

    Set-Content -Path $responseFile -Value $lines -Encoding ASCII
    & $compiler ("@" + $responseFile)
    $exitCode = $LASTEXITCODE
    Remove-Item -LiteralPath $responseFile -Force -ErrorAction SilentlyContinue
    if ($exitCode -ne 0)
    {
        throw "Compilation failed for $OutputAssembly"
    }
}

$vendorRoot = Join-Path $projectRoot "vendor\Archipelago.MultiClient.Net"
$vendorOutputDir = Join-Path $vendorRoot "build"
New-Item -ItemType Directory -Force -Path $vendorOutputDir | Out-Null

$vendorOutput = Join-Path $vendorOutputDir "Archipelago.MultiClient.Net.dll"
if (-not $SkipVendorBuild)
{
    $vendorSources = Get-ChildItem (Join-Path $vendorRoot "Archipelago.MultiClient.Net") -Recurse -Filter *.cs |
        Sort-Object FullName |
        ForEach-Object { $_.FullName }
    $vendorReferences = @(
        (Join-Path $gameRoot "MelonLoader\net35\Newtonsoft.Json.dll"),
        (Join-Path $vendorRoot "DLLs\websocket-sharp.dll")
    )

    Invoke-CSharpCompile `
        -OutputAssembly $vendorOutput `
        -SourceFiles $vendorSources `
        -References $vendorReferences `
        -Defines @("NET35")
}

$modOutputDir = Join-Path $projectRoot "bin"
if (![string]::IsNullOrEmpty($OutputDir))
{
    $modOutputDir = $OutputDir
}
New-Item -ItemType Directory -Force -Path $modOutputDir | Out-Null

$modOutput = Join-Path $modOutputDir $OutputName
$modSources = Get-ChildItem (Join-Path $projectRoot "src") -Recurse -Filter *.cs |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }
$modReferences = @(
    $vendorOutput,
    (Join-Path $vendorRoot "DLLs\websocket-sharp.dll"),
    (Join-Path $gameRoot "MelonLoader\net35\0Harmony.dll"),
    (Join-Path $gameRoot "MelonLoader\net35\MelonLoader.dll"),
    (Join-Path $gameRoot "MelonLoader\net35\Newtonsoft.Json.dll"),
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Drawing.dll",
    (Join-Path $gameRoot "CyberHook_Data\Managed\GlobalAssembly.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.CoreModule.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.AnimationModule.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.IMGUIModule.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.InputLegacyModule.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.PhysicsModule.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.TextRenderingModule.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.UIModule.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\UnityEngine.UI.dll"),
    (Join-Path $gameRoot "CyberHook_Data\Managed\Unity.TextMeshPro.dll")
)

$modResources = @()
$modResources += ((Join-Path $projectRoot "generated\ap_locations.json") + "|CyberHookAP.Runtime.ap_locations.json")
$modResources += ((Join-Path $projectRoot "generated\ap_levels.json") + "|CyberHookAP.Runtime.ap_levels.json")
$modResources += ((Join-Path $projectRoot "generated\cube_counts_by_level.csv") + "|CyberHookAP.Runtime.cube_counts_by_level.csv")
$modResources += ((Join-Path $projectRoot "generated\ap_runtime.json") + "|CyberHookAP.Runtime.ap_runtime.json")

$bootstrapDir = Join-Path $projectRoot "bootstrap"
if (Test-Path (Join-Path $bootstrapDir "GameData.shd"))
{
    $modResources += ((Join-Path $bootstrapDir "GameData.shd") + "|CyberHookAP.Runtime.bootstrap_gamedata.bin")
}
$bootstrapLevelsDir = Join-Path $bootstrapDir "Levels"
$bootstrapLevelFiles = @(
    "LevelData # 849d59bb-1b8a-41e7-b16c-489127224e46.shl",
    "LevelData # be8c2fc4-4a4f-4fa2-8048-916c64c4e5b9.shl",
    "LevelData # 2b9c4312-d2a0-44c0-a3d6-7332809de8a3.shl"
)
if (Test-Path $bootstrapLevelsDir)
{
    for ($i = 0; $i -lt $bootstrapLevelFiles.Count; $i++)
    {
        $path = Join-Path $bootstrapLevelsDir $bootstrapLevelFiles[$i]
        if (Test-Path $path)
        {
            $modResources += ($path + "|CyberHookAP.Runtime.bootstrap_level_" + $i)
        }
    }
}

$modDefines = @()
if ($IncludeAuxVisual)
{
    $modDefines += "IMPORTANTUPDATE"
    $modResources += ((Join-Path $projectRoot "coolimage.gif") + "|CyberHookAP.Runtime.ui_patch.bin")
}

Invoke-CSharpCompile `
    -OutputAssembly $modOutput `
    -SourceFiles $modSources `
    -References $modReferences `
    -Defines $modDefines `
    -Resources $modResources

if ($Install)
{
    $modsDir = Join-Path $gameRoot "Mods"
    Copy-Item $modOutput (Join-Path $modsDir "CyberHookAP.dll") -Force
    Copy-Item $vendorOutput (Join-Path $modsDir "Archipelago.MultiClient.Net.dll") -Force
    Copy-Item (Join-Path $vendorRoot "DLLs\websocket-sharp.dll") (Join-Path $modsDir "websocket-sharp.dll") -Force
}

Write-Host "Built $modOutput"
Write-Host "Built $vendorOutput"
if ($Install)
{
    Write-Host "Installed DLLs to $(Join-Path $gameRoot 'Mods')"
}
