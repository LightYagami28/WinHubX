$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $powershellPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $scriptArgument = '"{0}"' -f $PSCommandPath
    try {
        $elevatedProcess = Start-Process -FilePath $powershellPath `
            -ArgumentList @('-NoProfile', '-File', $scriptArgument) `
            -Verb RunAs -Wait -PassThru
        exit $elevatedProcess.ExitCode
    }
    catch {
        Write-Error "Impossibile avviare lo script WSL con privilegi amministrativi: $($_.Exception.Message)"
        exit 1
    }
}

# Hide Console
# Define the function to hide the console
Add-Type -Name Window -Namespace Console -MemberDefinition '
[DllImport("Kernel32.dll")]
public static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
public static extern bool ShowWindow(IntPtr hWnd, Int32 nCmdShow);
'

function Hide-Console {
    $consolePtr = [Console.Window]::GetConsoleWindow()
    [Console.Window]::ShowWindow($consolePtr, 0)
}

# Call the Hide-Console function
Hide-Console
####################################

####################################
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Create form
$form = New-Object System.Windows.Forms.Form
$form.Text = "Attivazione WSL"
$form.Size = New-Object System.Drawing.Size(350,200) 
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = 'FixedDialog'

# Set form background color to dark
$form.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 40)

# Set text color to white
$form.ForeColor = [System.Drawing.Color]::White

# Create x64 or x32
$groupBoxDistro = New-Object System.Windows.Forms.GroupBox
$groupBoxDistro.Location = New-Object System.Drawing.Point(10,20)
$groupBoxDistro.Size = New-Object System.Drawing.Size(290,100)
$groupBoxDistro.Text = "Seleziona quale distro Linux usare"
$groupBoxDistro.ForeColor = [System.Drawing.Color]::White
$form.Controls.Add($groupBoxDistro)

$radioButtonUbuntu = New-Object System.Windows.Forms.RadioButton
$radioButtonUbuntu.Location = New-Object System.Drawing.Point(10,20)
$radioButtonUbuntu.Size = New-Object System.Drawing.Size(120,20)
$radioButtonUbuntu.Text = "Ubuntu"
$groupBoxDistro.Controls.Add($radioButtonUbuntu)

$radioButtonDebian = New-Object System.Windows.Forms.RadioButton
$radioButtonDebian.Location = New-Object System.Drawing.Point(10,45)
$radioButtonDebian.Size = New-Object System.Drawing.Size(120,20)
$radioButtonDebian.Text = "Debian"
$groupBoxDistro.Controls.Add($radioButtonDebian)

$radioButtonKaliLinux = New-Object System.Windows.Forms.RadioButton
$radioButtonKaliLinux.Location = New-Object System.Drawing.Point(140,20)
$radioButtonKaliLinux.Size = New-Object System.Drawing.Size(120,20)
$radioButtonKaliLinux.Text = "Kali Linux"
$groupBoxDistro.Controls.Add($radioButtonKaliLinux)

$radioButtonOpensuse = New-Object System.Windows.Forms.RadioButton
$radioButtonOpensuse.Location = New-Object System.Drawing.Point(140,45)
$radioButtonOpensuse.Size = New-Object System.Drawing.Size(120,20)
$radioButtonOpensuse.Text = "Opensuse"
$groupBoxDistro.Controls.Add($radioButtonOpensuse)

$radioButtonOracle = New-Object System.Windows.Forms.RadioButton
$radioButtonOracle.Location = New-Object System.Drawing.Point(66,75)
$radioButtonOracle.Size = New-Object System.Drawing.Size(120,20)
$radioButtonOracle.Text = "Oracle"
$groupBoxDistro.Controls.Add($radioButtonOracle)
    
# Create OK button
$buildButton = New-Object System.Windows.Forms.Button
$buildButton.Location = New-Object System.Drawing.Point(220,130) 
$buildButton.Size = New-Object System.Drawing.Size(100,23) 
$buildButton.Text = "Attiva WSL!"
$buildButton.Add_Click({

    if (-not ($radioButtonUbuntu.Checked -or $radioButtonDebian.Checked -or $radioButtonKaliLinux.Checked -or $radioButtonOpensuse.Checked -or $radioButtonOracle.Checked)) {
    [System.Windows.Forms.MessageBox]::Show("Seleziona la Distro desiderata.", "Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
     return
    }

    if ($radioButtonUbuntu.Checked) {
        $Distro = "Ubuntu"
    } elseif ($radioButtonDebian.Checked) {
        $Distro = "Debian"
    } elseif ($radioButtonKaliLinux.Checked) {
        $Distro = "KaliLinux"
    } elseif ($radioButtonOpensuse.Checked) {
        $Distro = "Opensuse"
    } elseif ($radioButtonOracle.Checked) {
        $Distro = "Oracle"
    }

    $distribution = switch ($Distro) {
        "Ubuntu" { "Ubuntu" }
        "Debian" { "Debian" }
        "KaliLinux" { "kali-linux" }
        "Opensuse" { "openSUSE-Leap-16.0" }
        "Oracle" { "OracleLinux_9_5" }
        default { $null }
    }

    if ([string]::IsNullOrWhiteSpace($distribution)) {
        [System.Windows.Forms.MessageBox]::Show("La distribuzione selezionata non e valida.", "Installazione WSL", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
        return
    }

    $wslPath = Join-Path ([Environment]::SystemDirectory) 'wsl.exe'
    try {
        $installProcess = Start-Process -FilePath $wslPath `
            -ArgumentList @('--install', '--distribution', $distribution, '--no-launch') `
            -Wait -PassThru

        if ($installProcess.ExitCode -ne 0) {
            throw "wsl.exe e terminato con codice $($installProcess.ExitCode)."
        }

        [System.Windows.Forms.MessageBox]::Show(
            "Installazione WSL avviata correttamente per $distribution. Riavvia Windows se richiesto, poi apri la distribuzione dal menu Start per completare la configurazione dell'utente Linux.",
            "Installazione WSL",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information)
    }
    catch {
        [System.Windows.Forms.MessageBox]::Show(
            "Installazione WSL non riuscita. Dettagli: $($_.Exception.Message)",
            "Installazione WSL",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error)
    }
})
$form.Controls.Add($buildButton)

$form.Add_Shown({$form.Activate()})
$form.ShowDialog() | Out-Null
####################################
