param([switch]$Live)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'native-test-output'
New-Item -ItemType Directory -Force $output | Out-Null
& "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo /target:exe /platform:x64 /codepage:65001 `
    "/out:$output\tests.exe" /reference:System.dll /reference:System.Core.dll "$root\native\Tests.cs" "$root\native\Downloader.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
if ($Live) { & "$output\tests.exe" $root --live } else { & "$output\tests.exe" $root }
if ($LASTEXITCODE -ne 0) { throw 'Native tests failed.' }
