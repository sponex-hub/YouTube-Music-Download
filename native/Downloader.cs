using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace YouTubeAudio
{
    public static class Downloader
    {
        public static string Quote(string value)
        {
            // Windows CommandLineToArgvW rules, including quotes and trailing slashes.
            return "\"" + Regex.Replace(value, "(\\\\*)\"", "$1$1\\\"")
                .ReplaceTrailingSlashes() + "\"";
        }

        private static string ReplaceTrailingSlashes(this string value)
        {
            return Regex.Replace(value, "(\\\\+)$", "$1$1");
        }

        public static List<string> BuildArguments(string url, string format, string folder, string bin)
        {
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) ||
                (parsed.Scheme != "https" && parsed.Scheme != "http") ||
                !Regex.IsMatch(parsed.Host, @"^(www\.|music\.|m\.)?(youtube\.com|youtu\.be)$", RegexOptions.IgnoreCase))
                throw new ArgumentException("Introdu un link YouTube valid.");
            if (format != "mp3" && format != "webm") throw new ArgumentException("Format audio invalid.");
            if (!Directory.Exists(folder)) throw new ArgumentException("Alege un folder de descarcare existent.");
            var args = new List<string> {
                "--ignore-config", "--no-playlist", "--newline", "--windows-filenames",
                "--js-runtimes", "deno:" + Path.Combine(bin, "deno.exe"),
                "--ffmpeg-location", bin,
                "-f", format == "webm" ? "bestaudio[ext=webm]" : "bestaudio[ext=webm]/bestaudio[ext=m4a]/bestaudio/best",
                "-o", Path.Combine(folder, "%(title).180B [%(id)s].%(ext)s"),
                "--progress-template", "download:PROGRESS:%(progress._percent_str)s",
                "--print", "after_move:FILE:%(filepath)s"
            };
            if (format == "mp3") args.AddRange(new [] {
                "--extract-audio", "--audio-format", "mp3", "--audio-quality", "192K",
                "--postprocessor-args", "ExtractAudio+ffmpeg_o:-ar 44100 -ac 2 -id3v2_version 3"
            });
            args.Add("--"); args.Add(parsed.AbsoluteUri);
            return args;
        }

        public static Task<string> RunAsync(string url, string format, string folder, string bin,
            Action<double> progress, CancellationToken token)
        {
            return Task.Run(() => {
                var args = BuildArguments(url, format, folder, bin);
                foreach (string file in new [] { "yt-dlp.exe", "deno.exe", "ffmpeg.exe", "ffprobe.exe" })
                    if (!File.Exists(Path.Combine(bin, file)))
                        throw new FileNotFoundException("Lipseste " + file + ". Extrage tot continutul arhivei, inclusiv folderul tools.");
                token.ThrowIfCancellationRequested();
                var errors = new StringBuilder();
                string outputFile = null;
                var quoted = args.ConvertAll(Quote);
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo {
                        FileName = Path.Combine(bin, "yt-dlp.exe"),
                        Arguments = String.Join(" ", quoted),
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardOutput = true, RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
                    };
                    process.OutputDataReceived += (sender, e) => {
                        if (e.Data == null) return;
                        if (e.Data.StartsWith("FILE:")) outputFile = e.Data.Substring(5);
                        var match = Regex.Match(e.Data, @"PROGRESS:\s*([\d.]+)%");
                        double percent;
                        if (match.Success && Double.TryParse(match.Groups[1].Value, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out percent) && progress != null)
                            progress(Math.Max(0, Math.Min(100, percent)));
                    };
                    process.ErrorDataReceived += (sender, e) => {
                        if (e.Data == null) return;
                        lock (errors) {
                            errors.AppendLine(e.Data);
                            if (errors.Length > 16000) errors.Remove(0, errors.Length - 16000);
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine(); process.BeginErrorReadLine();
                    using (token.Register(() => {
                        try {
                            if (!process.HasExited) {
                                // Kill the whole tree, including any running FFmpeg conversion.
                                using (var killer = Process.Start(new ProcessStartInfo {
                                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe"),
                                    Arguments = "/PID " + process.Id + " /T /F", UseShellExecute = false,
                                    CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                                })) { killer.WaitForExit(); }
                            }
                        } catch (InvalidOperationException) { }
                    })) {
                        process.WaitForExit();
                        token.ThrowIfCancellationRequested();
                        if (process.ExitCode != 0) throw new InvalidOperationException(
                            errors.Length > 0 ? errors.ToString().Trim() : "Descarcarea a esuat (cod " + process.ExitCode + ").");
                        if (String.IsNullOrWhiteSpace(outputFile) || !File.Exists(outputFile))
                            throw new IOException("Descarcarea s-a oprit fara un fisier audio final.");
                        return outputFile;
                    }
                }
            });
        }
    }
}
