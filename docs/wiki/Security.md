# Sicurezza

I download devono usare HTTPS, allowlist di host e verifica dell'integrità quando disponibile. Le operazioni elevate devono usare il minimo privilegio, spiegare l'effetto e non riavviare automaticamente il sistema.

La creazione ISO e le riparazioni di sistema mantengono l'applicazione principale `asInvoker` e richiedono una sola elevazione UAC per operazione. Il broker accetta solo comandi `DISM`, `reg.exe`, `sfc.exe`, `chkdsk.exe` e `regsvr32.exe` con argomenti convalidati; non esegue comandi shell ricevuti dall'interfaccia. Pipe e workspace di sessione hanno ACL dedicate. I file temporanei sono confinati a una cartella casuale sul disco fisso con più spazio e il completamento elimina solo quella cartella, mai directory globali come `C:\ISO` o `C:\mount`. Il controllo disco usa `chkdsk /scan` online e non pianifica scansioni al riavvio. Il test end-to-end UAC va eseguito su Windows con un'immagine ISO e un ambiente di prova prima del rilascio.

Non includere certificati Authenticode, password, token o chiavi private nel repository. La firma delle release viene eseguita fuori dal controllo versione dal maintainer.

Per segnalazioni riservate consulta [SECURITY.md](../../SECURITY.md).
