using HartUI.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using WinHubX.Forms.Personalizzazione_office;
using WinHubX.Impostazioni;

namespace WinHubX
{
    public partial class FormOffice : Form
    {
        private readonly Form1 form1;
        private readonly NotifyIcon notifyIcon;
        private List<OfficeVersion> officeVersions = new();
        private string selectedOfficeVersion = string.Empty;
        private string selectedLanguage = string.Empty;
        private string selectedInstallationType = string.Empty;
        private string percorsoCompleto = string.Empty;
        private CancellationTokenSource? _cts;
        private static readonly HttpClient ResourceClient = CreateResourceClient();
        private readonly Action<int> _downloadProgressHandler;
        private readonly Action<bool> _downloadStateHandler;

        private static HttpClient CreateResourceClient()
        {
            var handler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 4,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(2) };
        }

        public FormOffice(Form1 form1)
        {
            InitializeComponent();
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = false
            };
            this.form1 = form1;
            _downloadProgressHandler = HandleDownloadProgressChanged;
            _downloadStateHandler = HandleDownloadStateChanged;
            FormClosed += FormOffice_FormClosed;

            ThemeManager.ApplyThemeToControl(this, ThemeManager.IsDarkTheme);
            string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            labelpercorso.Text = $"{downloadPath}";
            toolTip1.SetToolTip(labelpercorso, downloadPath);
            percorsoCompleto = downloadPath;
            AggiornaPercorsoLabel(downloadPath);

            WinHubX.Impostazioni.DownloadManager.ProgressChanged += _downloadProgressHandler;
            WinHubX.Impostazioni.DownloadManager.DownloadStateChanged += _downloadStateHandler;
            SetDownloadButtonStyle(WinHubX.Impostazioni.DownloadManager.IsDownloading);
            if (WinHubX.Impostazioni.DownloadManager.IsDownloading)
            {
                UpdateProgress(WinHubX.Impostazioni.DownloadManager.ProgressPercentage);
            }

