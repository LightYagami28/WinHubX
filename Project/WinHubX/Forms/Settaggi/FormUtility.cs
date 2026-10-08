using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.Json;
using WinHubX.Forms.Base;
using WinHubX.Impostazioni;

namespace WinHubX.Forms.Settaggi
{
    public partial class FormUtility : Form
    {
        private readonly Form1 form1;
        private readonly FormSettaggi formSettaggi;
        private readonly ConcurrentQueue<string> operationFailures = new();
        private int tIndex = -1;
        private int totalSteps = 0;
        private sealed record UtilitySelection(HashSet<string> Disable, HashSet<string> Enable);

        private static HashSet<string> SnapshotCheckedItems(CheckedListBox list)
        {
            var selectedItems = new HashSet<string>(StringComparer.Ordinal);
            foreach (object item in list.CheckedItems)
            {
                if (item is string name)
                    selectedItems.Add(name);
            }

            return selectedItems;
        }

        private void RecordOperationFailure(Exception exception)
        {
            operationFailures.Enqueue(exception.GetBaseException().Message);
            Debug.WriteLine(exception);
        }

        private string? GetOperationFailureSummary()
        {
            string[] failures = operationFailures.ToArray();
            if (failures.Length == 0)
                return null;

            int displayedFailureCount = Math.Min(failures.Length, 5);
            string details = string.Join(Environment.NewLine, failures, 0, displayedFailureCount);
            if (failures.Length > displayedFailureCount)
                details += Environment.NewLine + $"… e altri {failures.Length - displayedFailureCount} errori.";

            return $"Errori durante {failures.Length} operazioni:{Environment.NewLine}{details}";
        }
        public FormUtility(FormSettaggi formSettaggi, Form1 form1)
        {
            InitializeComponent();
            this.form1 = form1;
            this.formSettaggi = formSettaggi;
            LoadCheckboxStates();
            DisabilitaUtility.MouseMove += new MouseEventHandler(checkedListBox1_MouseMove);
            AbilitaUtility.MouseMove += new MouseEventHandler(checkedListBox2_MouseMove);
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
            int index = DisabilitaUtility.IndexFromPoint(e.Location);
            if (tIndex != index)
            {
                tIndex = index;
                if (tIndex > -1)
                {
                    string tooltipText = GetTooltipTextDisa(tIndex);
                    toolTip1.SetToolTip(DisabilitaUtility, tooltipText);
                }
            }
        }

        private void checkedListBox2_MouseMove(object? sender, MouseEventArgs e)
        {
            int index = AbilitaUtility.IndexFromPoint(e.Location);
            if (tIndex != index)
            {
                tIndex = index;
                if (tIndex > -1)
                {
                    string tooltipText = GetTooltipTextAbil(tIndex);
                    toolTip1.SetToolTip(AbilitaUtility, tooltipText);
                }
            }
        }

        private string GetTooltipTextDisa(int index)
        {
            return LanguageManager.GetTranslation("FormUtility", $"tooltipDisa_{index}");
        }
        private string GetTooltipTextAbil(int index)
        {
            return LanguageManager.GetTranslation("FormUtility", $"tooltipAbil_{index}");
        }

        private static void SetSystemVolumeIndexing(bool enabled)
        {
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory)
                ?? throw new InvalidOperationException("Impossibile individuare l'unità di Windows.");
            systemDrive = systemDrive.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string escapedDrive = systemDrive.Replace("'", "''", StringComparison.Ordinal);
            string enabledLiteral = enabled ? "$true" : "$false";
            string script = "$ErrorActionPreference='Stop'; " +
                $"$volume=Get-CimInstance -ClassName Win32_Volume -Filter \"DriveLetter='{escapedDrive}'\"; " +
                "if ($null -eq $volume) { throw 'Volume di Windows non trovato.' }; " +
                $"$volume | Set-CimInstance -Property @{{IndexingEnabled={enabledLiteral}}} | Out-Null";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                Verb = "runas"
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(script);

            using var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException("Impossibile avviare la modifica dell'indicizzazione.");
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Modifica dell'indicizzazione terminata con codice {process.ExitCode}.");
        }

