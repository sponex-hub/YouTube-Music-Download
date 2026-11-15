using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using YouTubeAudio;

static class Tests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--echo") {
            foreach (var arg in args.Skip(1)) Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(arg)));
            return 0;
        }
        try {
            string root = Path.GetFullPath(args[0]);
            var mp3 = Downloader.BuildArguments("https://www.youtube.com/watch?v=aqz-KE-bpKQ&list=example", "mp3", root, Path.Combine(root, "bin"));
            Check(mp3.Contains("--no-playlist") && mp3.Contains("--ignore-config"), "single video and isolated config");
            Check(mp3[mp3.IndexOf("--audio-format")+1] == "mp3", "MP3 conversion");
            Check(mp3.Contains("192K") && mp3.Contains("ExtractAudio+ffmpeg_o:-ar 44100 -ac 2 -id3v2_version 3"), "MP3 car playback export settings");
            Check(mp3[mp3.IndexOf("-f")+1].StartsWith("bestaudio[ext=webm]/bestaudio[ext=m4a]"), "prefer compatible audio before MP3 conversion");
            Check(mp3.Any(a => a.Contains("%(ext)s")) && !mp3.Contains("--extractor-args"), "real extensions and default YouTube clients");
            var webm = Downloader.BuildArguments("https://music.youtube.com/watch?v=aqz-KE-bpKQ", "webm", root, Path.Combine(root, "bin"));
            Check(webm.Contains("bestaudio[ext=webm]") && !webm.Contains("--audio-format"), "WebM without unsupported conversion flag");
            foreach (string invalid in new [] { "--exec=evil", "file:///C:/test", "https://youtube.com.evil.example/watch", "https://example.com" }) {
                bool rejected = false;
                try { Downloader.BuildArguments(invalid, "mp3", root, root); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "reject " + invalid);
            }
            string[] tricky = { "", "spaces in path", "C:\\folder\\", "a\"b", "slash\\\"quote", "%title% & song", "Muzică 日本語" };
            using (var echo = Process.Start(new ProcessStartInfo {
                FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
                Arguments = "--echo " + String.Join(" ", tricky.Select(Downloader.Quote)),
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true
            })) {
                var actual = echo.StandardOutput.ReadToEnd().Replace("\r", "").TrimEnd('\n').Split('\n'); echo.WaitForExit();
                Check(actual.SequenceEqual(tricky.Select(t => Convert.ToBase64String(Encoding.UTF8.GetBytes(t)))), "Windows argument quoting round trip");
            }
            bool missing = false;
            try { Downloader.RunAsync("https://youtu.be/aqz-KE-bpKQ", "mp3", root, Path.Combine(root, "missing-tools"), null, CancellationToken.None).GetAwaiter().GetResult(); }
            catch (FileNotFoundException) { missing = true; }
            Check(missing, "actionable missing-tool error");
            if (args.Length > 1 && args[1] == "--live") {
                string output = Path.Combine(root, "native-live-tests", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(output);
                foreach (string format in new [] { "webm", "mp3" }) {
                    string file = Downloader.RunAsync("https://www.youtube.com/watch?v=aqz-KE-bpKQ", format, output,
                        Path.Combine(root, "bin"), null, CancellationToken.None).GetAwaiter().GetResult();
                    Check(File.Exists(file) && new FileInfo(file).Length > 1000 && Path.GetExtension(file) == "." + format, "live " + format + " download");
                    Console.WriteLine(file);
                    if (format == "mp3") {
                        using (var probe = Process.Start(new ProcessStartInfo {
                            FileName = Path.Combine(root, "bin", "ffprobe.exe"),
                            Arguments = "-v error -show_entries stream=codec_name,sample_rate,channels,bit_rate -of default=nw=1 " + Downloader.Quote(file),
                            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true
                        })) {
                            string metadata = probe.StandardOutput.ReadToEnd(); probe.WaitForExit();
                            Check(probe.ExitCode == 0 && metadata.Contains("codec_name=mp3") && metadata.Contains("sample_rate=44100") &&
                                metadata.Contains("channels=2") && metadata.Contains("bit_rate=192000"), "real MP3 is 192 kbps, 44.1 kHz, stereo");
                        }
                    }
                }
                bool failed = false;
                try { Downloader.RunAsync("https://youtu.be/BaW_jenozKc", "webm", output, Path.Combine(root, "bin"), null, CancellationToken.None).GetAwaiter().GetResult(); }
                catch (InvalidOperationException ex) { failed = ex.Message.Contains("ERROR:"); }
                Check(failed, "unavailable video fails with actual yt-dlp error");
                using (var cts = new CancellationTokenSource()) {
                    var task = Downloader.RunAsync("https://www.youtube.com/watch?v=aqz-KE-bpKQ", "mp3", output, Path.Combine(root, "bin"), null, cts.Token);
                    Thread.Sleep(1000); cts.Cancel();
                    bool canceled = false;
                    try { task.GetAwaiter().GetResult(); } catch (OperationCanceledException) { canceled = true; }
                    Check(canceled, "cancel running download");
                }
            }
            return 0;
        } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
