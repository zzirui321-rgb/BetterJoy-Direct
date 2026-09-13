$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'outputs\BetterJoy-Direct'
$compiler = Join-Path $PSScriptRoot 'packages\Microsoft.NET.Compilers.3.11.0\tools\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /r:System.Windows.Forms.dll "/r:$out\BetterJoyForCemu.exe" "/out:$out\FeatureRegressionTests.exe" "$PSScriptRoot\tests\FeatureRegressionTests.cs"
if ($LASTEXITCODE) { throw 'Feature test build failed' }
Copy-Item -LiteralPath "$out\BetterJoyForCemu.exe.config" -Destination "$out\FeatureRegressionTests.exe.config"
& "$out\FeatureRegressionTests.exe"
if ($LASTEXITCODE) { throw 'Feature regression failed' }
