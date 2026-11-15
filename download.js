const path = require('node:path');
const fs = require('node:fs');
const { spawn } = require('node:child_process');

function buildArgs({ url, format, folder, bin }) {
  const parsed = new URL(url);
  if (!['https:', 'http:'].includes(parsed.protocol) ||
      !/^(www\.|music\.|m\.)?(youtube\.com|youtu\.be)$/.test(parsed.hostname)) {
    throw new Error('Please enter a YouTube video URL.');
  }
  if (!['mp3', 'webm'].includes(format)) throw new Error('Unsupported audio format.');
  if (!folder || !fs.statSync(folder).isDirectory()) throw new Error('Select an existing download folder.');
  const args = [
    '--ignore-config', '--no-playlist', '--newline', '--windows-filenames',
    '--js-runtimes', `deno:${path.join(bin, 'deno.exe')}`,
    '--ffmpeg-location', bin,
    '-f', format === 'webm' ? 'bestaudio[ext=webm]' : 'bestaudio/best',
    '-o', path.join(folder, '%(title).180B [%(id)s].%(ext)s'),
    ...(format === 'mp3' ? ['--extract-audio', '--audio-format', 'mp3'] : []),
    '--progress-template', 'download:PROGRESS:%(progress._percent_str)s',
    '--', parsed.href,
  ];
  return args;
}

function downloadAudio(options, notify, launch = spawn) {
  return new Promise((resolve, reject) => {
    const args = buildArgs(options);
    for (const name of ['yt-dlp.exe', 'deno.exe', 'ffmpeg.exe', 'ffprobe.exe']) {
      if (!fs.existsSync(path.join(options.bin, name))) throw new Error(`Missing ${name}. Reinstall the app or run npm run setup:binaries.`);
    }
    const child = launch(path.join(options.bin, 'yt-dlp.exe'), args, { windowsHide: true, shell: false });
    let stderr = '';
    let pending = '';
    child.stderr.on('data', data => { stderr = (stderr + data).slice(-12000); });
    child.stdout.on('data', data => {
      pending += data;
      const lines = pending.split(/[\r\n]+/);
      pending = lines.pop();
      for (const line of lines) {
        const match = line.match(/PROGRESS:\s*([\d.]+)%/);
        if (match) notify(Math.min(100, Number(match[1])));
      }
    });
    child.once('error', reject);
    child.once('close', code => {
      if (code === 0) resolve();
      else reject(new Error(stderr.trim() || `Downloader exited with code ${code}.`));
    });
  });
}

module.exports = { buildArgs, downloadAudio };
