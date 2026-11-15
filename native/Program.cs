using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("YouTube Audio")]
[assembly: System.Reflection.AssemblyDescription("Native Windows audio downloader")]
[assembly: System.Reflection.AssemblyVersion("2.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")]

namespace YouTubeAudio
{
    static class Program
    {
        public static Stopwatch Startup;
        [STAThread]
        static void Main(string[] args)
        {
            Startup = Stopwatch.StartNew();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainWindow()) {
                if (args.Length == 2 && args[0] == "--startup-probe") {
                    form.Shown += (s, e) => {
                        File.WriteAllText(args[1], Startup.ElapsedMilliseconds.ToString());
                        form.BeginInvoke(new Action(form.Close));
                    };
                }
                Application.Run(form);
            }
        }
    }

    public sealed class MainWindow : Form
    {
        private readonly Color ink = Color.FromArgb(30, 34, 40);
        private readonly Color muted = Color.FromArgb(105, 111, 119);
        private readonly Color blue = Color.FromArgb(37, 82, 157);
        private TextBox url;
        private Label folderLabel, status, detail, percent;
        private Button mp3, webm, download, cancel, choose, paste, open;
        private ProgressBar progress;
        private TextBox log;
        private string format = "mp3", folder;
        private CancellationTokenSource cancellation;
        private bool busy, pendingClose;
        private readonly string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YouTubeAudio", "folder.txt");

        public MainWindow()
        {
            Text = "YouTube Audio";
            ClientSize = new Size(660, 610);
            MinimumSize = MaximumSize = SizeFromClientSize(ClientSize);
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.FromArgb(246, 246, 244);
            ForeColor = ink;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            AddLabel("YouTube Audio", 32, 28, 450, 40, 23, FontStyle.Bold, ink);
            AddLabel("Un link. Un fișier audio.", 34, 77, 500, 24, 10, FontStyle.Regular, muted);
            var divider = new Panel { Bounds = new Rectangle(34, 120, 592, 1), BackColor = Color.FromArgb(220, 222, 222) };
            Controls.Add(divider);

            AddLabel("LINK YOUTUBE", 34, 145, 400, 20, 9, FontStyle.Bold, muted);
            var inputBorder = new Panel { Bounds = new Rectangle(34, 174, 482, 44), Padding = new Padding(1), BackColor = Color.FromArgb(190, 195, 201) };
            var inputWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            url = new TextBox { BorderStyle = BorderStyle.None, Bounds = new Rectangle(11, 10, 456, 25), Font = new Font("Segoe UI", 11F), AccessibleName = "Link YouTube" };
            inputWhite.Controls.Add(url); inputBorder.Controls.Add(inputWhite); Controls.Add(inputBorder);
            paste = AddButton("Lipește", 526, 174, 100, 44, false);
            paste.Click += (s, e) => { try { if (Clipboard.ContainsText()) url.Text = Clipboard.GetText().Trim(); } catch (Exception ex) { ShowError(ex.Message); } };
            AddLabel("youtube.com sau music.youtube.com · un singur clip", 34, 227, 590, 22, 9, FontStyle.Regular, muted);

            AddLabel("FORMAT", 34, 272, 400, 20, 9, FontStyle.Bold, muted);
            mp3 = AddButton("MP3", 34, 301, 86, 39, true);
            webm = AddButton("WebM", 126, 301, 86, 39, false);
            mp3.Click += (s, e) => SelectFormat("mp3");
            webm.Click += (s, e) => SelectFormat("webm");
            detail = AddLabel("192 kbps · stereo · pentru telefon și mașină", 230, 309, 396, 28, 10, FontStyle.Regular, muted);

            AddLabel("SALVEAZĂ ÎN", 34, 369, 400, 20, 9, FontStyle.Bold, muted);
            folderLabel = AddLabel("", 34, 401, 474, 28, 10, FontStyle.Regular, ink);
            folderLabel.AutoEllipsis = true;
            choose = AddButton("Schimbă…", 526, 394, 100, 38, false);
            choose.Click += ChooseFolder;

            status = AddLabel("Pregătit de descărcare", 34, 462, 490, 25, 10, FontStyle.Regular, muted);
            percent = AddLabel("", 548, 462, 78, 25, 10, FontStyle.Regular, muted);
            percent.TextAlign = ContentAlignment.MiddleRight;
            progress = new ProgressBar { Bounds = new Rectangle(34, 494, 592, 5), Maximum = 1000, Style = ProgressBarStyle.Continuous };
            Controls.Add(progress);
            download = AddButton("Descarcă audio", 446, 527, 180, 46, true);
            download.Click += async (s, e) => await StartDownload();
            cancel = AddButton("Anulează", 332, 527, 104, 46, false); cancel.Visible = false;
            cancel.Click += async (s, e) => { cancel.Enabled = false; status.Text = "Se anulează…"; await RequestCancel(); };
            open = AddButton("Deschide folderul", 34, 527, 169, 46, false);
            open.Click += (s, e) => { try { Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true }); } catch (Exception ex) { ShowError(ex.Message); } };
            log = new TextBox { Bounds = new Rectangle(34, 615, 592, 105), Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Font = new Font("Consolas", 9F), Visible = false, AccessibleName = "Detalii eroare" };
            Controls.Add(log);
            AcceptButton = download;

            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            try { if (File.Exists(settings) && Directory.Exists(File.ReadAllText(settings).Trim())) folder = File.ReadAllText(settings).Trim(); } catch { }
            if (!Directory.Exists(folder)) folder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            folderLabel.Text = folder;
            Shown += (s, e) => url.Focus();
            FormClosing += OnClosing;
        }

        private Label AddLabel(string text, int x, int y, int w, int h, float size, FontStyle style, Color color)
        {
            var label = new Label { Text = text, Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", size, style), ForeColor = color, UseMnemonic = false };
            Controls.Add(label); return label;
        }

        private Button AddButton(string text, int x, int y, int w, int h, bool primary)
        {
            var button = new Button { Text = text, Bounds = new Rectangle(x, y, w, h), FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false, TabStop = true };
            StyleButton(button, primary); Controls.Add(button); return button;
        }

        private void StyleButton(Button button, bool primary)
        {
            button.BackColor = primary ? blue : BackColor;
            button.ForeColor = primary ? Color.White : ink;
            button.FlatAppearance.BorderColor = primary ? blue : Color.FromArgb(196, 200, 204);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(28, 65, 127) : Color.FromArgb(232, 234, 234);
        }

        private void SelectFormat(string selected)
        {
            format = selected;
            StyleButton(mp3, selected == "mp3"); StyleButton(webm, selected == "webm");
            mp3.AccessibleDescription = selected == "mp3" ? "Selectat" : "";
            webm.AccessibleDescription = selected == "webm" ? "Selectat" : "";
            detail.Text = selected == "mp3" ? "192 kbps · stereo · pentru telefon și mașină" : "Audio original, fără conversie.";
        }

        private void ChooseFolder(object sender, EventArgs e)
        {
            using (var picker = new FolderBrowserDialog { Description = "Alege folderul pentru fișierele audio", SelectedPath = folder }) {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                folder = picker.SelectedPath; folderLabel.Text = folder;
                try { Directory.CreateDirectory(Path.GetDirectoryName(settings)); File.WriteAllText(settings, folder); }
                catch (Exception ex) { ShowError("Folderul este ales, dar preferința nu a putut fi salvată. " + ex.Message); }
            }
        }

        private void SetBusy(bool value)
        {
            busy = value;
            download.Enabled = choose.Enabled = paste.Enabled = mp3.Enabled = webm.Enabled = url.Enabled = !value;
            cancel.Visible = value; cancel.Enabled = value;
            download.Text = value ? "Se descarcă…" : "Descarcă audio";
        }

        private void ResizeForError(bool visible)
        {
            MinimumSize = Size.Empty; MaximumSize = Size.Empty;
            float scale;
            using (var graphics = CreateGraphics()) scale = graphics.DpiX / 96F;
            ClientSize = new Size((int)(660 * scale), (int)((visible ? 744 : 610) * scale));
            MinimumSize = MaximumSize = Size;
            log.Visible = visible;
        }

        private void ShowError(string message)
        {
            status.ForeColor = Color.FromArgb(164, 45, 39);
            status.Text = "Nu am putut finaliza. Vezi detaliile de mai jos.";
            progress.Style = ProgressBarStyle.Continuous; percent.Text = "";
            log.Text = message; ResizeForError(true);
        }

        private async Task StartDownload()
        {
            if (busy) return;
            ResizeForError(false);
            string link = url.Text.Trim();
            try { Downloader.BuildArguments(link, format, folder, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools")); }
            catch (Exception ex) { ShowError(ex.Message); return; }
            cancellation = new CancellationTokenSource();
            SetBusy(true); progress.Value = 0;
            progress.Style = ProgressBarStyle.Marquee;
            percent.Text = ""; status.ForeColor = muted; status.Text = "Se pregătește descărcarea…";
            try {
                string file = await Downloader.RunAsync(link, format, folder, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools"),
                    value => {
                        if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => {
                            progress.Style = ProgressBarStyle.Continuous;
                            progress.Value = (int)(value * 10);
                            percent.Text = value.ToString("0") + "%";
                            status.Text = value >= 100 && format == "mp3" ? "Se convertește în MP3…" : "Se descarcă audio…";
                        }));
                    }, cancellation.Token);
                progress.Style = ProgressBarStyle.Continuous; progress.Value = 1000; percent.Text = "100%";
                status.ForeColor = Color.FromArgb(37, 111, 72); status.Text = "Salvat. Fișierul audio este în folderul ales.";
                new ToolTip().SetToolTip(status, Path.GetFileName(file));
            } catch (OperationCanceledException) {
                progress.Style = ProgressBarStyle.Continuous; progress.Value = 0; percent.Text = "";
                status.Text = "Descărcare anulată.";
            } catch (Exception ex) { ShowError(ex.Message); }
            finally {
                SetBusy(false); cancellation.Dispose(); cancellation = null;
                if (pendingClose) Close();
            }
        }

        private async void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (!busy) return;
            e.Cancel = true;
            if (pendingClose) return;
            pendingClose = true; cancel.Enabled = false;
            await RequestCancel();
        }

        private Task RequestCancel()
        {
            var current = cancellation;
            return Task.Run(() => {
                try { if (current != null) current.Cancel(); }
                catch (ObjectDisposedException) { }
            });
        }
    }
}
