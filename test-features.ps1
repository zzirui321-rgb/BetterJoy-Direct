$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'outputs\BetterJoy-Direct'
$compiler = Join-Path $PSScriptRoot 'packages\Microsoft.NET.Compilers.3.11.0\tools\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /r:System.Windows.Forms.dll "/r:$out\BetterJoyForCemu.exe" "/out:$out\FeatureRegressionTests.exe" "$PSScriptRoot\tests\FeatureRegressionTests.cs"
if ($LASTEXITCODE) { throw 'Feature test build failed' }
Copy-Item -LiteralPath "$out\BetterJoyForCemu.exe.config" -Destination "$out\FeatureRegressionTests.exe.config"
& "$out\FeatureRegressionTests.exe"
if ($LASTEXITCODE) { throw 'Feature regression failed' }

$driver = Join-Path $out 'Drivers\ViGEmBusSetup_x64.msi'
if (!(Test-Path -LiteralPath $driver -PathType Leaf)) { throw 'Bundled ViGEmBus x64 installer is missing' }
if ((Get-Item -LiteralPath $driver).Length -le 0) { throw 'Bundled ViGEmBus x64 installer is empty' }
$expectedDriverHash = '5ABBBA8A4A07AAAEB50B4666183B2F243E0E5AD288026D2A9F3595ED237C4B28'
$actualDriverHash = (Get-FileHash -LiteralPath $driver -Algorithm SHA256).Hash
if ($actualDriverHash -ne $expectedDriverHash) { throw "Bundled ViGEmBus hash mismatch: $actualDriverHash" }
$driverSignature = Get-AuthenticodeSignature -LiteralPath $driver
if ($driverSignature.Status -ne 'Valid') { throw "Bundled ViGEmBus signature is not valid: $($driverSignature.Status)" }
if (!(Test-Path -LiteralPath (Join-Path $out 'Drivers\README.txt') -PathType Leaf)) { throw 'Bundled driver provenance note is missing' }

$installerSource = Get-Content -LiteralPath "$PSScriptRoot\BetterJoyForCemu\ViGEmBusInstaller.cs" -Raw
if ($installerSource -match '(?i)/(qn|quiet|passive)\b') { throw 'Driver installer must not use a silent or passive MSI switch' }
if (!$installerSource.Contains('/norestart')) { throw 'Driver installer must prevent an automatic restart' }
if (!$installerSource.Contains('Verb = "runas"')) { throw 'Driver installer must request Windows UAC explicitly' }
if (!$installerSource.Contains('MessageBoxButtons.YesNo')) { throw 'Driver installer must ask for confirmation before launch' }

Write-Output "PASS: signed ViGEmBus installer $actualDriverHash"
Write-Output 'PASS: driver installation remains explicit, interactive, UAC-gated, and no-restart'
