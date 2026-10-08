using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using WinHubX.Forms.Base;
using WinHubX.Impostazioni;

namespace WinHubX.Forms.Settaggi
{
    public partial class FormPrivacy : Form, IImportedSettingsForm
    {
        private sealed record PrivacySelection(HashSet<string> Disable, HashSet<string> Enable);

        private readonly Form1 form1;
        private readonly FormSettaggi formSettaggi;
        private int tIndex = -1;
        private int totalSteps = 0;
        public FormPrivacy(FormSettaggi formSettaggi, Form1 form1)
        {
            InitializeComponent();
            this.form1 = form1;
            this.formSettaggi = formSettaggi;
            LoadCheckboxStates();
            DisabilitaPrivacy.MouseMove += new MouseEventHandler(checkedListBox1_MouseMove);
            AbilitaPrivacy.MouseMove += new MouseEventHandler(checkedListBox2_MouseMove);
            ThemeManager.ApplyThemeToControl(this, ThemeManager.IsDarkTheme);
            btnAvviaSelezionatiVerdi.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => "  Avvia",
                "en" => "  Start",
                _ => btnAvviaSelezionatiVerdi.Content
            };
            btnSuggeritiVerdi.Content = LanguageManager.CurrentLanguage switch
            {
                "it" => "  Applica",
                "en" => "  Apply",
                _ => btnSuggeritiVerdi.Content
            };
        }

        private void checkedListBox1_MouseMove(object? sender, MouseEventArgs e)
        {
            int index = DisabilitaPrivacy.IndexFromPoint(e.Location);
            if (tIndex != index)
            {
                tIndex = index;
                if (tIndex > -1)
                {
                    string tooltipText = GetTooltipTextDisa(tIndex);
                    toolTip1.SetToolTip(DisabilitaPrivacy, tooltipText);
                }
            }
        }

        private void checkedListBox2_MouseMove(object? sender, MouseEventArgs e)
        {
            int index = AbilitaPrivacy.IndexFromPoint(e.Location);
            if (tIndex != index)
            {
                tIndex = index;
                if (tIndex > -1)
                {

                    string tooltipText = GetTooltipTextAbil(tIndex);
                    toolTip1.SetToolTip(AbilitaPrivacy, tooltipText);
                }
            }
        }

        private string GetTooltipTextDisa(int index)
        {
            return LanguageManager.GetTranslation("FormPrivacy", $"tooltipDisa{index}");
        }
        private string GetTooltipTextAbil(int index)
        {
            return LanguageManager.GetTranslation("FormPrivacy", $"tooltipAbil{index}");
        }

