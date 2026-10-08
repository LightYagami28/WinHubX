using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Globalization;
using WinHubX.Forms.Base;
using WinHubX.Forms.DriverRST;
using WinHubX.Forms.ImpostazioniApp;
using WinHubX.Impostazioni;

namespace WinHubX
{
    public partial class FormWin : Form
    {
        private Form1 form1;
        private static readonly HttpClient ResourceClient = new(CreateResourceHandler())
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        private static SocketsHttpHandler CreateResourceHandler() => new()
        {
            MaxConnectionsPerServer = 4,
            AutomaticDecompression = System.Net.DecompressionMethods.None,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        public FormWin(Form1 form1)
        {
            LanguageManager.LoadLanguageFromSettings();
            InitializeComponent();
            btnAttivaWindowsPrinci.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => "  Attiva Windows",
                "en" => "  Activate Windows",
                _ => btnAttivaWindowsPrinci.Content
            };
            btnCambiaEdizionePrinci.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => "  Cambia edizione",
                "en" => "  Change edition",
                _ => btnCambiaEdizionePrinci.Content
            };
            btnCreaIsoPrinci.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => "  Crea ISO",
                "en" => "  Create ISO",
                _ => btnCreaIsoPrinci.Content
            };
            this.form1 = form1;
            ThemeManager.ApplyThemeToControl(this, ThemeManager.IsDarkTheme);
        }

        private void btnAttivaWin_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:activation") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Impossibile aprire le impostazioni di attivazione: {ex.Message}", "WinHubX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnCambioEdizione_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Questa funzione scarica ed esegue lo script ufficiale Microsoft-Activation-Scripts per cambiare l'edizione di Windows. Continuare?",
                    "Avviso script esterno", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            string tempScript = Path.Combine(Path.GetTempPath(), $"WinHubX-ChangeEdition-{Guid.NewGuid():N}.cmd");
            try
            {
                string jsonResponse = await ResourceClient.GetStringAsync(Dipendenze.GitHubConfigUrl);
                var jsonObject = JObject.Parse(jsonResponse);
                string primaryUrl = jsonObject["FormWin"]?["cambiowin"]?.ToString()
                    ?? throw new InvalidOperationException("URL script non presente nella configurazione.");
                if (!Uri.TryCreate(primaryUrl, UriKind.Absolute, out Uri? scriptUri)
                    || scriptUri.Scheme != Uri.UriSchemeHttps
                    || !scriptUri.Host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                    || !scriptUri.AbsolutePath.StartsWith("/massgravel/Microsoft-Activation-Scripts/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Lo script deve provenire dal repository ufficiale Massgrave tramite HTTPS.");

                await DownloadManager.DownloadFileAsync(scriptUri.ToString(), tempScript,
                    CancellationToken.None, autoParallel: false);
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = false
                    }
                };
                process.StartInfo.ArgumentList.Add("/d");
                process.StartInfo.ArgumentList.Add("/c");
                process.StartInfo.ArgumentList.Add(tempScript);
                _ = process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Impossibile eseguire il cambio edizione: {ex.Message}",
                    "WinHubX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (File.Exists(tempScript)) File.Delete(tempScript); }
                catch (IOException) { }
            }
        }

        private void btnCreaIso_Click(object sender, EventArgs e)
        {
            panel50.Controls.Clear();
            Form1? mainForm = Application.OpenForms["Form1"] as Form1;
            if (mainForm == null) return;
            mainForm.pictureBox3.Visible = true;
            mainForm.pictureBox3.Click += (s, ev) =>
            {
                mainForm.pictureBox3.Visible = false;
                mainForm.LoadForm(new FormWin(mainForm), mainForm.btnWin, LanguageManager.GetTranslation("FormMain", "Windows"));
            };
            mainForm.pictureBoxlblalto.Image = btnCreaIsoPrinci.Image;
            mainForm.lblPanelTitle.Text = LanguageManager.GetTranslation("FormWin", "CreaISO");
            FormCreaISO formCreaIso = new FormCreaISO(mainForm);
            formCreaIso.TopLevel = false;
            formCreaIso.FormBorderStyle = FormBorderStyle.None;
            formCreaIso.Dock = DockStyle.Fill;
            panel50.Controls.Add(formCreaIso);
            formCreaIso.Show();
        }

        private void btnDownloadIsoPrinci_Click(object sender, EventArgs e)
        {
            string url = "https://mrnico98.github.io/ISODownloader/";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossibile aprire il browser: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDriverRSTPrinci_Click(object sender, EventArgs e)
        {
            FormDriverRST formDriverRST = new FormDriverRST();
            formDriverRST.Show();
        }
    }
}
