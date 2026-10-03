param(
    [string]$UnityEditorData = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$outputDir = Join-Path $projectRoot 'Temp/InputFoundationChecks'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$referenceDir = Join-Path $UnityEditorData 'MonoBleedingEdge/lib/mono/4.8-api'
$engineDir = Join-Path $UnityEditorData 'Managed/UnityEngine'
$assemblyDir = Join-Path $projectRoot 'Library/ScriptAssemblies'
$nunit = Get-ChildItem (Join-Path $projectRoot 'Library/PackageCache') -Filter nunit.framework.dll -Recurse | Select-Object -First 1
if (!$nunit) { throw 'Open the project in Unity once to restore its test packages.' }
$references = @(
    (Join-Path $referenceDir 'mscorlib.dll'),
    (Join-Path $referenceDir 'System.dll'),
    (Join-Path $referenceDir 'System.Core.dll'),
    (Join-Path $referenceDir 'Facades/netstandard.dll'),
    (Join-Path $assemblyDir 'Unity.InputSystem.dll'),
    (Join-Path $assemblyDir 'Unity.InputSystem.TestFramework.dll'),
    (Join-Path $assemblyDir 'UnityEngine.TestRunner.dll'),
    $nunit.FullName
)
$references += (Get-ChildItem $engineDir -Filter '*.dll').FullName
$references += (Get-ChildItem (Join-Path $UnityEditorData 'Managed') -Filter 'UnityEditor.*.dll').FullName
$referenceArgs = $references | ForEach-Object { '-r:' + $_ }
$runtimeSources = (Get-ChildItem (Join-Path $projectRoot 'Assets/Scripts') -Recurse -Filter '*.cs').FullName
$testSources = (Get-ChildItem (Join-Path $projectRoot 'Assets/Tests') -Recurse -Filter '*.cs').FullName
$compiler = Join-Path $UnityEditorData 'DotNetSdkRoslyn/csc.dll'
$compiled = Join-Path $outputDir 'InputFoundationChecks.exe'
# Compile all runtime and test scripts against the installed Unity binaries.
& dotnet $compiler -nologo -noconfig -nostdlib -langversion:9 -target:exe "-out:$compiled" @referenceArgs @runtimeSources @testSources (Join-Path $PSScriptRoot 'Tests/PureInputTestRunner.cs')
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed.' }
$previousMonoPath = $env:MONO_PATH
try {
    $env:MONO_PATH = "$engineDir;$assemblyDir;$($nunit.DirectoryName)"
    & (Join-Path $UnityEditorData 'MonoBleedingEdge/bin/mono.exe') $compiled
    if ($LASTEXITCODE -ne 0) { throw 'Pure input tests failed.' }
} finally { $env:MONO_PATH = $previousMonoPath }
