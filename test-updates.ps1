$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'outputs\BetterJoy-Direct'
$compiler = Join-Path $PSScriptRoot 'packages\Microsoft.NET.Compilers.3.11.0\tools\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /r:System.Net.Http.dll "/r:$out\BetterJoyForCemu.exe" "/out:$out\UpdateTests.exe" "$PSScriptRoot\tests\UpdateTests.cs"
if ($LASTEXITCODE) { throw 'Update test build failed' }
Copy-Item -LiteralPath "$out\BetterJoyForCemu.exe.config" -Destination "$out\UpdateTests.exe.config"
& "$out\UpdateTests.exe"
if ($LASTEXITCODE) { throw 'Update regression failed' }