        private void SetCheckboxState(string itemName, bool isChecked)
        {
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey("Software\\WinHubX"))
            {
                key?.SetValue(itemName, isChecked ? 1 : 0, RegistryValueKind.DWord);
            }
        }

        private bool GetCheckboxState(string itemName)
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\WinHubX"))
            {
                return key?.GetValue(itemName) is int value && value == 1;
            }
        }

        private void LoadCheckboxStates()
        {
            var checkboxMappings = new (CheckedListBox box, string displayName, string regKey)[]
            {
        (DisabilitaPrivacy, "Disabilita Opzioni Lingua", "DisabilitaOpzioniLingua"),
        (DisabilitaPrivacy, "Disabilita Suggerimenti App", "DisabilitaSuggerimentiApp"),
        (DisabilitaPrivacy, "Disabilita Telemetria", "DisabilitaTelemetria"),
        (DisabilitaPrivacy, "Disabilita Tracking", "DisabilitaTracking"),
        (DisabilitaPrivacy, "Disabilita Segnalazione Errori", "DisabilitaSegnalazioneErrori"),
        (DisabilitaPrivacy, "Disabilita Tracking Diagnostica", "DisabilitaTrackingDiagnostica"),
        (DisabilitaPrivacy, "Disabilita WAP Push Service", "DisabilitaWAPPushService"),
        (DisabilitaPrivacy, "Disabilita Home Group", "DisbailitaHomeGroup"),
        (DisabilitaPrivacy, "Disabilita Assistenza Remota", "DisabilitaAssistenzaRemota"),
        (DisabilitaPrivacy, "Disabilita Schedul Defrag", "DisbailitaSchedulDefrag"),
        (DisabilitaPrivacy, "Disabilita Xbox Features", "DisabilitaXboxFeatures"),
        (DisabilitaPrivacy, "Disabilita Auto Manteinance", "DisabilitaAutoManteinance"),
        (DisabilitaPrivacy, "Disabilita Spazio Riservato", "DisabilitaSpazioRiservato"),
        (DisabilitaPrivacy, "Disabilita Tweaks Game DVR", "DisabilitaTweaksGameDVR"),
        (DisabilitaPrivacy, "Disabilita Storia Attivita", "DisabilitaStoriaAttivita"),
        (DisabilitaPrivacy, "Disabilita Wifi-Sense", "DisabilitaWifiSense"),
        (DisabilitaPrivacy, "Disabilita Notifiche Tray/Calendario", "DisabilitaNotificheTrayCalendario"),

        (AbilitaPrivacy, "Abilita Opzioni Lingua", "AbilitaOpzioniLingua"),
        (AbilitaPrivacy, "Abilita Suggerimenti App", "AbilitaSuggerimentiApp"),
        (AbilitaPrivacy, "Abilita Telemetria", "AbilitaTelemetria"),
        (AbilitaPrivacy, "Abilita Tracking", "AbilitaTracking"),
        (AbilitaPrivacy, "Abilita Segnalazione Errori", "AbilitaSegnalazioneErrori"),
        (AbilitaPrivacy, "Abilita Tracking Diagnostica", "AbilitaTrackingDiagnostica"),
        (AbilitaPrivacy, "Abilita WAP Push Service", "AbilitaWAPPushService"),
        (AbilitaPrivacy, "Abilita Home Group", "AbilitaHomeGroup"),
        (AbilitaPrivacy, "Abilita Assistenza Remota", "AbilitaAssistenzaRemota"),
        (AbilitaPrivacy, "Abilita Schedul Defrag", "AbilitaSchedulDefrag"),
        (AbilitaPrivacy, "Abilita Xbox Features", "AbilitaXboxFeatures"),
        (AbilitaPrivacy, "Abilita Auto Manteinance", "AbilitaAutoManteinance"),
        (AbilitaPrivacy, "Abilita Spazio Riservato", "AbilitaSpazioRiservato"),
        (AbilitaPrivacy, "Abilita Tweaks Game DVR", "AbilitaTweaksGameDVR"),
        (AbilitaPrivacy, "Abilita Storie Attivita", "AbilitaStoriaAttivita"),
        (AbilitaPrivacy, "Abilita Wifi-Sense", "AbilitaWifiSense"),
        (AbilitaPrivacy, "Abilita Notifiche Tray/Calendario", "AbilitaNotificheTrayCalendario")
            };

            foreach (var (box, displayName, regKey) in checkboxMappings)
            {
                int index = box.Items.IndexOf(displayName);
                if (index != -1)
                {
                    box.SetItemChecked(index, GetCheckboxState(regKey));
                }
            }
        }

        private void ExecutePowerShellScript(string script, bool use32BitRegistry = false)
        {
            if (use32BitRegistry)
            {
                script = script.Replace("HKLM:\\SOFTWARE\\", "HKLM:\\SOFTWARE\\WOW6432Node\\");
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(script);

            using var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException("Impossibile avviare Windows PowerShell.");
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            string output = outputTask.GetAwaiter().GetResult();
            string error = errorTask.GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(output))
            {
                System.Diagnostics.Debug.WriteLine(output);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Lo script PowerShell è terminato con codice {process.ExitCode}: {error}");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                System.Diagnostics.Debug.WriteLine(error);
            }
        }

        private void btnSuggeriti_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                       "WinHubX\\Computer\\osehardware.json");

            string tipoDisk = "";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                HardwareInfo? info = JsonSerializer.Deserialize<HardwareInfo>(json);
                tipoDisk = info?.Hardware?.Disk ?? "";
            }

            bool isHDD = tipoDisk.Contains("HDD", StringComparison.OrdinalIgnoreCase);
            var daDisabilitare = new List<string>
    {
        "Disabilita Opzioni Lingua",
        "Disabilita Suggerimenti App",
        "Disabilita Telemetria",
        "Disabilita Tracking",
        "Disabilita Segnalazione Errori",
        "Disabilita Tracking Diagnostica",
        "Disabilita WAP Push Service",
        "Disabilita Home Group",
        "Disabilita Assistenza Remota",
        "Disabilita Auto Manteinance",
        "Disabilita Spazio Riservato",
        "Disabilita Tweaks Game DVR",
        "Disabilita Storia Attivita",
        "Disabilita Wifi-Sense"
    };
            if (!isHDD)
            {
                daDisabilitare.Add("Disabilita Schedul Defrag");
            }
            for (int i = 0; i < DisabilitaPrivacy.Items.Count; i++)
                DisabilitaPrivacy.SetItemChecked(i, false);
            foreach (string nome in daDisabilitare)
            {
                int index = DisabilitaPrivacy.Items.IndexOf(nome);
                if (index != -1)
                    DisabilitaPrivacy.SetItemChecked(index, true);
            }
            string[] daAbilitare;

            if (isHDD)
            {
                daAbilitare = new string[] { "Abilita Schedul Defrag" };
            }
            else
            {
                daAbilitare = new string[0];
            }
            for (int i = 0; i < AbilitaPrivacy.Items.Count; i++)
                AbilitaPrivacy.SetItemChecked(i, false);
            foreach (string nome in daAbilitare)
            {
                int index = AbilitaPrivacy.Items.IndexOf(nome);
                if (index != -1)
                    AbilitaPrivacy.SetItemChecked(index, true);
            }
        }


        private void btnAvviaSelezionati_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                return;
            }

            var selection = new PrivacySelection(
                DisabilitaPrivacy.CheckedItems.Cast<string>().ToHashSet(StringComparer.Ordinal),
                AbilitaPrivacy.CheckedItems.Cast<string>().ToHashSet(StringComparer.Ordinal));
            totalSteps = selection.Disable.Count + selection.Enable.Count;
            if (totalSteps == 0)
            {
                return;
            }

            progressBar1.MaxValue = totalSteps;
            progressBar1.Value = 0;
            backgroundWorker1.RunWorkerAsync(selection);
        }

        System.Threading.Tasks.Task IImportedSettingsForm.ApplyImportedSettingsAsync() =>
            ImportedSettingsWorker.RunAsync(
                backgroundWorker1,
                () => btnAvviaSelezionati_Click(btnAvviaSelezionatiVerdi, EventArgs.Empty));

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error is not null)
            {
                _ = MessageBox.Show(
                    $"Operazione non completata: {e.Error.GetBaseException().Message}",
                    "WinHubX",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (e.Cancelled)
            {
                _ = MessageBox.Show("Operazione annullata.", "WinHubX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (e.Result is List<string> failures && failures.Count > 0)
            {
                string details = string.Join(Environment.NewLine, failures.Distinct().Take(5));
                string remaining = failures.Count > 5
                    ? $"{Environment.NewLine}Altri errori: {failures.Count - 5}."
                    : string.Empty;
                _ = MessageBox.Show(
                    $"Alcune impostazioni privacy non sono state applicate:{Environment.NewLine}{details}{remaining}",
                    "WinHubX",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string messaggio = LanguageManager.GetTranslation("Global", "modifichesuccesso");

            _ = MessageBox.Show(
                messaggio,
                "WinHubX",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = Math.Min(e.ProgressPercentage, progressBar1.MaxValue);
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument is not PrivacySelection selection)
            {
                throw new InvalidOperationException("Selezione delle impostazioni privacy non valida.");
            }

            int currentStep = 0;
            var failures = new List<string>();
            if (selection.Disable.Contains("Disabilita Opzioni Lingua"))
            {
                SetCheckboxState("DisabilitaOpzioniLingua", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                                                          .OpenSubKey(@"Control Panel\International\User Profile", writable: true))
                    {
                        key64?.SetValue("HttpAcceptLanguageOptOut", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                                                          .OpenSubKey(@"Control Panel\International\User Profile", writable: true))
                    {
                        key32?.SetValue("HttpAcceptLanguageOptOut", 1, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaOpzioniLingua", false);
            }
            if (selection.Disable.Contains("Disabilita Suggerimenti App"))
            {
                SetCheckboxState("DisabilitaSuggerimentiApp", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    var registrySettings = new Dictionary<string, Tuple<string, int>>
                {
                { @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", new Tuple<string, int>("DisableThirdPartySuggestions", 1) },
                { @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", new Tuple<string, int>("DisableWindowsConsumerFeatures", 1) },
                { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata", new Tuple<string, int>("PreventDeviceMetadataFromNetwork", 1) },
                { @"SOFTWARE\Policies\Microsoft\MRT", new Tuple<string, int>("DontOfferThroughWUAU", 1) },
                { @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", new Tuple<string, int>("CEIPEnable", 0) },
                { @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", new Tuple<string, int>("AITEnable", 0) },
                { @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", new Tuple<string, int>("DisableUAR", 1) },
                { @"SYSTEM\CurrentControlSet\Control\WMI\AutoLogger\AutoLogger-Diagtrack-Listener", new Tuple<string, int>("Start", 0) },
                { @"SYSTEM\CurrentControlSet\Control\WMI\AutoLogger\SQMLogger", new Tuple<string, int>("Start", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SilentInstalledAppsEnabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SystemPaneSuggestionsEnabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SoftLandingEnabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-310093Enabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-314559Enabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-338393Enabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-353694Enabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-353698Enabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("ContentDeliveryAllowed", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("OemPreInstalledAppsEnabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("PreInstalledAppsEnabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("PreInstalledAppsEverEnabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-338387Enabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-338388Enabled", 0) },
                { @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", new Tuple<string, int>("SubscribedContent-338389Enabled", 0) }
                };
                    foreach (var registryView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        foreach (var setting in registrySettings)
                        {
                            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
                            using (RegistryKey subKey = baseKey.CreateSubKey(setting.Key, writable: true))
                            {
                                subKey?.SetValue(setting.Value.Item1, setting.Value.Item2, RegistryValueKind.DWord);
                            }
                        }
                        using (RegistryKey baseKeyCU = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, registryView))
                        {
                            foreach (var setting in registrySettings.Where(s => s.Key.StartsWith("Software", StringComparison.OrdinalIgnoreCase)))
                            {
                                using (RegistryKey subKey = baseKeyCU.CreateSubKey(setting.Key, writable: true))
                                {
                                    subKey?.SetValue(setting.Value.Item1, setting.Value.Item2, RegistryValueKind.DWord);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaSuggerimentiApp", false);
            }
            if (selection.Disable.Contains("Disabilita Telemetria"))
            {
                SetCheckboxState("DisabilitaTelemetria", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                            "AllowTelemetry", 0, RegistryValueKind.DWord, view);
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                            "AllowTelemetry", 0, RegistryValueKind.DWord, view);
                    }
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                        "DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                        "DisabledByGroupPolicy", 1, RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
                        "Disabled", 1, RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config",
                        "DODownloadMode", 1, RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Control\Remote Assistance",
                        "fAllowToGetHelp", 0, RegistryValueKind.DWord, RegistryView.Registry64);

                    string scheduledTaskScript = @"
            $taskNames = @(
                'Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser',
                'Microsoft\Windows\Application Experience\ProgramDataUpdater',
                'Microsoft\Windows\Autochk\Proxy',
                'Microsoft\Windows\Customer Experience Improvement Program\Consolidator',
                'Microsoft\Windows\Customer Experience Improvement Program\UsbCeip',
                'Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector',
                'Microsoft\Windows\Feedback\Siuf\DmClient',
                'Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload',
                'Microsoft\Windows\Windows Error Reporting\QueueReporting',
                'Microsoft\Windows\Application Experience\MareBackup',
                'Microsoft\Windows\Application Experience\StartupAppTask',
                'Microsoft\Windows\Application Experience\PcaPatchDbTask',
                'Microsoft\Windows\Maps\MapsUpdateTask'
            )
            foreach ($taskName in $taskNames) {
                $separatorIndex = $taskName.LastIndexOf('\')
                $taskPath = '\' + $taskName.Substring(0, $separatorIndex + 1)
                $taskLeafName = $taskName.Substring($separatorIndex + 1)
                $task = Get-ScheduledTask -TaskName $taskLeafName -TaskPath $taskPath -ErrorAction SilentlyContinue
                if ($null -ne $task -and $task.State -ne 'Disabled') {
                    Disable-ScheduledTask -TaskName $taskLeafName -TaskPath $taskPath -ErrorAction Stop | Out-Null
                }
            }
            ";
                    string elevatedScript = registryChanges.BuildCommand() + Environment.NewLine + scheduledTaskScript;
                    RunElevatedPowerShellScript(Convert.ToBase64String(Encoding.Unicode.GetBytes(elevatedScript)));

                    using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                    {
                        key?.SetValue("ContentDeliveryAllowed", 0, RegistryValueKind.DWord);
                        key?.SetValue("OemPreInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("PreInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("PreInstalledAppsEverEnabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("SilentInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("SubscribedContent-338387Enabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("SubscribedContent-353698Enabled", 0, RegistryValueKind.DWord);
                        key?.SetValue("SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Siuf\Rules"))
                    {
                        key?.SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                    {
                        key.SetValue("DisableTailoredExperiencesWithDiagnosticData", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"))
                    {
                        key.SetValue("EnableFeeds", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Feeds"))
                    {
                        key.SetValue("ShellFeedsTaskbarViewMode", 2, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                    {
                        key.SetValue("HideSCAMeetNow", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement"))
                    {
                        key.SetValue("ScoobeSystemSettingEnabled", 0, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaTelemetria", false);
            }
            if (selection.Disable.Contains("Disabilita Tracking"))
            {
                SetCheckboxState("DisabilitaTracking", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                            "Value", "Deny", RegistryValueKind.String, view);
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}",
                            "SensorPermissionState", 0, RegistryValueKind.DWord, view);
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration",
                            "Status", 0, RegistryValueKind.DWord, view);
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaTracking", false);
            }
            if (selection.Disable.Contains("Disabilita Segnalazione Errori"))
            {
                SetCheckboxState("DisabilitaSegnalazioneErrori", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1,
                            RegistryValueKind.DWord, view);
                    }
                    ApplyElevatedRegistryMutations(registryChanges, @"
            $taskPath = '\Microsoft\Windows\Windows Error Reporting\'
            $taskName = 'QueueReporting'
            $task = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction SilentlyContinue
            if ($null -ne $task -and $task.State -ne 'Disabled') {
                Disable-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction Stop | Out-Null
            }
            ");
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaSegnalazioneErrori", false);
            }
            if (selection.Disable.Contains("Disabilita Tracking Diagnostica"))
            {
                SetCheckboxState("DisabilitaTrackingDiagnostica", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    var registryChanges = new ElevatedRegistryMutationBatch();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableDiagnostics", 1,
                        RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableDiagnostics", 1,
                        RegistryValueKind.DWord, RegistryView.Registry32);
                    ConfigureServices(registryChanges,
                        new PrivacyServiceChange("DiagTrack", "Disabled", StartAfterConfiguration: false));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaTrackingDiagnostica", false);
            }
            if (selection.Disable.Contains("Disabilita WAP Push Service"))
            {
                SetCheckboxState("DisabilitaWAPPushService", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    var registryChanges = new ElevatedRegistryMutationBatch();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableWAPPushService", 1,
                        RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableWAPPushService", 1,
                        RegistryValueKind.DWord, RegistryView.Registry32);
                    ConfigureServices(registryChanges,
                        new PrivacyServiceChange("dmwappushservice", "Disabled", StartAfterConfiguration: false));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaWAPPushService", false);
            }
            if (selection.Disable.Contains("Disabilita Home Group"))
            {
                SetCheckboxState("DisabilitaHomeGroup", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    var registryChanges = new ElevatedRegistryMutationBatch();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableHomeGroup", 1,
                        RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableHomeGroup", 1,
                        RegistryValueKind.DWord, RegistryView.Registry32);
                    ConfigureServices(registryChanges,
                        new PrivacyServiceChange("HomeGroupListener", "Disabled", StartAfterConfiguration: false),
                        new PrivacyServiceChange("HomeGroupProvider", "Disabled", StartAfterConfiguration: false));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisbailitaHomeGroup", false);
            }
            if (selection.Disable.Contains("Disabilita Assistenza Remota"))
            {
                SetCheckboxState("DisabilitaAssistenzaRemota", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 0,
                            RegistryValueKind.DWord, view);
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaAssistenzaRemota", false);
            }
            if (selection.Disable.Contains("Disabilita Schedul Defrag"))
            {
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                SetCheckboxState("DisbailitaSchedulDefrag", true);
                try
                {
                    ApplyElevatedRegistryMutations(new ElevatedRegistryMutationBatch(), @"
            $taskPath = '\Microsoft\Windows\Defrag\'
            $taskName = 'ScheduledDefrag'
            $task = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction SilentlyContinue
            if ($null -ne $task -and $task.State -ne 'Disabled') {
                Disable-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction Stop | Out-Null
            }
            ");
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisbailitaSchedulDefrag", false);
            }
            if (selection.Disable.Contains("Disabilita Xbox Features"))
            {
                SetCheckboxState("DisabilitaXboxFeatures", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0,
                        RegistryValueKind.DWord, RegistryView.Registry64);
                    ApplyElevatedRegistryMutations(registryChanges);

                    ExecutePowerShellScript(@"
                Get-AppxPackage ""Microsoft.XboxApp"" | Remove-AppxPackage -ErrorAction SilentlyContinue;
                Get-AppxPackage ""Microsoft.XboxIdentityProvider"" | Remove-AppxPackage -ErrorAction SilentlyContinue;
                Get-AppxPackage ""Microsoft.XboxSpeechToTextOverlay"" | Remove-AppxPackage -ErrorAction SilentlyContinue;
                Get-AppxPackage ""Microsoft.XboxGameOverlay"" | Remove-AppxPackage -ErrorAction SilentlyContinue;
                Get-AppxPackage ""Microsoft.Xbox.TCUI"" | Remove-AppxPackage -ErrorAction SilentlyContinue;
            ");

                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                                                             .OpenSubKey(@"System\GameConfigStore", writable: true))
                    {
                        key32?.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaXboxFeatures", false);
            }
            if (selection.Disable.Contains("Disabilita Auto Manteinance"))
            {
                SetCheckboxState("DisabilitaAutoManteinance", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
                            "MaintenanceDisabled", 1, RegistryValueKind.DWord, view);
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaAutoManteinance", false);
            }
            if (selection.Disable.Contains("Disabilita Spazio Riservato"))
            {
                SetCheckboxState("DisabilitaSpazioRiservato", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\ReservedStorage", "ReservedStorageState", 0,
                            RegistryValueKind.DWord, view);
                    }
                    string script = "$ErrorActionPreference = 'Stop'" + Environment.NewLine
                        + "Set-WindowsReservedStorageState -State Disabled -Online -ErrorAction Stop" + Environment.NewLine
                        + registryChanges.BuildCommand();
                    RunElevatedPowerShellScript(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaSpazioRiservato", false);
            }
            if (selection.Disable.Contains("Disabilita Tweaks Game DVR"))
            {
                SetCheckboxState("DisabilitaTweaksGameDVR", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        foreach (string valueName in new[]
                        {
                            "GameDVR_DXGIHonorFSEWindowsCompatible",
                            "GameDVR_HonorUserFSEBehaviorMode",
                            "GameDVR_EFSEFeatureFlags"
                        })
                        {
                            registryChanges.SetValue(RegistryHive.LocalMachine, @"SYSTEM\GameConfigStore", valueName,
                                new byte[4], RegistryValueKind.Binary, view);
                        }
                        registryChanges.SetValue(RegistryHive.LocalMachine, @"SYSTEM\GameConfigStore", "GameDVR_Enabled",
                            0, RegistryValueKind.DWord, view);
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaTweaksGameDVR", false);
            }
            if (selection.Disable.Contains("Disabilita Storia Attivita"))
            {
                SetCheckboxState("DisabilitaStoriaAttivita", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        foreach (string valueName in new[] { "EnableActivityFeed", "PublishUserActivities", "UploadUserActivities" })
                        {
                            registryChanges.SetValue(RegistryHive.LocalMachine,
                                @"SOFTWARE\Policies\Microsoft\Windows\System", valueName, 0,
                                RegistryValueKind.DWord, view);
                        }
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaStoriaAttivita", false);
            }
            if (selection.Disable.Contains("Disabilita Wifi-Sense"))
            {
                SetCheckboxState("DisabilitaWifiSense", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"Software\Microsoft\PolicyManager\default\WiFi", "AllowWiFiHotSpotReporting", 0,
                            RegistryValueKind.DWord, view);
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"Software\Microsoft\PolicyManager\default\WiFi", "AllowAutoConnectToWiFiSenseHotspots", 0,
                            RegistryValueKind.DWord, view);
                        foreach (string policyName in new[] { "AllowWiFiHotSpotReporting", "AllowAutoConnectToWiFiSenseHotspots" })
                        {
                            registryChanges.SetValue(RegistryHive.LocalMachine,
                                $@"SOFTWARE\Microsoft\PolicyManager\default\WiFi\{policyName}", "Value", 0,
                                RegistryValueKind.DWord, view);
                        }
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config", "AutoConnectAllowedOEM", 0,
                            RegistryValueKind.DWord, view);
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config", "WiFISenseAllowed", 0,
                            RegistryValueKind.DWord, view);
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaWifiSense", false);
            }
            if (selection.Disable.Contains("Disabilita Notifiche Tray/Calendario"))
            {
                SetCheckboxState("DisabilitaNotificheTrayCalendario", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                                                             .CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer"))
                    {
                        key64?.SetValue("DisableNotificationCenter", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                                                             .CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PushNotifications"))
                    {
                        key64?.SetValue("ToastEnabled", 0, RegistryValueKind.DWord);
                    }
                    using (RegistryKey key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                                                             .CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer"))
                    {
                        key32?.SetValue("DisableNotificationCenter", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                                                             .CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PushNotifications"))
                    {
                        key32?.SetValue("ToastEnabled", 0, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaNotificheTrayCalendario", false);
            }
            if (selection.Enable.Contains("Abilita Opzioni Lingua"))
            {
                SetCheckboxState("AbilitaOpzioniLingua", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Control Panel\International\User Profile"))
                    {
                        key?.SetValue("HttpAcceptLanguageOptOut", 0, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaOpzioniLingua", false);
            }
            if (selection.Enable.Contains("Abilita Suggerimenti App"))
            {
                SetCheckboxState("AbilitaSuggerimentiApp", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    registryChanges.DeleteValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures",
                        RegistryView.Registry64);
                    ApplyElevatedRegistryMutations(registryChanges);

                    string contentDeliveryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(contentDeliveryPath))
                    {
                        if (key != null)
                        {
                            key.SetValue("ContentDeliveryAllowed", 1, RegistryValueKind.DWord);
                            key.SetValue("OemPreInstalledAppsEnabled", 1, RegistryValueKind.DWord);
                            key.SetValue("PreInstalledAppsEnabled", 1, RegistryValueKind.DWord);
                            key.SetValue("PreInstalledAppsEverEnabled", 1, RegistryValueKind.DWord);
                            key.SetValue("SilentInstalledAppsEnabled", 1, RegistryValueKind.DWord);
                            key.SetValue("SubscribedContent-338388Enabled", 1, RegistryValueKind.DWord);
                            key.SetValue("SubscribedContent-338389Enabled", 1, RegistryValueKind.DWord);
                            key.SetValue("SystemPaneSuggestionsEnabled", 1, RegistryValueKind.DWord);
                            key.DeleteValue("SubscribedContent-338387Enabled", false);
                            key.DeleteValue("SubscribedContent-353698Enabled", false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaSuggerimentiApp", false);
            }
            if (selection.Enable.Contains("Abilita Telemetria"))
            {
                SetCheckboxState("AbilitaTelemetria", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    string dataCollectionPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection";
                    string wow6432NodePath = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection";
                    string policiesPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
                    var registryChanges = new ElevatedRegistryMutationBatch();
                    registryChanges.SetValue(RegistryHive.LocalMachine, dataCollectionPath, "AllowTelemetry", 3,
                        RegistryValueKind.DWord, RegistryView.Default);
                    registryChanges.SetValue(RegistryHive.LocalMachine, wow6432NodePath, "AllowTelemetry", 3,
                        RegistryValueKind.DWord, RegistryView.Default);
                    registryChanges.SetValue(RegistryHive.LocalMachine, policiesPath, "AllowTelemetry", 3,
                        RegistryValueKind.DWord, RegistryView.Default);
                    ConfigureServices(registryChanges,
                        new PrivacyServiceChange("DiagTrack", "Automatic", StartAfterConfiguration: true),
                        new PrivacyServiceChange("dmwappushservice", "Automatic", StartAfterConfiguration: true));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaTelemetria", false);
            }
            if (selection.Enable.Contains("Abilita Tracking"))
            {
                SetCheckboxState("AbilitaTracking", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                        "Value", "Allow", RegistryValueKind.String, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}",
                        "SensorPermissionState", 1, RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration",
                        "Status", 1, RegistryValueKind.DWord, RegistryView.Registry64);
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaTracking", false);
            }
            if (selection.Enable.Contains("Abilita Segnalazione Errori"))
            {
                SetCheckboxState("AbilitaSegnalazioneErrori", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    registryChanges.DeleteValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", RegistryView.Registry64);
                    registryChanges.DeleteValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", RegistryView.Registry32);
                    ApplyElevatedRegistryMutations(registryChanges, @"
            $taskPath = '\Microsoft\Windows\Windows Error Reporting\'
            $taskName = 'QueueReporting'
            $task = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction SilentlyContinue
            if ($null -ne $task -and $task.State -eq 'Disabled') {
                Enable-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction Stop | Out-Null
            }
            ");
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaSegnalazioneErrori", false);
            }
            if (selection.Enable.Contains("Abilita Tracking Diagnostica"))
            {
                SetCheckboxState("AbilitaTrackingDiagnostica", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    var registryChanges = new ElevatedRegistryMutationBatch();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 3,
                        RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 3,
                        RegistryValueKind.DWord, RegistryView.Default);
                    ConfigureServices(registryChanges,
                        new PrivacyServiceChange("DiagTrack", "Automatic", StartAfterConfiguration: true));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaTrackingDiagnostica", false);
            }
            if (selection.Enable.Contains("Abilita WAP Push Service"))
            {
                SetCheckboxState("AbilitaWAPPushService", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    var registryChanges = new ElevatedRegistryMutationBatch();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Services\dmwappushservice", "DelayedAutoStart", 1,
                        RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SYSTEM\WOW6432Node\CurrentControlSet\Services\dmwappushservice", "DelayedAutoStart", 1,
                        RegistryValueKind.DWord, RegistryView.Default);
                    ConfigureServices(registryChanges,
                        new PrivacyServiceChange("dmwappushservice", "Automatic", StartAfterConfiguration: true));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaWAPPushService", false);
            }
            if (selection.Enable.Contains("Abilita Home Group"))
            {
                SetCheckboxState("AbilitaHomeGroup", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ConfigureServices(new ElevatedRegistryMutationBatch(),
                        new PrivacyServiceChange("HomeGroupListener", "Manual", StartAfterConfiguration: false),
                        new PrivacyServiceChange("HomeGroupProvider", "Manual", StartAfterConfiguration: false));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaHomeGroup", false);
            }
            if (selection.Enable.Contains("Abilita Assistenza Remota"))
            {
                SetCheckboxState("AbilitaAssistenzaRemota", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 1,
                        RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 1,
                        RegistryValueKind.DWord, RegistryView.Registry32);
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (UnauthorizedAccessException ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaAssistenzaRemota", false);
            }
            if (selection.Enable.Contains("Abilita Schedul Defrag"))
            {
                SetCheckboxState("AbilitaSchedulDefrag", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Defrag",
                        "ScheduledDefrag", 1, RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Defrag",
                        "ScheduledDefrag", 1, RegistryValueKind.DWord, RegistryView.Registry32);
                    ApplyElevatedRegistryMutations(registryChanges, @"
            $taskPath = '\Microsoft\Windows\Defrag\'
            $taskName = 'ScheduledDefrag'
            $task = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction SilentlyContinue
            if ($null -ne $task -and $task.State -eq 'Disabled') {
                Enable-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction Stop | Out-Null
            }
            ");
                }
                catch (UnauthorizedAccessException ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaSchedulDefrag", false);
            }
            if (selection.Enable.Contains("Abilita Xbox Features"))
            {
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                SetCheckboxState("AbilitaXboxFeatures", true);
                ExecutePowerShellScript(@"Get-AppxPackage -AllUsers """"Microsoft.XboxApp"""" | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register """"$($_.InstallLocation)\AppXManifest.xml""""};
                     Get-AppxPackage -AllUsers """"Microsoft.XboxIdentityProvider"""" | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register """"$($_.InstallLocation)\AppXManifest.xml""""};
                     Get-AppxPackage -AllUsers """"Microsoft.XboxSpeechToTextOverlay"""" | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register """"$($_.InstallLocation)\AppXManifest.xml""""};
                     Get-AppxPackage -AllUsers """"Microsoft.XboxGameOverlay"""" | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register """"$($_.InstallLocation)\AppXManifest.xml""""};
                     Get-AppxPackage -AllUsers """"Microsoft.Xbox.TCUI"""" | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register """"$($_.InstallLocation)\AppXManifest.xml""""};
                     Set-ItemProperty -Path """"HKCU:\System\GameConfigStore"""" -Name """"GameDVR_Enabled"""" -Type DWord -Value 1;
                     Remove-ItemProperty -Path """"HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR"""" -Name """"AllowGameDVR"""" -ErrorAction SilentlyContinue""");
            }
            else
            {
                SetCheckboxState("AbilitaXboxFeatures", false);
            }
            if (selection.Enable.Contains("Abilita Auto Manteinance"))
            {
                SetCheckboxState("AbilitaAutoManteinance", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
                        "MaintenanceDisabled", 0, RegistryValueKind.DWord, RegistryView.Registry64);
                    registryChanges.SetValue(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
                        "MaintenanceDisabled", 0, RegistryValueKind.DWord, RegistryView.Registry32);
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (UnauthorizedAccessException ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaAutoManteinance", false);
            }
            if (selection.Enable.Contains("Abilita Spazio Riservato"))
            {
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                SetCheckboxState("AbilitaSpazioRiservato", true);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\ReservedStorage", "ReservedStorageState", 1,
                            RegistryValueKind.DWord, view);
                    }
                    string script = "$ErrorActionPreference = 'Stop'" + Environment.NewLine
                        + "Set-WindowsReservedStorageState -State Enabled -Online -ErrorAction Stop" + Environment.NewLine
                        + registryChanges.BuildCommand();
                    RunElevatedPowerShellScript(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                SetCheckboxState("AbilitaSpazioRiservato", false);
            }
            if (selection.Enable.Contains("Abilita Tweaks Game DVR"))
            {
                SetCheckboxState("AbilitaTweaksGameDVR", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        foreach (string valueName in new[]
                        {
                            "GameDVR_DXGIHonorFSEWindowsCompatible",
                            "GameDVR_HonorUserFSEBehaviorMode",
                            "GameDVR_EFSEFeatureFlags",
                            "GameDVR_Enabled"
                        })
                        {
                            registryChanges.DeleteValue(RegistryHive.LocalMachine, @"SYSTEM\GameConfigStore", valueName, view);
                        }
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (UnauthorizedAccessException ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaTweaksGameDVR", false);
            }
            if (selection.Enable.Contains("Abilita Storie Attivita"))
            {
                SetCheckboxState("AbilitaStoriaAttivita", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        foreach (string valueName in new[] { "EnableActivityFeed", "PublishUserActivities", "UploadUserActivities" })
                        {
                            registryChanges.SetValue(RegistryHive.LocalMachine,
                                @"SOFTWARE\Policies\Microsoft\Windows\System", valueName, 1,
                                RegistryValueKind.DWord, view);
                        }
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (UnauthorizedAccessException ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaStoriaAttivita", false);
            }
            if (selection.Enable.Contains("Abilita Wifi-Sense"))
            {
                SetCheckboxState("AbilitaWifiSense", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    ElevatedRegistryMutationBatch registryChanges = new();
                    foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                    {
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"Software\Microsoft\PolicyManager\default\WiFi", "AllowWiFiHotSpotReporting", 1,
                            RegistryValueKind.DWord, view);
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"Software\Microsoft\PolicyManager\default\WiFi", "AllowAutoConnectToWiFiSenseHotspots", 1,
                            RegistryValueKind.DWord, view);
                        foreach (string policyName in new[] { "AllowWiFiHotSpotReporting", "AllowAutoConnectToWiFiSenseHotspots" })
                        {
                            registryChanges.SetValue(RegistryHive.LocalMachine,
                                $@"SOFTWARE\Microsoft\PolicyManager\default\WiFi\{policyName}", "Value", 1,
                                RegistryValueKind.DWord, view);
                        }
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config", "AutoConnectAllowedOEM", 1,
                            RegistryValueKind.DWord, view);
                        registryChanges.SetValue(RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config", "WiFISenseAllowed", 1,
                            RegistryValueKind.DWord, view);
                    }
                    ApplyElevatedRegistryMutations(registryChanges);
                }
                catch (UnauthorizedAccessException ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaStoriaAttivita", false);
            }
            if (selection.Enable.Contains("Abilita Notifiche Tray/Calendario"))
            {
                SetCheckboxState("AbilitaNotificheTrayCalendario", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                                                             .CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer"))
                    {
                        key64?.SetValue("DisableNotificationCenter", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                                                             .CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PushNotifications"))
                    {
                        key64?.SetValue("ToastEnabled", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                                                             .CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer"))
                    {
                        key32?.SetValue("DisableNotificationCenter", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                                                             .CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PushNotifications"))
                    {
                        key32?.SetValue("ToastEnabled", 1, RegistryValueKind.DWord);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
                catch (Exception ex)
                {
                    failures.Add(ex.GetBaseException().Message);
                }
            }
            else
            {
                SetCheckboxState("AbilitaNotificheTrayCalendario", false);
            }

            e.Result = failures;
        }

        private static void ConfigureServices(ElevatedRegistryMutationBatch registryChanges, params PrivacyServiceChange[] changes)
        {
            string encodedScript = PrivacyServiceScriptBuilder.BuildEncodedCommand(changes, registryChanges);
            RunElevatedPowerShellScript(encodedScript);
        }

        private static void ApplyElevatedRegistryMutations(ElevatedRegistryMutationBatch registryChanges, string? additionalScript = null)
        {
            ArgumentNullException.ThrowIfNull(registryChanges);
            if (registryChanges.Count == 0 && string.IsNullOrWhiteSpace(additionalScript))
                return;

            string script = registryChanges.Count > 0 ? registryChanges.BuildCommand() : "$ErrorActionPreference = 'Stop'";
            if (!string.IsNullOrWhiteSpace(additionalScript))
                script += Environment.NewLine + additionalScript;
            RunElevatedPowerShellScript(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
        }

        private static void RunElevatedPowerShellScript(string encodedScript)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };
            startInfo.ArgumentList.Add("-NoLogo");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-EncodedCommand");
            startInfo.ArgumentList.Add(encodedScript);

            using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException("Impossibile avviare la configurazione elevata dei servizi.");
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"La configurazione del servizio è terminata con codice {process.ExitCode}.");
        }

        private void AbilitaPrivacy_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                string itemName = AbilitaPrivacy.Items[e.Index]?.ToString() ?? string.Empty;
                string disabilitaName = itemName.Replace("Abilita", "Disabilita");

                int index = DisabilitaPrivacy.Items.IndexOf(disabilitaName);
                if (index >= 0)
                {
                    DisabilitaPrivacy.ItemCheck -= DisabilitaPrivacy_ItemCheck;
                    DisabilitaPrivacy.SetItemChecked(index, false);
                    DisabilitaPrivacy.ItemCheck += DisabilitaPrivacy_ItemCheck;
                }
            }
        }

        private void DisabilitaPrivacy_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            {
                if (e.NewValue == CheckState.Checked)
                {
                    string itemName = DisabilitaPrivacy.Items[e.Index]?.ToString() ?? string.Empty;
                    string abilitaName = itemName.Replace("Disabilita", "Abilita");

                    int index = AbilitaPrivacy.Items.IndexOf(abilitaName);
                    if (index >= 0)
                    {
                        AbilitaPrivacy.ItemCheck -= AbilitaPrivacy_ItemCheck;
                        AbilitaPrivacy.SetItemChecked(index, false);
                        AbilitaPrivacy.ItemCheck += AbilitaPrivacy_ItemCheck;
                    }
                }
            }
        }

        private void AbilitaPrivacy_MouseDown(object sender, MouseEventArgs e)
        {
            int index = AbilitaPrivacy.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                AbilitaPrivacy.SetItemChecked(index, !AbilitaPrivacy.GetItemChecked(index));
            }
            AbilitaPrivacy.ClearSelected();
        }

        private void DisabilitaPrivacy_MouseDown(object sender, MouseEventArgs e)
        {
            int index = DisabilitaPrivacy.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                DisabilitaPrivacy.SetItemChecked(index, !DisabilitaPrivacy.GetItemChecked(index));
            }
            DisabilitaPrivacy.ClearSelected();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < AbilitaPrivacy.Items.Count; i++)
            {
                AbilitaPrivacy.SetItemChecked(i, false);
            }
            for (int i = 0; i < DisabilitaPrivacy.Items.Count; i++)
            {
                DisabilitaPrivacy.SetItemChecked(i, false);
            }
        }
    }
}
