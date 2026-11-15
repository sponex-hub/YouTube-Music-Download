# YouTube Audio — Windows native

A small Windows desktop app for downloading a single YouTube video's audio as MP3 or WebM. This fork replaces Electron with **C# / Windows Forms on .NET Framework 4.8**, supplied with Windows 10/11. No Chromium, Node.js, Python installation, web server or app extraction at startup.

## Use

1. Extract the entire portable ZIP to a folder on your PC.
2. Open **YouTube Audio.exe**. Keep the **tools** folder next to the executable.
3. Paste a YouTube or YouTube Music link, choose MP3 or WebM, and choose a destination folder.
4. Click **Descarcă audio**. The app reports progress, conversion, completion and actual errors. **Anulează** stops the downloader and its FFmpeg subprocess.

The interface is in Romanian. The destination folder is remembered under `%LOCALAPPDATA%\YouTubeAudio\folder.txt`. No browser cookies or account credentials are collected.

**MP3:** 192 kbps constant bitrate, 44.1 kHz, stereo, ID3v2.3 tags for broad player compatibility. Copy the resulting `.mp3` files to a USB stick for a car stereo that supports USB MP3 playback. USB capacity, filesystem and filename restrictions depend on the car's manual. The Windows app itself does not run on a car stereo.

**WebM:** original WebM audio without conversion. Use MP3 for devices that do not support WebM.

The executables are unsigned. This is a Windows x64 build.

## Build from source

In PowerShell on Windows 10/11:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup-binaries.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-native.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-native.ps1
```

The app and bundled tools are in `native-dist/`. No npm install, Rust toolchain or Visual Studio installation is needed; the build uses the .NET Framework compiler included with Windows. `-SkipTools` recompiles just the app when the tools are already present.

The manual **Build Windows app** GitHub Actions workflow creates a portable ZIP. Run `setup-binaries.ps1` again and rebuild to update the downloader/runtime when YouTube changes.

## Verify

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-native.ps1 -Live
```

Offline tests cover URL validation, Windows process argument quoting (including spaces, quotes, Unicode and trailing slashes), correct format selection, and missing tools. The live suite downloads the Creative Commons Big Buck Bunny test video in both formats, checks the actual MP3 codec/bitrate/sample rate/channels using ffprobe, verifies a failed download, and cancels a running download. Tests require network access and current binaries.

Startup can be measured without leaving a window open:

```powershell
& '.\native-dist\YouTube Audio.exe' --startup-probe "$PWD\startup-ms.txt"
```

The file records elapsed milliseconds from entry into `Main` to the WinForms `Shown` event. This excludes OS process loading and is not a download-speed benchmark.

## Credits and licenses

Fork of [Refloow/YouTube-Music-Download](https://github.com/Refloow/YouTube-Music-Download), originally by Veljko Vuckovic / Refloow. Original copyright and MIT license are retained. The native interface and implementation are maintained in this fork and are not an official upstream release.

The tools have separate licenses: [yt-dlp](https://github.com/yt-dlp/yt-dlp), [Deno](https://github.com/denoland/deno), and [FFmpeg / ffprobe](https://ffmpeg.org/). Windows yt-dlp binaries contain GPL components; see [third-party licenses](https://github.com/yt-dlp/yt-dlp/blob/master/THIRD_PARTY_LICENSES.txt). Bundled FFmpeg GPL builds, source links and build configuration are available from [Gyan](https://www.gyan.dev/ffmpeg/builds/).

Download only content you are authorized to use. Access restrictions, regional restrictions, removed videos or sign-in requirements can still prevent individual downloads.
