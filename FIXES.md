# Download repair (1.1.1)

- Bundle current yt-dlp and Deno; official yt-dlp executables include the EJS solver required for current YouTube challenges.
- Use default YouTube player selection instead of hard-coded extractor overrides.
- Download once, with title, video ID and real extension supplied by yt-dlp.
- Select actual WebM audio for WebM; use FFmpeg extraction/conversion only for MP3.
- Include ffprobe alongside FFmpeg in packaged apps.
- Show completion only for a successful subprocess exit; expose actual errors.
- Validate URLs and formats, ignore external yt-dlp config, and disable playlists.
- Prevent concurrent downloads in the UI and main process.

## Run or rebuild on Windows

Install Node.js 22 or later, then run:

```powershell
npm ci
npm run setup:binaries
npm test
npm start
npm run dist -- --config.win.signAndEditExecutable=false
```

`setup:binaries` downloads current official yt-dlp and Deno builds and FFmpeg/ffprobe from Gyan's FFmpeg builds. Run it again and rebuild when YouTube changes break an older downloader. Close any running app before updating tools.

The manual **Build Windows app** GitHub Actions workflow produces downloadable executables. The upstream GitLab mirror workflow only runs in the original repository.

Windows executables are unsigned. No signing certificate was provided.

## Verification

Regression tests cover URL validation, format selection, filename templates, split progress messages, and subprocess failures. Live download validation uses the Creative Commons Big Buck Bunny video at https://www.youtube.com/watch?v=aqz-KE-bpKQ . Network restrictions, authentication requirements, removed videos, and regional restrictions can still cause individual videos to fail; the application displays yt-dlp's error.

Upstream documentation: https://github.com/yt-dlp/yt-dlp/wiki/EJS

## Third-party software

The packaged app bundles Electron, yt-dlp, Deno, FFmpeg and ffprobe. Their licenses apply independently of this repository's MIT license. FFmpeg builds used here are GPL builds; corresponding FFmpeg source and build information are available at https://www.gyan.dev/ffmpeg/builds/ and https://ffmpeg.org/download.html . yt-dlp Windows binaries include GPL-licensed components; see https://github.com/yt-dlp/yt-dlp/blob/master/THIRD_PARTY_LICENSES.txt .
