$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'outputs\BetterJoy-Direct'
$compiler = Join-Path $PSScriptRoot 'packages\Microsoft.NET.Compilers.3.11.0\tools\csc.exe'
& $compiler /nologo /target:exe /platform:x64 "/r:$out\BetterJoyForCemu.exe" "/out:$out\InputTests.exe" "$PSScriptRoot\tests\InputTests.cs"
if ($LASTEXITCODE) { throw 'Test build failed' }
Copy-Item -LiteralPath "$out\BetterJoyForCemu.exe.config" -Destination "$out\InputTests.exe.config"
& "$out\InputTests.exe"
if ($LASTEXITCODE) { throw 'Regression test failed' }