            LanguageManager.LoadLanguageFromSettings();
            btnAttivaOfficePrinci.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => "  Attiva Office",
                "en" => "  Activate Office",
                _ => btnAttivaOfficePrinci.Content
            };
            btnScrubberPrinci.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => "  Disinstalla Office",
                "en" => "  Scrubber Office",
                _ => btnScrubberPrinci.Content
            };
            btnPersonalizzaOfficePrinci.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => " Crea versione personalizzata",
                "en" => " Create custom version",
                _ => btnPersonalizzaOfficePrinci.Content
            };
            btnAggRimAppOfficePrinci.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => " Aggiungi/Rimuovi app",
                "en" => "Add/Remove apps",
                _ => btnAggRimAppOfficePrinci.Content
            };
        }

        private void HandleDownloadProgressChanged(int progress)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => UpdateProgress(progress)));
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"Aggiornamento progresso Office ignorato dopo la chiusura: {ex.Message}");
                }
                return;
            }

            UpdateProgress(progress);
        }

        private void HandleDownloadStateChanged(bool isDownloading)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => SetDownloadButtonStyle(isDownloading)));
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"Aggiornamento stato Office ignorato dopo la chiusura: {ex.Message}");
                }
                return;
            }

            SetDownloadButtonStyle(isDownloading);
        }

        private void FormOffice_FormClosed(object? sender, FormClosedEventArgs e)
        {
            WinHubX.Impostazioni.DownloadManager.ProgressChanged -= _downloadProgressHandler;
            WinHubX.Impostazioni.DownloadManager.DownloadStateChanged -= _downloadStateHandler;
            notifyIcon.Dispose();
        }
        private void UpdateProgress(int progress)
        {
            progressBar1.Visible = true;
            progressBar1.Value = Math.Min(progress, 100);
            label2.Visible = true;
            label2.Text = $"{progress}%";
        }
        #region AttivaOffice
        private void btnAttivaOffice_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://account.microsoft.com/services") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossibile aprire la gestione ufficiale dell’abbonamento Office: {ex.Message}",
                    "WinHubX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task<bool> IsInternetAvailableAsync()
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using HttpResponseMessage result = await ResourceClient.GetAsync(
                    "https://www.google.com", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                return result.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        private async Task<string> OttieniURL(string jsonUrl)
        {
            var response = await ResourceClient.GetStringAsync(jsonUrl);
            var json = JObject.Parse(response);
            string url = json["FormOffice"]?["scrubber"]?.Value<string>()
                ?? throw new InvalidOperationException("URL scrubber non presente nella configurazione.");
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("URL scrubber non valido: è richiesto HTTPS.");
            return uri.ToString();
        }

        private async void btnScrubber_Click(object? sender, EventArgs e)
        {
            string? tempFolder = null;
            try
            {
                string zipFileUrl = string.Empty;

                if (await IsInternetAvailableAsync())
                {
                    zipFileUrl = await OttieniURL(Dipendenze.GitHubConfigUrl);

                    if (string.IsNullOrEmpty(zipFileUrl))
                        throw new Exception(LanguageManager.GetTranslation("FormOffice", "url_non_trovato_github"));
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.GetTranslation("Global", "nointernet"),
                        LanguageManager.GetTranslation("FormOffice", "errore"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
                tempFolder = Path.Combine(Path.GetTempPath(), $"WinHubX-OfficeScrubber-{Guid.NewGuid():N}");
                string tempZipPath = Path.Combine(tempFolder, "OfficeScrubber.zip");

                Directory.CreateDirectory(tempFolder);
                await DownloadManager.DownloadFileAsync(zipFileUrl, tempZipPath,
                    _cts?.Token ?? CancellationToken.None, autoParallel: false);
                ExtractZipSafely(tempZipPath, tempFolder);
                string cmdPath = Path.Combine(tempFolder, "OfficeScrubber.cmd");

                if (!File.Exists(cmdPath))
                {
                    MessageBox.Show(
                        LanguageManager.GetTranslation("FormOffice", "file_non_trovato"),
                        LanguageManager.GetTranslation("FormOffice", "errore"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                using Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                        WorkingDirectory = tempFolder,
                        Verb = "runas",
                        UseShellExecute = true
                    }
                };
                process.StartInfo.ArgumentList.Add("/d");
                process.StartInfo.ArgumentList.Add("/c");
                process.StartInfo.ArgumentList.Add(cmdPath);

                process.Start();
                await process.WaitForExitAsync(_cts?.Token ?? CancellationToken.None);
                await Task.Run(() => AttendiScrubberConTitolo("Office Scrubber v12"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LanguageManager.GetTranslation("FormOffice", "errore_generico_scrubber")} {ex.Message}",
                    LanguageManager.GetTranslation("FormOffice", "errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                if (tempFolder is not null)
                {
                    try
                    {
                        if (Directory.Exists(tempFolder))
                            Directory.Delete(tempFolder, recursive: true);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        private static void ExtractZipSafely(string archivePath, string destination)
        {
            Directory.CreateDirectory(destination);
            string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Archivio Office Scrubber non valido: percorso ZIP non sicuro.");

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(target);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
            }
        }

        private async Task AttendiScrubberConTitolo(string titolo, int timeoutMs = 10 * 60 * 1000)
        {
            int waited = 0;
            Process? scrubberProc = null;

            while (waited < timeoutMs)
            {
                scrubberProc = Process.GetProcessesByName("powershell")
                    .FirstOrDefault(p => p.MainWindowTitle.Contains(titolo));

                if (scrubberProc != null)
                    break;

                await Task.Delay(1000);
                waited += 1000;
            }

            scrubberProc?.WaitForExit();
        }

        private void PictureBox3_Click_BackToOffice(object? sender, EventArgs e)
        {
            Form1? mainForm = Application.OpenForms["Form1"] as Form1;
            if (mainForm == null) return;

            mainForm.pictureBox3.Visible = false;
            mainForm.LoadForm(new FormOffice(mainForm), mainForm.btnOffice, "Office");
        }

        private async Task<List<OfficeVersion>> CaricaOfficeVersions(string jsonUrl)
        {
            List<OfficeVersion> officeVersions = new List<OfficeVersion>();

            {
                string jsonResponse = await ResourceClient.GetStringAsync(jsonUrl);
                var jsonObject = JObject.Parse(jsonResponse);
                foreach (var prop in jsonObject.Properties().Where(p => p.Name.StartsWith("Office")))
                {
                    string nomeOffice = prop.Name;
                    nomeOffice = Regex.Replace(nomeOffice, @"^Office(\d+)$", "Office $1");

                    var office = new OfficeVersion
                    {
                        Nome = nomeOffice,
                        Lingue = new Dictionary<string, Dictionary<string, string>>()
                    };

                    var officeObj = (JObject)prop.Value;

                    foreach (var lang in officeObj.Properties())
                    {
                        var links = new Dictionary<string, string>();

                        foreach (var kvp in (JObject)lang.Value)
                        {
                            string chiave = kvp.Key;
                            string? url = kvp.Value?.ToString();
                            if (string.IsNullOrWhiteSpace(url))
                                continue;
                            if (chiave.Equals("Officex64", StringComparison.OrdinalIgnoreCase) ||
                                chiave.Equals("Officex32", StringComparison.OrdinalIgnoreCase))
                                chiave = "Online";
                            else if (chiave.StartsWith("Offline", StringComparison.OrdinalIgnoreCase))
                                chiave = "Offline";
                            else if (chiave.Equals("officehash", StringComparison.OrdinalIgnoreCase))
                                continue;

                            links[chiave] = url;
                        }

                        office.Lingue.Add(lang.Name, links);
                    }

                    officeVersions.Add(office);
                }
            }

            return officeVersions;
        }


        private void comboBoxVerOffice_SelectedIndexChanged(object? sender, EventArgs e)
        {
            selectedOfficeVersion = comboBoxVerOffice.SelectedItem?.ToString() ?? string.Empty;

            comboBox_Lingua.Items.Clear();
            if (!string.IsNullOrEmpty(selectedOfficeVersion))
            {
                var office = officeVersions.FirstOrDefault(o => o.Nome == selectedOfficeVersion);
                if (office != null)
                {
                    comboBox_Lingua.Items.AddRange(office.Lingue.Keys.ToArray());
                }
            }
        }


        private void comboBox_Lingua_SelectedIndexChanged(object? sender, EventArgs e)
        {
            selectedLanguage = comboBox_Lingua.SelectedItem?.ToString() ?? string.Empty;

            comboBoxInstallazione.Items.Clear();
            if (!string.IsNullOrEmpty(selectedOfficeVersion) && !string.IsNullOrEmpty(selectedLanguage))
            {
                var office = officeVersions.First(o => o.Nome == selectedOfficeVersion);
                if (office.Lingue.TryGetValue(selectedLanguage, out var options))
                {
                    comboBoxInstallazione.Items.AddRange(options.Keys.ToArray());
                }
            }
        }
        private void comboBoxInstallazione_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            string selezione = comboBoxInstallazione.SelectedItem?.ToString() ?? "";
            selectedInstallationType = selezione;
            selectedOfficeVersion = comboBoxVerOffice.SelectedItem?.ToString() ?? string.Empty;
            labelavviso.Visible = selezione.Equals("Online", StringComparison.OrdinalIgnoreCase);
            Checkbox_Salva.Visible = selezione.Equals("Offline", StringComparison.OrdinalIgnoreCase);
            Checkbox_Installa.Visible = selezione.Equals("Offline", StringComparison.OrdinalIgnoreCase);
            labelpercorso.Visible = selezione.Equals("Offline", StringComparison.OrdinalIgnoreCase);
            label1.Visible = selezione.Equals("Offline", StringComparison.OrdinalIgnoreCase);
            btn_cambiaBianco.Visible = selezione.Equals("Offline", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHardwareArchitecture()
        {
            try
            {
                string hwPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WinHubX", "Computer", "osehardware.json");

                if (File.Exists(hwPath))
                {
                    var json = File.ReadAllText(hwPath);
                    var hwInfo = JsonConvert.DeserializeObject<WinHubX.Impostazioni.HardwareInfo>(json);

                    string? arch = hwInfo?.Architettura?.Trim()?.ToLowerInvariant();
                    if (arch != null)
                    {
                        if (arch.Contains("arm64")) return "x64";
                        if (arch.Contains("64")) return "x64";
                        if (arch.Contains("32")) return "x32";
                    }
                }
            }
            catch { }

            return Environment.Is64BitOperatingSystem ? "x64" : "x32";
        }

        private async void btnDownload_Click(object? sender, EventArgs e)
        {
            string hardwarePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinHubX", "Computer", "osehardware.json");

            if (!File.Exists(hardwarePath))
            {
                var popup = new WinHubX.DialogBlock.Form_DialogBlock(form1);
                var panelCenter = panel50.PointToScreen(new Point(panel50.Width / 2, panel50.Height / 2));
                popup.StartPosition = FormStartPosition.Manual;
                popup.Location = new Point(
                    panelCenter.X - popup.Width / 2,
                    panelCenter.Y - popup.Height / 2
                );

                popup.ShowDialog();
                return;
            }

            if (WinHubX.Impostazioni.DownloadManager.IsDownloading)
            {
                _cts?.Cancel();
                WinHubX.Impostazioni.DownloadManager.ForceStopDownload();
                await Task.Delay(1000);
                SetDownloadButtonStyle(false);
                return;
            }

            if (string.IsNullOrEmpty(selectedInstallationType) ||
                string.IsNullOrEmpty(selectedOfficeVersion) ||
                string.IsNullOrEmpty(selectedLanguage))
            {
                MessageBox.Show(LanguageManager.GetTranslation("FormOffice", "seleziona_versione_tipo_installazione"));
                return;
            }

            string? tempFile = null;
            string? savePath = null;
            SetDownloadButtonStyle(true);
            _cts = new CancellationTokenSource();

            try
            {
                _cts.Token.ThrowIfCancellationRequested();

                var office = officeVersions.FirstOrDefault(o => o.Nome == selectedOfficeVersion);
                if (office == null)
                    throw new Exception(LanguageManager.GetTranslation("FormOffice", "versione_non_trovata"));

                if (!office.Lingue.TryGetValue(selectedLanguage, out var links))
                    throw new Exception(LanguageManager.GetTranslation("FormOffice", "lingua_non_trovata"));

                string arch = GetHardwareArchitecture();
                string? url = null;

                if (selectedInstallationType.Equals("Offline", StringComparison.OrdinalIgnoreCase))
                {
                    url = links.GetValueOrDefault("Offline");
                }
                else if (selectedInstallationType.Equals("Online", StringComparison.OrdinalIgnoreCase))
                {
                    if (arch == "x64")
                        url = links.FirstOrDefault(k => k.Key.Contains("x64", StringComparison.OrdinalIgnoreCase)).Value
                              ?? links.GetValueOrDefault("Online");
                    else
                        url = links.FirstOrDefault(k => k.Key.Contains("x32", StringComparison.OrdinalIgnoreCase)).Value
                              ?? links.GetValueOrDefault("Online");
                }

                if (string.IsNullOrEmpty(url))
                    throw new Exception(LanguageManager.GetTranslation("FormOffice", "link_non_trovato"));

                progressBar1.Visible = true;
                progressBar1.Value = 0;
                label2.Visible = true;
                label2.Text = "0%";

                if (selectedInstallationType.Contains("Offline"))
                {
                    bool salva = Checkbox_Salva.Checked;
                    bool installa = Checkbox_Installa.Checked;
                    if (!salva && !installa)
                        throw new Exception("Seleziona almeno una delle opzioni: Salva o Installa.");
                    savePath = Path.Combine(percorsoCompleto, Path.GetFileName(url));
                    await WinHubX.Impostazioni.DownloadManager.DownloadFileAsync(url, savePath, _cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();
                    if (installa)
                    {
                        WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile = savePath;
                        WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = true;
                        WinHubX.Impostazioni.OfficeSettings.InstallationType = "Offline";
                        await StartInstallation(savePath, salva);
                    }
                }

                else
                {
                    string tempDir = Path.Combine(Path.GetTempPath());
                    Directory.CreateDirectory(tempDir);
                    string platform = arch == "x32" ? "x86" : "x64";

                    url = links.FirstOrDefault(k => k.Key.Contains($"Officex{(platform == "x64" ? "64" : "32")}", StringComparison.OrdinalIgnoreCase)).Value
                          ?? links.GetValueOrDefault("Online");

                    if (string.IsNullOrEmpty(url))
                        throw new Exception("Link di download non trovato per questa configurazione.");

                    Uri uri = new Uri(url);
                    string query = uri.Query.TrimStart('?');
                    var parameters = System.Web.HttpUtility.ParseQueryString(query);

                    string product = parameters["ProductreleaseID"] ?? selectedOfficeVersion;
                    string lang = parameters["language"] ?? selectedLanguage;
                    string version = parameters["version"] ?? "O16GA";

                    string cleanFileName = $"OfficeSetup_{product}_{platform}_{lang}_{version}.exe";
                    tempFile = Path.Combine(tempDir, cleanFileName);
                    await WinHubX.Impostazioni.DownloadManager.DownloadFileAsync(url, tempFile, _cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();
                    WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile = tempFile;
                    WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = true;
                    WinHubX.Impostazioni.OfficeSettings.InstallationType = "Online";
                    await StartOnlineInstallation(tempFile);
                }
            }
            catch (OperationCanceledException)
            {
                WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = false;
                WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile = null;
                MessageBox.Show("Download annullato dall'utente.");
            }
            catch (Exception ex)
            {
                WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = false;
                WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile = null;
                MessageBox.Show($"{LanguageManager.GetTranslation("FormOffice", "errore")}:\n{ex.Message}");
            }
            finally
            {
                progressBar1.Visible = false;
                label2.Visible = false;
                SetDownloadButtonStyle(false);
                try
                {
                    if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
                        File.Delete(tempFile);
                    if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath) &&
                        !WinHubX.Impostazioni.DownloadManager.IsDownloading &&
                        selectedInstallationType.Contains("Offline"))
                    {
                        bool salva = Checkbox_Salva.Checked;
                        bool installa = Checkbox_Installa.Checked;
                        if (!salva && !installa)
                        {
                            File.Delete(savePath);
                        }
                    }
                }
                catch { }
            }
        }
        private async Task StartInstallation(string savePath, bool salva)
        {
            try
            {
                if (!await MountIsoAsync(savePath))
                {
                    MessageBox.Show("Errore durante il montaggio dell'immagine ISO.", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string? driveLetter = null;
                for (int i = 0; i < 10; i++)
                {
                    driveLetter = await GetIsoDriveLetterAsync(savePath);
                    if (!string.IsNullOrWhiteSpace(driveLetter)) break;
                    await Task.Delay(1000);
                }

                if (string.IsNullOrWhiteSpace(driveLetter))
                {
                    MessageBox.Show("Impossibile determinare la lettera di unità dell'immagine montata.", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string drivePath = driveLetter + @":\";

                try
                {
                    var possibleSetups = Directory.GetFiles(drivePath, "*.exe", SearchOption.AllDirectories)
                        .Where(f => f.Contains("setup", StringComparison.OrdinalIgnoreCase)
                                 || f.Contains("install", StringComparison.OrdinalIgnoreCase)
                                 || f.Contains("autorun", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (possibleSetups.Count == 0)
                    {
                        MessageBox.Show("Nessun file di installazione trovato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await Task.Delay(2000);
                    string setupExe = possibleSetups.First();
                    await Task.Delay(2000);

                    var proc = Process.Start(new ProcessStartInfo(setupExe)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(setupExe)
                    });

                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                        await Task.Delay(3000);
                        while (Process.GetProcesses().Any(p => p.ProcessName.Contains("setup", StringComparison.OrdinalIgnoreCase)))
                            await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante l'esecuzione del setup:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    await RunPowerShellAsync($"Dismount-DiskImage -ImagePath '{savePath}'");
                }

                if (!salva)
                {
                    try { File.Delete(savePath); } catch { }
                }
                WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = false;
                WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'installazione: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = false;
            }
        }

        private async Task StartOnlineInstallation(string tempFile)
        {
            try
            {
                using Process setup = Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true })
                    ?? throw new InvalidOperationException("Impossibile avviare il programma di installazione di Office.");
                await Task.Run(setup.WaitForExit, _cts?.Token ?? CancellationToken.None);

                try { File.Delete(tempFile); } catch { }
                WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = false;
                WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'installazione: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = false;
            }
        }

        private void SetDownloadButtonStyle(bool isStop)
        {
            if (isStop)
            {
                btnDownloadVerdi.Image = Properties.Resources.pngCloseCreazioneISO;
                btnDownloadVerdi.CheckedBackground = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.CheckedForeColor = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.CheckedImageTint = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.CheckedOutline = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.HoverBackground = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.HoverOutline = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.NormalOutline = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.PressedBackground = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.PressedOutline = Color.FromArgb(192, 0, 0);
                btnDownloadVerdi.Content = "  STOP";
                label1.Visible = true;
                labelpercorso.Visible = true;
                Checkbox_Installa.Visible = true;
                Checkbox_Installa.Checked = WinHubX.Impostazioni.OfficeSettings.Installa;
                Checkbox_Salva.Visible = true;
                Checkbox_Salva.Checked = WinHubX.Impostazioni.OfficeSettings.SalvaFile;
            }
            else
            {
                btnDownloadVerdi.Image = Properties.Resources.pngScaricaOffice;
                btnDownloadVerdi.CheckedBackground = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.CheckedForeColor = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.CheckedImageTint = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.CheckedOutline = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.HoverBackground = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.HoverOutline = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.NormalOutline = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.PressedBackground = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.PressedOutline = Color.FromArgb(46, 125, 60);
                btnDownloadVerdi.Content = "  Download";
            }
        }

        private void Checkbox_Salva_CheckedChanged(object? sender, EventArgs e)
        {
            WinHubX.Impostazioni.OfficeSettings.SalvaFile = Checkbox_Salva.Checked;
        }

        private void Checkbox_Installa_CheckedChanged(object? sender, EventArgs e)
        {
            WinHubX.Impostazioni.OfficeSettings.Installa = Checkbox_Installa.Checked;
        }

        private async Task CheckPendingInstallation()
        {
            if (WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation &&
                !string.IsNullOrEmpty(WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile) &&
                File.Exists(WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile))
            {
                var result = MessageBox.Show(
                    "Trovata un'installazione di Office in sospeso. Vuoi completarla ora?",
                    "Installazione in sospeso",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    SetDownloadButtonStyle(true); 

                    try
                    {
                        if (WinHubX.Impostazioni.OfficeSettings.InstallationType == "Offline")
                        {
                            bool salva = WinHubX.Impostazioni.OfficeSettings.SalvaFile;
                            await StartInstallation(WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile, salva);
                        }
                        else
                        {
                            await StartOnlineInstallation(WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile);
                        }
                    }
                    finally
                    {
                        SetDownloadButtonStyle(false);
                    }
                }
                else
                {
                    WinHubX.Impostazioni.OfficeSettings.HasPendingInstallation = false;
                    WinHubX.Impostazioni.OfficeSettings.LastDownloadedFile = null;
                }
            }
        }
        private static async Task<bool> MountIsoAsync(string isoPath)
        {
            return await RunPowerShellAsync($"Mount-DiskImage -ImagePath '{EscapePowerShellLiteral(isoPath)}'") == 0;
        }

        private static async Task<string> GetIsoDriveLetterAsync(string isoPath)
        {
            string result = await RunPowerShellOutputAsync(
                $"(Get-DiskImage -ImagePath '{EscapePowerShellLiteral(isoPath)}' | Get-Volume).DriveLetter"
            );
            return result.Trim();
        }
        private static async Task<int> RunPowerShellAsync(string command)
        {
            using var process = new Process
            {
                StartInfo = CreatePowerShellStartInfo(command)
            };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode;
        }

        private static async Task<string> RunPowerShellOutputAsync(string command)
        {
            using var process = new Process
            {
                StartInfo = CreatePowerShellStartInfo(command)
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }

        private static ProcessStartInfo CreatePowerShellStartInfo(string command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(command);
            return startInfo;
        }

        private static string EscapePowerShellLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

        private async void FormOffice_Load(object? sender, EventArgs e)
        {
            try
            {
                officeVersions = await CaricaOfficeVersions(Dipendenze.GitHubConfigUrl);
                comboBoxVerOffice.Items.Clear();

                foreach (var office in officeVersions)
                {
                    comboBoxVerOffice.Items.Add(office.Nome);
                }
            }
            catch
            {

            }
            Checkbox_Salva.Checked = WinHubX.Impostazioni.OfficeSettings.SalvaFile;
            Checkbox_Installa.Checked = WinHubX.Impostazioni.OfficeSettings.Installa;
            await CheckPendingInstallation();
        }

        private void AggiornaPercorsoLabel(string downloadPath)
        {
            if (string.IsNullOrEmpty(percorsoCompleto) || percorsoCompleto != downloadPath)
                percorsoCompleto = downloadPath;
            int maxWidth = btn_cambiaBianco.Left - labelpercorso.Left - 10;
            int fullWidth = TextRenderer.MeasureText(percorsoCompleto, labelpercorso.Font).Width;
            if (fullWidth <= maxWidth)
            {
                labelpercorso.Text = percorsoCompleto;
                toolTip1.SetToolTip(labelpercorso, percorsoCompleto);
                return;
            }
            string path = percorsoCompleto;
            while (TextRenderer.MeasureText(path + "...", labelpercorso.Font).Width > maxWidth && path.Contains("\\"))
            {
                int lastSlash = path.LastIndexOf('\\');
                if (lastSlash <= 0) break;
                path = path.Substring(0, lastSlash);
            }

            labelpercorso.Text = path + "...";
        }
        private void btn_cambia_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    AggiornaPercorsoLabel(dialog.SelectedPath);
                }
            }
        }

        private void panel50_Resize(object? sender, EventArgs e) => AggiornaPercorsoLabel(percorsoCompleto);
        private void panel70_Resize(object? sender, EventArgs e) => AggiornaPercorsoLabel(percorsoCompleto);
        private void tableLayoutPanel50_Resize(object? sender, EventArgs e) => AggiornaPercorsoLabel(percorsoCompleto);
        private void FormOffice_Resize(object? sender, EventArgs e) => AggiornaPercorsoLabel(percorsoCompleto);

        private void btnAggRimAppOffice_Click(object? sender, EventArgs e)
        {
            MostraFormInPanel<FormAggiungiRimuoviAppOffice>("AggiungiRimuoviApp", btnAggRimAppOfficePrinci);
        }

        private void btnPersonalizzaOffice_Click(object? sender, EventArgs e)
        {
            string hardwarePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "WinHubX", "Computer", "osehardware.json");

            if (!File.Exists(hardwarePath))
            {
                var popup = new WinHubX.DialogBlock.Form_DialogBlock(form1);
                var panelCenter = panel50.PointToScreen(new Point(panel50.Width / 2, panel50.Height / 2));
                popup.StartPosition = FormStartPosition.Manual;
                popup.Location = new Point(
                    panelCenter.X - popup.Width / 2,
                    panelCenter.Y - popup.Height / 2
                );

                popup.ShowDialog();
                return;
            }
            MostraFormInPanel<PersonalizzazioneOffice>("PersonalizzazioneTitoloForm", btnPersonalizzaOfficePrinci);
        }

        private void MostraFormInPanel<T>(string titoloTraduzione, cuiButton button) where T : Form
        {
            panel50.Controls.Clear();

            Form1? mainForm = Application.OpenForms["Form1"] as Form1;
            if (mainForm == null) return;

            mainForm.pictureBox3.Visible = true;

            mainForm.pictureBox3.Click -= PictureBox3_Click_BackToOffice;
            mainForm.pictureBox3.Click += PictureBox3_Click_BackToOffice;

            mainForm.lblPanelTitle.Text = LanguageManager.GetTranslation("FormPersonallizatoOffice", titoloTraduzione);
            mainForm.pictureBoxlblalto.Image = button.Image;
            Form form = Activator.CreateInstance(typeof(T), mainForm, this) as Form
                ?? throw new InvalidOperationException($"Impossibile creare il form {typeof(T).Name}.");
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;

            panel50.Controls.Add(form);
            form.Show();
        }
    }
}
