[CmdletBinding()]
param(
    [switch]$Install
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

$modOutputDir = Join-Path $projectRoot "bin"
New-Item -ItemType Directory -Force -Path $modOutputDir | Out-Null

$modOutput = Join-Path $modOutputDir "CyberHookAP.dll"
$modSources = Get-ChildItem (Join-Path $projectRoot "src") -Recurse -Filter *.cs |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }
$modReferences = @(
    $vendorOutput,
    (Join-Path $vendorRoot "DLLs\websocket-sharp.dll"),
    (Join-Path $gameRoot "MelonLoader\net35\0Harmony.dll"),
    (Join-Path $gameRoot "MelonLoader\net35\MelonLoader.dll"),
    (Join-Path $gameRoot "MelonLoader\net35\Newtonsoft.Json.dll"),
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

Invoke-CSharpCompile `
    -OutputAssembly $modOutput `
    -SourceFiles $modSources `
    -References $modReferences `
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
