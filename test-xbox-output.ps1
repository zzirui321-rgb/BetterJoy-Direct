# Creates temporary Xbox devices and sends one synthetic axis, then neutral.
# Run separately from games / controller software and the Bluetooth diagnostic.
$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'outputs\BetterJoy-Direct'
$compiler = Join-Path $PSScriptRoot 'packages\Microsoft.NET.Compilers.3.11.0\tools\csc.exe'
& $compiler /nologo /target:exe /platform:x64 "/r:$out\BetterJoyForCemu.exe" "/r:$out\Nefarius.ViGEm.Client.dll" "/out:$out\XboxOutputTests.exe" "$PSScriptRoot\tests\XboxOutputTests.cs"
if ($LASTEXITCODE) { throw 'Output test build failed' }
Copy-Item -LiteralPath "$out\BetterJoyForCemu.exe.config" -Destination "$out\XboxOutputTests.exe.config"
& "$out\XboxOutputTests.exe"
if ($LASTEXITCODE) { throw 'XInput output regression failed' }
