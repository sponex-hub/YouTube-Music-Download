$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $root 'bin'
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/')
if ([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($temp)) -ne $tempRoot) { throw 'Invalid temporary directory' }
New-Item -ItemType Directory -Force $bin, $temp | Out-Null
try {
    Invoke-WebRequest 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -OutFile "$temp/yt-dlp.exe"
    Invoke-WebRequest 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS' -OutFile "$temp/sums.txt"
    $expected = ((Get-Content "$temp/sums.txt" | Where-Object { $_ -match '\s+yt-dlp.exe$' }) -split '\s+')[0]
    if (!$expected -or (Get-FileHash "$temp/yt-dlp.exe" -Algorithm SHA256).Hash -ne $expected) { throw 'yt-dlp checksum mismatch' }
    Invoke-WebRequest 'https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip' -OutFile "$temp/deno.zip"
    Expand-Archive "$temp/deno.zip" "$temp/deno"
    Invoke-WebRequest 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip' -OutFile "$temp/ffmpeg.zip"
    Expand-Archive "$temp/ffmpeg.zip" "$temp/ffmpeg"
    Copy-Item "$temp/yt-dlp.exe", "$temp/deno/deno.exe" -Destination $bin -Force
    Get-ChildItem "$temp/ffmpeg" -Recurse -File | Where-Object { $_.Name -in 'ffmpeg.exe', 'ffprobe.exe' } | Copy-Item -Destination $bin -Force
    Write-Host 'Download tools updated. Restart the app and rebuild packaged versions.'
} finally {
    Remove-Item -LiteralPath $temp -Recurse -Force
}
