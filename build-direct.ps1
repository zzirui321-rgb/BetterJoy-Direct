$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$src = Join-Path $root 'BetterJoyForCemu'
$out = Join-Path $root 'outputs\BetterJoy-Direct'
$obj = Join-Path $root 'work-build'
New-Item -ItemType Directory -Path $out,$obj -Force | Out-Null
$compiler = Join-Path $root 'packages\Microsoft.NET.Compilers.3.11.0\tools\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Run python tools/restore.py first.' }
& $compiler /nologo /target:exe /r:System.Windows.Forms.dll /r:System.Drawing.dll "/out:$obj\ResourceCompiler.exe" "$root\tools\ResourceCompiler.cs"
if ($LASTEXITCODE) { throw 'Resource compiler build failed' }
[xml]$project = Get-Content -LiteralPath "$src\BetterJoy.csproj"
$argsList = @('/nologo','/target:winexe','/platform:x64','/optimize+',"/out:$out\BetterJoyForCemu.exe", "/win32icon:$src\Icons\betterjoyforcemu_icon.ico", "/win32manifest:$src\Properties\app.manifest")
foreach ($reference in $project.Project.ItemGroup.Reference) {
    if (!$reference) { continue }
    if ($reference.HintPath) {
        $dll = [IO.Path]::GetFullPath((Join-Path $src $reference.HintPath))
        $argsList += "/r:$dll"
        Copy-Item -LiteralPath $dll -Destination $out
    } else { $argsList += '/r:' + $reference.Include + '.dll' }
}
foreach ($resource in $project.Project.ItemGroup.EmbeddedResource) {
    if (!$resource) { continue }
    $name = 'BetterJoyForCemu.' + ($resource.Include -replace '\\','.' -replace '\.resx$','.resources')
    if ($resource.Include -eq '3rdPartyControllers.resx') { $name = 'BetterJoyForCemu._3rdPartyControllers.resources' }
    & "$obj\ResourceCompiler.exe" "$src\$($resource.Include)" "$obj\$name"
    if ($LASTEXITCODE) { throw "Resource conversion failed: $name" }
    $argsList += "/resource:$obj\$name,$name"
}
foreach ($source in $project.Project.ItemGroup.Compile) {
    if ($source) { $argsList += Join-Path $src $source.Include }
}
& $compiler @argsList
if ($LASTEXITCODE) { throw 'Build failed' }
Copy-Item -LiteralPath "$src\App.config" -Destination "$out\BetterJoyForCemu.exe.config"
New-Item -ItemType Directory -Path "$out\x64" -Force | Out-Null
Copy-Item -LiteralPath "$src\x64\hidapi.dll" -Destination "$out\x64"
Copy-Item -LiteralPath "$root\LICENSE" -Destination $out
$driverSource = Join-Path $src 'Drivers\ViGEmBusSetup_x64.msi'
$driverHash = (Get-FileHash -LiteralPath $driverSource -Algorithm SHA256).Hash
$expectedDriverHash = '5ABBBA8A4A07AAAEB50B4666183B2F243E0E5AD288026D2A9F3595ED237C4B28'
if ($driverHash -ne $expectedDriverHash) { throw 'ViGEmBus installer hash mismatch' }
$driverOut = Join-Path $out 'Drivers'
New-Item -ItemType Directory -Path $driverOut -Force | Out-Null
Copy-Item -LiteralPath $driverSource -Destination $driverOut
Copy-Item -LiteralPath "$src\Drivers\README.txt" -Destination $driverOut
Write-Output "Built $out\BetterJoyForCemu.exe"
