# Version 2.0 — native Windows UI

- Replaced Electron/JavaScript with C# Windows Forms on .NET Framework 4.8.
- Removed Chromium, npm dependencies and portable EXE extraction at startup.
- Added a restrained native desktop interface in Romanian with MP3/WebM selection, paste, remembered folder, progress, actual error details and open-folder action.
- Added cancellation of the downloader process tree, including FFmpeg. Closing during a download cancels before exiting.
- Kept current yt-dlp, Deno and FFmpeg tools. They start only when downloading, not when opening the UI.
- Prefer WebM or M4A audio before MP3 conversion, avoiding selection of less compatible new audio codecs when ordinary audio streams are available.
- MP3 output is 192 kbps CBR, 44.1 kHz stereo with ID3v2.3 tags for broad device compatibility. Actual car/USB support depends on the stereo.
- Keep the single-video restriction, real file extensions, default YouTube clients and success-only completion.
- Added native regression tests and live MP3/WebM, ffprobe, unavailable-video and cancellation checks.
- Portable delivery is now a ZIP: extract everything once, then launch `YouTube Audio.exe` alongside its `tools` directory.

## Startup observations

Local Windows measurements before final packaging showed 38–42 ms from `Main` to the first `Shown` event. External measurement including process launch, writing the measurement file and exiting was approximately 0.64 seconds on the first measured run and 0.09 seconds on subsequent runs. These are machine-specific observations, not a universal performance promise or a direct Electron comparison.

The UI executable is approximately 165 KB; downloader/runtime/conversion binaries still make up most of the portable archive. They are not loaded at UI startup.

See README.md for build, test, update and third-party license instructions.
