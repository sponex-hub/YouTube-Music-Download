param([switch]$SkipTools)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8 is required (included with Windows 10/11).' }
$output = Join-Path $root 'native-dist'
New-Item -ItemType Directory -Force $output | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 `
    "/out:$output\YouTube Audio.exe" "/win32icon:$root\build\icon.ico" "/win32manifest:$root\native\app.manifest" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll `
    "$root\native\Program.cs" "$root\native\Downloader.cs"
if ($LASTEXITCODE -ne 0) { throw 'Native app compilation failed.' }
Copy-Item "$root\native\App.config" "$output\YouTube Audio.exe.config" -Force
Copy-Item "$root/LICENSE", "$root/README.md", "$root/FIXES.md" -Destination $output -Force
Copy-Item "$root/third-party" -Destination $output -Recurse -Force
if (!$SkipTools) {
    New-Item -ItemType Directory -Force "$output/tools" | Out-Null
    foreach ($name in @('yt-dlp.exe', 'deno.exe', 'ffmpeg.exe', 'ffprobe.exe')) {
        $file = Join-Path $root "bin/$name"
        if (!(Test-Path -LiteralPath $file)) { throw "Missing $name. Run scripts/setup-binaries.ps1 first." }
        Copy-Item -LiteralPath $file -Destination "$output/tools/$name" -Force
    }
}
Write-Host "Built: $output\YouTube Audio.exe"