        private static void RunSystemTool(string executableName, params string[] arguments)
        {
            string executablePath = Path.Combine(Environment.SystemDirectory, executableName);
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Impossibile avviare {executableName}.");
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"{executableName} è terminato con codice {process.ExitCode}.");
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
                if (key != null)
                {
                    object? value = key.GetValue(itemName);
                    if (value != null)
                    {
                        return (int)value == 1;
                    }
                }
            }
            return false;
        }
        private void btnSuggeriti_Click(object? sender, EventArgs e)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                       "WinHubX\\Computer\\osehardware.json");

            string tipoDisk = "";
            int ramGB = 8;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                HardwareInfo? info = JsonSerializer.Deserialize<HardwareInfo>(json);
                tipoDisk = info?.Hardware?.Disk ?? "";
                if (int.TryParse(info?.Hardware?.RAM?.Replace(" GB", ""), out int parsedRam))
                    ramGB = parsedRam;
            }

            bool isHDD = tipoDisk.Contains("HDD", StringComparison.OrdinalIgnoreCase);
            bool isSSD = tipoDisk.Contains("SSD", StringComparison.OrdinalIgnoreCase) || tipoDisk.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
            var daDisabilitare = new List<string>
    {
        "Disabilita Background App",
        "Disabilita Feedback",
        "Disabilita Advertising ID",
        "Disabilita Wifi Sense",
        "Disabilita News e Interessi",
        "Disabilita Mappe",
        "Disabilita UWP apps",
        "Disabilita Esperienze Personalizzate Microsoft",
        "Disabilita Ibernazione",
        "Disabilita Desktop Remoto"
    };
            if (ramGB < 8)
            {
                daDisabilitare.Add("Disabilita Superfetch");
                daDisabilitare.Add("Disabilita Storage Check");
            }
            if (isHDD)
            {
                daDisabilitare.Add("Disabilita Ottimizzazione FullScreen");
                daDisabilitare.Add("Disabilita Index File");
            }
            if (isSSD)
            {
                daDisabilitare.Add("Disabilita Superfetch");
                daDisabilitare.Add("Disabilita Index File");
                daDisabilitare.Add("Disabilita Avvio Rapido");
            }
            for (int i = 0; i < DisabilitaUtility.Items.Count; i++)
                DisabilitaUtility.SetItemChecked(i, false);
            foreach (string nome in daDisabilitare)
            {
                int index = DisabilitaUtility.Items.IndexOf(nome);
                if (index != -1)
                    DisabilitaUtility.SetItemChecked(index, true);
            }

            var daAbilitare = new List<string>
    {
        "Abilita Ottimizzazione FullScreen",
        "Abilita Risparmio Energetico Personalizzato",
        "Abilita attivazione del Numlock in avvio"
    };

            if (isHDD)
            {
                daAbilitare.Add("Abilita Superfetch");
                daAbilitare.Add("Abilita Avvio Rapido");
            }
            if (isSSD)
            {
                daAbilitare.Add("Migliora uso SSD");
            }
            for (int i = 0; i < AbilitaUtility.Items.Count; i++)
                AbilitaUtility.SetItemChecked(i, false);
            foreach (string nome in daAbilitare)
            {
                int index = AbilitaUtility.Items.IndexOf(nome);
                if (index != -1)
                    AbilitaUtility.SetItemChecked(index, true);
            }
        }

        private void LoadCheckboxStates()
        {
            var checkboxMappings = new (CheckedListBox box, string displayName, string regKey)[]
            {
        (DisabilitaUtility, "Disabilita Background App", "DisabilitaBackgroundApp"),
        (DisabilitaUtility, "Disabilita Feedback", "DisabilitaFeedback"),
        (DisabilitaUtility, "Disabilita Advertising ID", "DisabilitaAdvertisingID"),
        (DisabilitaUtility, "Disabilita Filtro Smart Screen", "DisabilitaFiltroSmartScreen"),
        (DisabilitaUtility, "Disabilita Wifi Sense", "DisabilitaWifiSense"),
        (DisabilitaUtility, "Disabilita Desktop Remoto", "DisabilitaDesktopRemoto"),
        (DisabilitaUtility, "Disabilita attivazione del Numlock in avvio", "DisabilitaattivazionedelNumlockinavvio"),
        (DisabilitaUtility, "Disabilita News e Interessi", "DisabilitaNewseInteressi"),
        (DisabilitaUtility, "Disabilita Index File", "DisabilitaIndexFile"),
        (DisabilitaUtility, "Disabilita Edge PDF", "DisabilitaEdgePDF"),
        (DisabilitaUtility, "Disabilita Mappe", "DisabilitaMappe"),
        (DisabilitaUtility, "Disabilita UWP apps", "DisabilitaUWPapps"),
        (DisabilitaUtility, "Disabilita Esperienze Personalizzate Microsoft", "DisabilitaEsperienzePersonalizzateMicrosoft"),
        (DisabilitaUtility, "Disabilita Storage Check", "DisabilitaStorageCheck"),
        (DisabilitaUtility, "Disabilita Superfetch", "DisabilitaSuperfetch"),
        (DisabilitaUtility, "Disabilita Ibernazione", "DisabilitaIbernazione"),
        (DisabilitaUtility, "Disabilita Ottimizzazione FullScreen", "DisabilitaOttimizzazioneFullScreen"),
        (DisabilitaUtility, "Disabilita Avvio Rapido", "DisabilitaAvvioRapido"),
        (DisabilitaUtility, "Normal Bandwidth", "NormalBandwidth"),
        (DisabilitaUtility, "Disabilita Migliora uso SSD", "DisabilitaMigliorausoSSD"),

        (AbilitaUtility, "All Bandwidth", "AllBandwidth"),
        (AbilitaUtility, "Abilita Storage Check", "AbilitaStorageCheck"),
        (AbilitaUtility, "Abilita Superfetch", "AbilitaSuperfetch"),
        (AbilitaUtility, "Abilita Ibernazione", "AbilitaIbernazione"),
        (AbilitaUtility, "Abilita Ottimizzazione FullScreen", "AbilitaOttimizzazioneFullScreen"),
        (AbilitaUtility, "Abilita Avvio Rapido", "AbilitaAvvioRapido"),
        (AbilitaUtility, "Abilita Background App", "AbilitaBackgroundApp"),
        (AbilitaUtility, "Abilita Feedback", "AbilitaFeedback"),
        (AbilitaUtility, "Abilita Advertising ID", "AbilitaAdvertisingID"),
        (AbilitaUtility, "Abilita Filtro Smart Screen", "AbilitaFiltroSmartScreen"),
        (AbilitaUtility, "Abilita Wifi Sense", "AbilitaWifiSense"),
        (AbilitaUtility, "Abilita Desktop Remoto", "AbilitaDesktopRemoto"),
        (AbilitaUtility, "Abilita Index File", "AbilitaIndexFile"),
        (AbilitaUtility, "Abilita attivazione del Numlock in avvio", "AbilitaattivazionedelNumlockinavvio"),
        (AbilitaUtility, "Abilita News e Interessi", "AbilitaNewseInteressi"),
        (AbilitaUtility, "Abilita Risparmio Energetico Personalizzato", "AbilitaRisparmioEnergeticoPersonalizzato"),
        (AbilitaUtility, "Abilita Mappe", "AbilitaMappe"),
        (AbilitaUtility, "Abilita UWP apps", "AbilitaUWPapps"),
        (AbilitaUtility, "Abilita Esperienze Personalizzate Microsoft", "AbilitaEsperienzePersonalizzateMicrosoft"),
        (AbilitaUtility, "Abilita Migliora uso SSD", "MigliorausoSSD")
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

        private void btnAvviaSelezionatiUti_Click(object? sender, EventArgs e)
        {
            var selection = new UtilitySelection(
                SnapshotCheckedItems(DisabilitaUtility),
                SnapshotCheckedItems(AbilitaUtility)
            );
            totalSteps = selection.Disable.Count + selection.Enable.Count;
            if (totalSteps == 0) totalSteps = 1;
            progressBar1.MaxValue = totalSteps;
            progressBar1.Value = 0;

            if (!backgroundWorker1.IsBusy)
            {
                while (operationFailures.TryDequeue(out _))
                {
                }

                backgroundWorker1.RunWorkerAsync(selection);
            }
        }

        private static void RestartExplorer()
        {
            _ = System.Diagnostics.Process.Start("explorer.exe");
        }

        private void DisableScheduledTask(string taskPath)
        {
            try
            {
                using var taskService = new Microsoft.Win32.TaskScheduler.TaskService();
                var task = taskService.GetTask(taskPath);
                if (task != null)
                {
                    task.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                RecordOperationFailure(new InvalidOperationException(
                    $"Impossibile disabilitare l’attività pianificata '{taskPath}'.",
                    ex
                ));
            }
        }

        private void SetRegistryValue(string path, string name, object? value, RegistryView view = RegistryView.Default)
        {
            if (value is null)
                return;

            RegistryKey baseKey = view == RegistryView.Registry64 ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view) : Registry.LocalMachine;

            using (var key = baseKey.OpenSubKey(path, true))
            {
                key?.SetValue(name, value, RegistryValueKind.DWord);
            }
        }

        private void DeleteRegistryKey(string path, string name, RegistryView view = RegistryView.Default)
        {
            RegistryKey baseKey = view == RegistryView.Registry64 ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view) : Registry.LocalMachine;

            using (var key = baseKey.OpenSubKey(path, true))
            {
                key?.DeleteValue(name, false);
            }
        }

        private void RunPowerShellCommands(string[] commands)
        {
            var commandString = string.Join("; ", commands);
            var startInfo = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                Verb = "runas"
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(commandString);

            using (var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException("Impossibile avviare il comando elevato."))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"PowerShell terminato con codice {process.ExitCode}.");
            }
        }
        private void EnableScheduledTask(string taskName)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    Verb = "runas"
                };
                startInfo.ArgumentList.Add("/Change");
                startInfo.ArgumentList.Add("/TN");
                startInfo.ArgumentList.Add(taskName);
                startInfo.ArgumentList.Add("/ENABLE");

                using (var process = System.Diagnostics.Process.Start(startInfo)
                    ?? throw new InvalidOperationException("Impossibile avviare schtasks."))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                        throw new InvalidOperationException($"schtasks.exe è terminato con codice {process.ExitCode} per l’attività '{taskName}'.");
                }
            }
            catch (Exception ex)
            {
                RecordOperationFailure(new InvalidOperationException(
                    $"Impossibile riattivare l’attività pianificata '{taskName}'.",
                    ex
                ));
            }
        }
        private void RemoveDisabledProperties(RegistryKey backgroundAppsKey)
        {
            foreach (string subKeyName in backgroundAppsKey.GetSubKeyNames())
            {
                if (subKeyName.StartsWith("Microsoft.Windows.Cortana"))
                {
                    continue;
                }

                using (RegistryKey? subKey = backgroundAppsKey.OpenSubKey(subKeyName, true))
                {
                    if (subKey != null)
                    {
                        if (subKey.GetValue("Disabled") != null)
                        {
                            subKey.DeleteValue("Disabled", false);
                        }

                        if (subKey.GetValue("DisabledByUser") != null)
                        {
                            subKey.DeleteValue("DisabledByUser", false);
                        }
                    }
                }
            }
        }

        private void backgroundWorker1_DoWork(object? sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (e.Argument is not UtilitySelection selection)
            {
                e.Cancel = true;
                return;
            }

            HashSet<string> selectedToDisable = selection.Disable;
            HashSet<string> selectedToEnable = selection.Enable;
            int currentStep = 0;
            if (selectedToDisable.Contains("Disabilita Background App"))
            {
                SetCheckboxState("DisabilitaBackgroundApp", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key64 = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                    {
                        key64?.SetValue("GlobalUserDisabled", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key32 = Registry.CurrentUser.OpenSubKey(@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                    {
                        key32?.SetValue("GlobalUserDisabled", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? baseKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                    {
                        if (baseKey != null)
                        {
                            foreach (string subKeyName in baseKey.GetSubKeyNames())
                            {
                                if (!subKeyName.StartsWith("Microsoft.Windows.Cortana"))
                                {
                                    using (RegistryKey? subKey = baseKey.OpenSubKey(subKeyName, true))
                                    {
                                        subKey?.SetValue("Disabled", 1, RegistryValueKind.DWord);
                                        subKey?.SetValue("DisabledByUser", 1, RegistryValueKind.DWord);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(new InvalidOperationException("Modifica delle app in background non riuscita.", ex));
                }
            }
            else
            {
                SetCheckboxState("DisabilitaBackgroundApp", false);
            }
            if (selectedToDisable.Contains("Disabilita Feedback"))
            {
                SetCheckboxState("DisabilitaFeedback", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\Siuf\Rules", true))
                    {
                        key32?.SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Siuf\Rules", true))
                    {
                        key64?.SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key32LM = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true))
                    {
                        key32LM?.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key64LM = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true))
                    {
                        key64LM?.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);
                    }
                    DisableScheduledTask(@"Microsoft\Windows\Feedback\Siuf\DmClient");
                    DisableScheduledTask(@"Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload");
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaFeedback", false);
            }
            if (selectedToDisable.Contains("Disabilita Advertising ID"))
            {
                SetCheckboxState("DisabilitaAdvertisingID", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", true))
                    {
                        key32?.SetValue("DisabledByGroupPolicy", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", true))
                    {
                        key64?.SetValue("DisabledByGroupPolicy", 1, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(new InvalidOperationException("Modifica Advertising ID non riuscita.", ex));
                }
            }
            else
            {
                SetCheckboxState("DisabilitaAdvertisingID", false);
            }
            if (selectedToDisable.Contains("Disabilita Filtro Smart Screen"))
            {
                SetCheckboxState("DisabilitaFiltroSmartScreen", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key32System = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", true))
                    {
                        key32System?.SetValue("EnableSmartScreen", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key64System = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", true))
                    {
                        key64System?.SetValue("EnableSmartScreen", 0, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key32Edge = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", true))
                    {
                        key32Edge?.SetValue("EnabledV9", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key64Edge = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", true))
                    {
                        key64Edge?.SetValue("EnabledV9", 0, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaFiltroSmartScreen", false);
            }
            if (selectedToDisable.Contains("Disabilita Desktop Remoto"))
            {
                SetCheckboxState("DisabilitaDesktopRemoto", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key32TSConnections = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true))
                    {
                        key32TSConnections?.SetValue("fDenyTSConnections", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key64TSConnections = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true))
                    {
                        key64TSConnections?.SetValue("fDenyTSConnections", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key32UserAuth = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", true))
                    {
                        key32UserAuth?.SetValue("UserAuthentication", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey? key64UserAuth = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", true))
                    {
                        key64UserAuth?.SetValue("UserAuthentication", 1, RegistryValueKind.DWord);
                    }


                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaDesktopRemoto", false);
            }
            if (selectedToDisable.Contains("Disabilita attivazione del Numlock in avvio"))
            {
                SetCheckboxState("DisabilitaattivazionedelNumlockinavvio", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry32).OpenSubKey(@".DEFAULT\Control Panel\Keyboard", true))
                    {
                        key32?.SetValue("InitialKeyboardIndicators", 2147483648, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64).OpenSubKey(@".DEFAULT\Control Panel\Keyboard", true))
                    {
                        key64?.SetValue("InitialKeyboardIndicators", 2147483648, RegistryValueKind.DWord);
                    }
                    if (Control.IsKeyLocked(Keys.NumLock))
                    {
                        SendKeys.SendWait("{NUMLOCK}");
                    }


                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaattivazionedelNumlockinavvio", false);
            }
            if (selectedToDisable.Contains("Disabilita News e Interessi"))
            {
                SetCheckboxState("DisabilitaNewseInteressi", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {

                    using (Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/IM explorer.exe /F",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }) ?? throw new InvalidOperationException("Impossibile riavviare Explorer."))
                    {
                        process.WaitForExit();
                    }

                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Feeds"))
                    {
                        key32?.SetValue("ShellFeedsTaskbarViewMode", 2, RegistryValueKind.DWord);
                        key32?.SetValue("IsFeedsAvailable", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Feeds"))
                    {
                        key64?.SetValue("ShellFeedsTaskbarViewMode", 2, RegistryValueKind.DWord);
                        key64?.SetValue("IsFeedsAvailable", 0, RegistryValueKind.DWord);
                    }
                    using (RegistryKey keyLM32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"))
                    {
                        keyLM32?.SetValue("EnableFeeds", 0, RegistryValueKind.DWord);
                    }

                    using (RegistryKey keyLM64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"))
                    {
                        keyLM64?.SetValue("EnableFeeds", 0, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaNewseInteressi", false);
            }
            if (selectedToDisable.Contains("Disabilita Index File"))
            {
                SetCheckboxState("DisabilitaIndexFile", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetSystemVolumeIndexing(enabled: false);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(new InvalidOperationException("Disattivazione indicizzazione non riuscita.", ex));
                }
            }
            else
            {
                SetCheckboxState("DisabilitaIndexFile", false);
            }
            if (selectedToDisable.Contains("Disabilita Edge PDF"))
            {
                SetCheckboxState("DisabilitaEdgePDF", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    string pdfKeyPath = @"Software\Classes\.pdf";
                    string openWithProgidsPath = @"Software\Classes\.pdf\OpenWithProgids";
                    string openWithListPath = @"Software\Classes\.pdf\OpenWithList";
                    using (RegistryKey pdfKey32 = Registry.CurrentUser.CreateSubKey(pdfKeyPath))
                    {
                        if (pdfKey32 != null)
                        {
                            pdfKey32.SetValue("NoOpenWith", "", RegistryValueKind.String);
                            pdfKey32.SetValue("NoStaticDefaultVerb", "", RegistryValueKind.String);
                        }
                    }

                    using (RegistryKey openWithProgidsKey32 = Registry.CurrentUser.CreateSubKey(openWithProgidsPath))
                    {
                        if (openWithProgidsKey32 != null)
                        {
                            openWithProgidsKey32.SetValue("NoOpenWith", "", RegistryValueKind.String);
                            openWithProgidsKey32.SetValue("NoStaticDefaultVerb", "", RegistryValueKind.String);
                        }
                    }

                    using (RegistryKey openWithListKey32 = Registry.CurrentUser.CreateSubKey(openWithListPath))
                    {
                        if (openWithListKey32 != null)
                        {
                            openWithListKey32.SetValue("NoOpenWith", "", RegistryValueKind.String);
                            openWithListKey32.SetValue("NoStaticDefaultVerb", "", RegistryValueKind.String);
                        }
                    }
                    string edgeKeyPath = @"Software\Classes\AppXd4nrz8ff68srnhf9t5a8sbjyar1cr723_";

                    using (RegistryKey edgeKey64 = Registry.CurrentUser.CreateSubKey(edgeKeyPath))
                    {
                        edgeKey64?.SetValue("AppXd4nrz8ff68srnhf9t5a8sbjyar1cr723_", "", RegistryValueKind.String);
                    }


                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaEdgePDF", false);
            }
            if (selectedToDisable.Contains("Disabilita Mappe"))
            {
                SetCheckboxState("DisabilitaMappe", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey key32 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Maps"))
                    {
                        key32?.SetValue("AutoUpdateEnabled", 0, RegistryValueKind.DWord);
                    }
                    using (RegistryKey key64 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Maps"))
                    {
                        key64?.SetValue("AutoUpdateEnabled", 0, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaMappe", false);
            }
            if (selectedToDisable.Contains("Disabilita UWP apps"))
            {
                SetCheckboxState("DisabilitaUWPapps", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    Version osVersion = Environment.OSVersion.Version;
                    if (osVersion.Build >= 17763)
                    {
                        using (RegistryKey appPrivacyKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"))
                        {
                            if (appPrivacyKey != null)
                            {
                                appPrivacyKey.SetValue("LetAppsRunInBackground", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsActivateWithVoice", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsActivateWithVoiceAboveLock", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessNotifications", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessAccountInfo", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessContacts", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessCalendar", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessPhone", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessCallHistory", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessEmail", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessTasks", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessMessaging", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsAccessRadios", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsSyncWithDevices", 2, RegistryValueKind.DWord);
                                appPrivacyKey.SetValue("LetAppsGetDiagnosticInfo", 2, RegistryValueKind.DWord);
                            }
                        }
                        using (RegistryKey capabilityAccessKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore"))
                        {
                            if (capabilityAccessKey != null)
                            {
                                capabilityAccessKey.CreateSubKey("documentsLibrary")?.SetValue("Value", "Deny", RegistryValueKind.String);
                                capabilityAccessKey.CreateSubKey("picturesLibrary")?.SetValue("Value", "Deny", RegistryValueKind.String);
                                capabilityAccessKey.CreateSubKey("videosLibrary")?.SetValue("Value", "Deny", RegistryValueKind.String);
                                capabilityAccessKey.CreateSubKey("broadFileSystemAccess")?.SetValue("Value", "Deny", RegistryValueKind.String);
                            }
                        }
                        using (RegistryKey memoryManagementKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                        {
                            memoryManagementKey?.SetValue("SwapfileControl", 0, RegistryValueKind.DWord);
                        }
                    }
                    else
                    {
                        using (RegistryKey? backgroundAccessKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                        {
                            if (backgroundAccessKey != null)
                            {
                                foreach (var subKey in backgroundAccessKey.GetSubKeyNames())
                                {
                                    if (!subKey.StartsWith("Microsoft.Windows.Cortana") && !subKey.StartsWith("Microsoft.Windows.ShellExperienceHost"))
                                    {
                                        using (var appKey = backgroundAccessKey.OpenSubKey(subKey, true))
                                        {
                                            appKey?.SetValue("Disabled", 1, RegistryValueKind.DWord);
                                            appKey?.SetValue("DisabledByUser", 1, RegistryValueKind.DWord);
                                        }
                                    }
                                }
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaUWPapps", false);
            }
            if (selectedToDisable.Contains("Disabilita Esperienze Personalizzate Microsoft"))
            {
                SetCheckboxState("DisabilitaEsperienzePersonalizzateMicrosoft", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (var systemKey64 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        if (systemKey64 != null)
                        {
                            systemKey64.SetValue("EnableCdp", 0, RegistryValueKind.DWord);
                            systemKey64.SetValue("EnableMmx", 0, RegistryValueKind.DWord);
                        }
                    }
                    using (var systemKey32 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\Windows\System"))
                    {
                        if (systemKey32 != null)
                        {
                            systemKey32.SetValue("EnableCdp", 0, RegistryValueKind.DWord);
                            systemKey32.SetValue("EnableMmx", 0, RegistryValueKind.DWord);
                        }
                    }
                    using (var cloudContentKey64 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                    {
                        cloudContentKey64?.SetValue("DisableTailoredExperiencesWithDiagnosticData", 1, RegistryValueKind.DWord);
                    }

                    using (var cloudContentKey32 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\Windows\CloudContent"))
                    {
                        cloudContentKey32?.SetValue("DisableTailoredExperiencesWithDiagnosticData", 1, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaEsperienzePersonalizzateMicrosoft", false);
            }
            if (selectedToDisable.Contains("Disabilita Storage Check"))
            {
                SetCheckboxState("DisabilitaStorageCheck", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                    {
                        key64.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy", false);
                    }
                    using (RegistryKey key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
                    {
                        key32.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy", false);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaStorageCheck", false);
            }
            if (selectedToDisable.Contains("Disabilita Superfetch"))
            {
                SetCheckboxState("DisabilitaSuperfetch", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Services\SysMain", "Start", 4);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaSuperfetch", false);
            }
            if (selectedToDisable.Contains("Disabilita Storage Check"))
            {
                SetCheckboxState("DisabilitaStorageCheck", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    DeleteRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters", "StoragePolicy", RegistryView.Registry32);
                    DeleteRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters", "StoragePolicy", RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaStorageCheck", false);
            }
            if (selectedToDisable.Contains("Disabilita Ibernazione"))
            {
                SetCheckboxState("DisabilitaIbernazione", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HibernateEnabled", 0);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowHibernateOption", 0);
                    RunSystemTool("powercfg.exe", "/hibernate", "off");
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaIbernazione", false);
            }
            if (selectedToDisable.Contains("Disabilita Ottimizzazione FullScreen"))
            {
                SetCheckboxState("DisabilitaOttimizzazioneFullScreen", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1, RegistryView.Registry32);
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1, RegistryView.Registry64);
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_FSEBehavior", 2, RegistryView.Registry32);
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_FSEBehavior", 2, RegistryView.Registry64);
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, RegistryView.Registry32);
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, RegistryView.Registry64);
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryView.Registry32);
                    SetRegistryValue(@"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaOttimizzazioneFullScreen", false);
            }
            if (selectedToDisable.Contains("Disabilita Avvio Rapido"))
            {
                SetCheckboxState("DisabilitaAvvioRapido", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaAvvioRapido", false);
            }
            if (selectedToDisable.Contains("Normal Bandwidth"))
            {
                SetCheckboxState("NormalBandwidth", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    DeleteRegistryKey(@"SOFTWARE\Policies\Microsoft\Psched", "NonBestEffortLimit", RegistryView.Registry32);
                    DeleteRegistryKey(@"SOFTWARE\Policies\Microsoft\Psched", "NonBestEffortLimit", RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("NormalBandwidth", false);
            }
            if (selectedToDisable.Contains("Disabilita Migliora uso SSD"))
            {
                SetCheckboxState("DisabilitaMigliorausoSSD", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "DisableLastAccess", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "DisableLastAccess", 0, RegistryView.Registry64);

                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "EncryptPagingFile", 1, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "EncryptPagingFile", 1, RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("DisabilitaMigliorausoSSD", false);
            }
            if (selectedToEnable.Contains("Abilita Storage Check"))
            {
                SetCheckboxState("AbilitaStorageCheck", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    string storagePolicyKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy";
                    using (RegistryKey? key32 = Registry.CurrentUser.OpenSubKey(storagePolicyKey, true))
                    {
                        if (key32 != null)
                        {
                            key32.SetValue("01", 1, RegistryValueKind.DWord);
                            key32.SetValue("04", 1, RegistryValueKind.DWord);
                            key32.SetValue("08", 1, RegistryValueKind.DWord);
                            key32.SetValue("32", 0, RegistryValueKind.DWord);
                            key32.SetValue("StoragePoliciesNotified", 1, RegistryValueKind.DWord);
                        }
                        else
                        {

                        }
                    }
                    using (RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(storagePolicyKey, true))
                    {
                        if (key64 != null)
                        {
                            key64.SetValue("01", 1, RegistryValueKind.DWord);
                            key64.SetValue("04", 1, RegistryValueKind.DWord);
                            key64.SetValue("08", 1, RegistryValueKind.DWord);
                            key64.SetValue("32", 0, RegistryValueKind.DWord);
                            key64.SetValue("StoragePoliciesNotified", 1, RegistryValueKind.DWord);
                        }
                        else
                        {

                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    RecordOperationFailure(ex);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaStorageCheck", false);
            }
            if (selectedToEnable.Contains("Abilita Superfetch"))
            {
                SetCheckboxState("AbilitaSuperfetch", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Services\SysMain", "Start", 2, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Services\SysMain", "Start", 2, RegistryView.Registry64);
                    RunSystemTool("sc.exe", "start", "SysMain");
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaSuperfetch", false);
            }
            if (selectedToEnable.Contains("Abilita Ibernazione"))
            {
                SetCheckboxState("AbilitaIbernazione", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HibernateEnabled", 1, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HibernateEnabled", 1, RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowHibernateOption", 1, RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowHibernateOption", 1, RegistryView.Registry64);
                    RunSystemTool("powercfg.exe", "/hibernate", "on");
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaIbernazione", false);
            }
            if (selectedToEnable.Contains("Abilita Ottimizzazione FullScreen"))
            {
                SetCheckboxState("AbilitaOttimizzazioneFullScreen", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 0, RegistryView.Registry64);
                    DeleteRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_FSEBehavior", RegistryView.Registry32);
                    DeleteRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_FSEBehavior", RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_FSEBehaviorMode", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_FSEBehaviorMode", 0, RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 0, RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaOttimizzazioneFullScreen", false);
            }
            if (selectedToEnable.Contains("Abilita Avvio Rapido"))
            {
                SetCheckboxState("AbilitaAvvioRapido", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 1, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 1, RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaAvvioRapido", false);
            }
            if (selectedToEnable.Contains("All Bandwidth"))
            {
                SetCheckboxState("AllBandwidth", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SOFTWARE\Policies\Microsoft\Psched", "NonBestEffortLimit", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Policies\Microsoft\Psched", "NonBestEffortLimit", 0, RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AllBandwidth", false);
            }
            if (selectedToEnable.Contains("Abilita Background App"))
            {
                SetCheckboxState("AbilitaBackgroundApp", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);

                try
                {
                    RegistryView view32 = RegistryView.Registry32;
                    RegistryView view64 = RegistryView.Registry64;
                    using (RegistryKey? backgroundAppsKey32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view32).OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                    using (RegistryKey? backgroundAppsKey64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view64).OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                    {
                        if (backgroundAppsKey32 != null)
                        {
                            backgroundAppsKey32.SetValue("GlobalUserDisabled", 0, RegistryValueKind.DWord);
                            RemoveDisabledProperties(backgroundAppsKey32);
                        }

                        if (backgroundAppsKey64 != null)
                        {
                            backgroundAppsKey64.SetValue("GlobalUserDisabled", 0, RegistryValueKind.DWord);
                            RemoveDisabledProperties(backgroundAppsKey64);
                        }
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaBackgroundApp", false);
            }

            if (selectedToEnable.Contains("Abilita Feedback"))
            {
                SetCheckboxState("AbilitaFeedback", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? rulesKey32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                        .OpenSubKey(@"SOFTWARE\Microsoft\Siuf\Rules", true))
                    {
                        if (rulesKey32 != null && rulesKey32.GetValue("NumberOfSIUFInPeriod") != null)
                        {
                            rulesKey32.DeleteValue("NumberOfSIUFInPeriod", false);
                        }
                    }
                    using (RegistryKey? dataCollectionKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true))
                    {
                        if (dataCollectionKey64 != null && dataCollectionKey64.GetValue("DoNotShowFeedbackNotifications") != null)
                        {
                            dataCollectionKey64.DeleteValue("DoNotShowFeedbackNotifications", false);
                        }
                    }
                    EnableScheduledTask("Microsoft\\Windows\\Feedback\\Siuf\\DmClient");
                    EnableScheduledTask("Microsoft\\Windows\\Feedback\\Siuf\\DmClientOnScenarioDownload");
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaFeedback", false);
            }
            if (selectedToEnable.Contains("Abilita Advertising ID"))
            {
                SetCheckboxState("AbilitaAdvertisingID", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? advertisingKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                        .OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", true))
                    {
                        if (advertisingKey32 != null)
                        {
                            if (advertisingKey32.GetValue("DisabledByGroupPolicy") != null)
                            {
                                advertisingKey32.DeleteValue("DisabledByGroupPolicy", false);
                            }
                        }
                    }
                    using (RegistryKey? advertisingKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", true))
                    {
                        if (advertisingKey64 != null)
                        {
                            if (advertisingKey64.GetValue("DisabledByGroupPolicy") != null)
                            {
                                advertisingKey64.DeleteValue("DisabledByGroupPolicy", false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaAdvertisingID", false);
            }
            if (selectedToEnable.Contains("Abilita Filtro Smart Screen"))
            {
                SetCheckboxState("AbilitaFiltroSmartScreen", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (RegistryKey? systemKey32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", true))
                    {
                        systemKey32?.DeleteValue("EnableSmartScreen", false);
                    }

                    using (RegistryKey? edgeKey32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", true))
                    {
                        edgeKey32?.DeleteValue("EnabledV9", false);
                    }
                    using (RegistryKey? systemKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", true))
                    {
                        systemKey64?.DeleteValue("EnableSmartScreen", false);
                    }

                    using (RegistryKey? edgeKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey(@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", true))
                    {
                        edgeKey64?.DeleteValue("EnabledV9", false);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaFiltroSmartScreen", false);
            }
            if (selectedToEnable.Contains("Abilita Desktop Remoto"))
            {
                SetCheckboxState("AbilitaDesktopRemoto", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0, RegistryView.Registry64);

                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", 0, RegistryView.Registry64);
                    string[] commands = new[]
                    {
            "Enable-NetFirewallRule -Name \"RemoteDesktop*\""
        };
                    RunPowerShellCommands(commands);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaDesktopRemoto", false);
            }
            if (selectedToEnable.Contains("Abilita attivazione del Numlock in avvio"))
            {
                SetCheckboxState("AbilitaattivazionedelNumlockinavvio", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"HKEY_USERS\.DEFAULT\Control Panel\Keyboard", "InitialKeyboardIndicators", 2147483650, RegistryView.Registry32);
                    SetRegistryValue(@"HKEY_USERS\.DEFAULT\Control Panel\Keyboard", "InitialKeyboardIndicators", 2147483650, RegistryView.Registry64);
                    string[] commands = new[]
                    {
            "Add-Type -AssemblyName System.Windows.Forms",
            "If (!([System.Windows.Forms.Control]::IsKeyLocked('NumLock'))) { $wsh = New-Object -ComObject WScript.Shell; $wsh.SendKeys('{NUMLOCK}') }"
        };
                    RunPowerShellCommands(commands);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaattivazionedelNumlockinavvio", false);
            }
            if (selectedToEnable.Contains("Abilita News e Interessi"))
            {
                SetCheckboxState("AbilitaNewseInteressi", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    using (Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/IM explorer.exe /F",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }) ?? throw new InvalidOperationException("Impossibile riavviare Explorer."))
                    {
                        process.WaitForExit();
                    }
                    using (RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Feeds"))
                    {
                        key32?.SetValue("ShellFeedsTaskbarViewMode", 1, RegistryValueKind.DWord);
                        key32?.SetValue("IsFeedsAvailable", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Feeds"))
                    {
                        key64?.SetValue("ShellFeedsTaskbarViewMode", 1, RegistryValueKind.DWord);
                        key64?.SetValue("IsFeedsAvailable", 1, RegistryValueKind.DWord);
                    }
                    using (RegistryKey keyLM32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"))
                    {
                        keyLM32?.SetValue("EnableFeeds", 1, RegistryValueKind.DWord);
                    }

                    using (RegistryKey keyLM64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"))
                    {
                        keyLM64?.SetValue("EnableFeeds", 1, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaNewseInteressi", false);
            }
            if (selectedToEnable.Contains("Abilita Index File"))
            {
                SetCheckboxState("AbilitaIndexFile", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);

                try
                {
                    SetSystemVolumeIndexing(enabled: true);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(new InvalidOperationException("Attivazione indicizzazione non riuscita.", ex));
                }
            }
            else
            {
                SetCheckboxState("AbilitaIndexFile", false);
            }
            if (selectedToEnable.Contains("Abilita Risparmio Energetico Personalizzato"))
            {
                SetCheckboxState("AbilitaRisparmioEnergeticoPersonalizzato", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = "powercfg.exe",
                        UseShellExecute = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        Verb = "runas"
                    };
                    startInfo.ArgumentList.Add("-duplicatescheme");
                    startInfo.ArgumentList.Add("e9a42b02-d5df-448d-aa00-03f14749eb61");

                    using (var process = System.Diagnostics.Process.Start(startInfo)
                        ?? throw new InvalidOperationException("Impossibile avviare il processo di risparmio energetico."))
                    {
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            throw new InvalidOperationException($"powercfg terminato con codice {process.ExitCode}.");
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(new InvalidOperationException("Creazione del profilo energetico non riuscita.", ex));
                }
            }
            else
            {
                SetCheckboxState("AbilitaRisparmioEnergeticoPersonalizzato", false);
            }
            if (selectedToEnable.Contains("Abilita Migliora uso SSD"))
            {
                SetCheckboxState("MigliorausoSSD", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "DisableLastAccess", 1, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "DisableLastAccess", 1, RegistryView.Registry64);

                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "EncryptPagingFile", 0, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "EncryptPagingFile", 0, RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("MigliorausoSSD", false);
            }

            if (selectedToEnable.Contains("Abilita Mappe"))
            {
                SetCheckboxState("AbilitaMappe", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\Maps", "AutoUpdateEnabled", null, RegistryView.Registry32);
                    SetRegistryValue(@"SYSTEM\Maps", "AutoUpdateEnabled", null, RegistryView.Registry64);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaMappe", false);
            }
            if (selectedToEnable.Contains("Abilita UWP apps"))
            {
                SetCheckboxState("AbilitaUWPapps", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "SwapfileControl", null, RegistryView.Registry64);
                    SetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "SwapfileControl", null, RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary", "Value", "Allow", RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary", "Value", "Allow", RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary", "Value", "Allow", RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary", "Value", "Allow", RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary", "Value", "Allow", RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary", "Value", "Allow", RegistryView.Registry32);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess", "Value", "Allow", RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess", "Value", "Allow", RegistryView.Registry32);
                    string appPrivacyPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy";
                    string[] propertiesToRemove = {
                "LetAppsGetDiagnosticInfo",
                "LetAppsSyncWithDevices",
                "LetAppsAccessRadios",
                "LetAppsAccessMessaging",
                "LetAppsAccessTasks",
                "LetAppsAccessEmail",
                "LetAppsAccessCallHistory",
                "LetAppsAccessPhone",
                "LetAppsAccessCalendar",
                "LetAppsAccessContacts",
                "LetAppsAccessAccountInfo",
                "LetAppsAccessNotifications",
                "LetAppsActivateWithVoice",
                "LetAppsActivateWithVoiceAboveLock",
                "LetAppsRunInBackground"
            };

                    foreach (string property in propertiesToRemove)
                    {
                        SetRegistryValue(appPrivacyPath, property, null, RegistryView.Registry64);
                        SetRegistryValue(appPrivacyPath, property, null, RegistryView.Registry32);
                    }
                    using (RegistryKey? backgroundAccessKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                    {
                        if (backgroundAccessKey != null)
                        {
                            foreach (var subkeyName in backgroundAccessKey.GetSubKeyNames())
                            {
                                using (var subkey = backgroundAccessKey.OpenSubKey(subkeyName, true))
                                {
                                    if (subkey != null)
                                    {
                                        subkey.DeleteValue("Disabled", false);
                                        subkey.DeleteValue("DisabledByUser", false);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaUWPapps", false);
            }

            if (selectedToEnable.Contains("Abilita Esperienze Personalizzate Microsoft"))
            {
                SetCheckboxState("AbilitaEsperienzePersonalizzateMicrosoft", true);
                currentStep++;
                backgroundWorker1.ReportProgress(currentStep);
                try
                {
                    SetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableTailoredExperiencesWithDiagnosticData", null, RegistryView.Registry64);
                    SetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableTailoredExperiencesWithDiagnosticData", null, RegistryView.Registry32);
                }
                catch (Exception ex)
                {
                    RecordOperationFailure(ex);
                }
            }
            else
            {
                SetCheckboxState("AbilitaEsperienzePersonalizzateMicrosoft", false);
            }
        }

        private void backgroundWorker1_ProgressChanged(object? sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Value = Math.Min(e.ProgressPercentage, progressBar1.MaxValue);
        }

        private void backgroundWorker1_RunWorkerCompleted(object? sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error is not null)
            {
                string errorMessage = LanguageManager.GetTranslation("Global", "erroreoperazione");
                string details = e.Error.GetBaseException().Message;
                string? recordedFailures = GetOperationFailureSummary();
                if (recordedFailures is not null)
                    details += Environment.NewLine + recordedFailures;

                _ = MessageBox.Show(
                    $"{errorMessage}{Environment.NewLine}{details}",
                    "WinHubX",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            if (e.Cancelled)
            {
                _ = MessageBox.Show(
                    LanguageManager.GetTranslation("Global", "operazioneannullatatoken"),
                    "WinHubX",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            RestartExplorer();
            string? operationFailureSummary = GetOperationFailureSummary();
            if (operationFailureSummary is not null)
            {
                _ = MessageBox.Show(
                    $"{LanguageManager.GetTranslation("Global", "erroreoperazione")}{Environment.NewLine}{operationFailureSummary}",
                    "WinHubX",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
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

        private void AbilitaUtility_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                string itemName = AbilitaUtility.Items[e.Index]?.ToString() ?? string.Empty;
                string disabilitaName = itemName.Replace("Abilita", "Disabilita");

                int index = DisabilitaUtility.Items.IndexOf(disabilitaName);
                if (index >= 0)
                {
                    DisabilitaUtility.ItemCheck -= DisabilitaUtility_ItemCheck;
                    DisabilitaUtility.SetItemChecked(index, false);
                    DisabilitaUtility.ItemCheck += DisabilitaUtility_ItemCheck;
                }
            }
        }

        private void DisabilitaUtility_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                string itemName = DisabilitaUtility.Items[e.Index]?.ToString() ?? string.Empty;
                string abilitaName = itemName.Replace("Disabilita", "Abilita");

                int index = AbilitaUtility.Items.IndexOf(abilitaName);
                if (index >= 0)
                {
                    AbilitaUtility.ItemCheck -= AbilitaUtility_ItemCheck;
                    AbilitaUtility.SetItemChecked(index, false);
                    AbilitaUtility.ItemCheck += AbilitaUtility_ItemCheck;
                }
            }
        }

        private void AbilitaUtility_MouseDown(object? sender, MouseEventArgs e)
        {
            int index = AbilitaUtility.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                AbilitaUtility.SetItemChecked(index, !AbilitaUtility.GetItemChecked(index));
            }
            AbilitaUtility.ClearSelected();
        }

        private void DisabilitaUtility_MouseDown(object? sender, MouseEventArgs e)
        {
            int index = DisabilitaUtility.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                DisabilitaUtility.SetItemChecked(index, !DisabilitaUtility.GetItemChecked(index));
            }
            DisabilitaUtility.ClearSelected();
        }

        private void btnReset_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < AbilitaUtility.Items.Count; i++)
            {
                AbilitaUtility.SetItemChecked(i, false);
            }
            for (int i = 0; i < DisabilitaUtility.Items.Count; i++)
            {
                DisabilitaUtility.SetItemChecked(i, false);
            }
        }
    }
}
