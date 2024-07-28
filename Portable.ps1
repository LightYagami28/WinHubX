$URL = "https://github.com/MrNico98/WinHubX/releases/download/WinHubX-v.2.4.0.2/WinHubX.portable.exe"
$FILE = "WinHubX.portable.exe"

try {
    # Scarica il file
    Write-Output "Inizio download..."
    Invoke-WebRequest -Uri $URL -OutFile $FILE -UseBasicP
    Write-Output "Download completato."

    # Verifica se il file è stato scaricato
    if (-Not (Test-Path $FILE)) {
        throw "Il download non è riuscito. Il file non è stato trovato."
    }

    # Esegui l'applicazione
    Write-Output "Avviando l'applicazione..."
    $process = Start-Process -FilePath $FILE -PassThru
    $process.WaitForExit()

    # Verifica se il processo è stato terminato con successo
    if ($process.ExitCode -eq 0) {
        Write-Output "Applicazione terminata con successo."
    } else {
        throw "L'applicazione è terminata con codice di uscita non zero: $($process.ExitCode)"
    }

    # Rimuovi il file
    Write-Output "Eliminando $FILE..."
    Remove-Item -Path $FILE -Force
    Write-Output "Operazione completata."
}
catch {
    Write-Error "Errore: $_"
}
finally {
    # Assicurati che il file venga eliminato anche in caso di errore
    if (Test-Path $FILE) {
        Remove-Item -Path $FILE -Force
    }
}