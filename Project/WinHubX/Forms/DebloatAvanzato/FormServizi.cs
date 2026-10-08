using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using WinHubX.Impostazioni;

namespace WinHubX.Forms.DebloatAvanzato
{
public partial class FormServizi : Form
{
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static HttpClient CreateHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        }

        private const string ServicesUrl =
            "https://raw.githubusercontent.com/LightYagami28/WinHubX-Resource/refs/heads/main/servizi.json";

        public FormServizi()
        {
            LanguageManager.LoadTranslations();
            InitializeComponent();
            ThemeManager.ApplyThemeToControl(this, ThemeManager.IsDarkTheme);
        }

        private async void FormServizi_Load(object? sender, EventArgs e)
        {
            try
            {
                    string json = await HttpClient.GetStringAsync(ServicesUrl);
                    ServiziRoot serviziRoot = JsonConvert.DeserializeObject<ServiziRoot>(json)
                        ?? throw new InvalidOperationException("Configurazione servizi non valida.");
                    ServiceConfigurationValidator.ValidateCatalog(serviziRoot);

                    for (int i = 0; i < serviziRoot.service.Count; i++)
                    {
                        var servizio = serviziRoot.service[i];
                        _ = DisabilitaServizi.Items.Add(servizio.Name);
                        DisabilitaServizi.SetItemChecked(i, true);
                    }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error: {ex.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void ModificaServiziButton_Click(object? sender, EventArgs e)
        {
            string[] selectedServices = DisabilitaServizi.CheckedItems
                .Cast<string>()
                .ToArray();
            if (selectedServices.Length == 0 || !button1.Enabled)
            {
                return;
            }

            progressBar1.Maximum = selectedServices.Length;
            progressBar1.Value = 0;
            richTextBox1.Clear();
            button1.Enabled = false;
            DisabilitaServizi.Enabled = false;

            try
            {
                await EseguiModificaServiziAsync(selectedServices);
            }
            finally
            {
                button1.Enabled = true;
                DisabilitaServizi.Enabled = true;
            }
        }

        private async Task EseguiModificaServiziAsync(IReadOnlyList<string> selectedServices)
        {
            try
            {
                    string json = await HttpClient.GetStringAsync(ServicesUrl);
                    ServiziRoot serviziRoot = JsonConvert.DeserializeObject<ServiziRoot>(json)
                        ?? throw new InvalidOperationException("Configurazione servizi non valida.");
                    ServiceConfigurationValidator.ValidateCatalog(serviziRoot);

                    int currentStep = 0;

                    foreach (string serviceName in selectedServices)
                    {
                        var servizio = serviziRoot.service.FirstOrDefault(s => s.Name == serviceName);

                        if (servizio != null)
                        {
                            ServiceConfigurationValidator.ValidateService(servizio);

                            string stato = await Task.Run(() =>
                            {
                                try
                                {
                                    var script = "$ErrorActionPreference = 'Stop'; " +
                                        $"Get-Service -Name '{EscapePowerShellLiteral(servizio.Name)}' | Stop-Service; " +
                                        $"Set-Service -Name '{EscapePowerShellLiteral(servizio.Name)}' -StartupType '{servizio.StartupType}'";
                                    var psi = new ProcessStartInfo
                                    {
                                        FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
                                        Verb = "runas",
                                        UseShellExecute = true,
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    };
                                    psi.ArgumentList.Add("-NoProfile");
                                    psi.ArgumentList.Add("-NonInteractive");
                                    psi.ArgumentList.Add("-Command");
                                    psi.ArgumentList.Add(script);

                                    using (var proc = Process.Start(psi)
                                        ?? throw new InvalidOperationException("Impossibile avviare PowerShell."))
                                    {
                                        proc.WaitForExit();

                                        if (proc.ExitCode == 0)
                                        {
                                            return $"Servizio \"{servizio.Name}\" → ✅ Modificato con successo";
                                        }
                                        else
                                        {
                                            return $"❌ PowerShell terminato con codice {proc.ExitCode}.";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return $"❌ Error: {ex.Message}";
                                }
                            });

                            _ = richTextBox1.Invoke((MethodInvoker)(() =>
                            {
                                richTextBox1.AppendText($"{stato}\n");
                                richTextBox1.ScrollToCaret();
                                progressBar1.Value = Math.Min(++currentStep, progressBar1.Maximum);
                            }));
                        }
                    }
            }
            catch (Exception ex)
            {
                _ = richTextBox1.Invoke((MethodInvoker)(() =>
                {
                    richTextBox1.AppendText($"❌ {ex.GetBaseException().Message}");
                }));
            }
        }

        private static string EscapePowerShellLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    }

}
