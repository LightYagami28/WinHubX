# Roadmap di modernizzazione WinHubX

Roadmap tecnica verificabile per portare WinHubX a uno standard moderno Windows/.NET. Ogni task va chiuso con build `-warnaserror`, test pertinenti, audit dipendenze e commit GPG.

## Criteri finiti di completamento

Lo scope WinHubX è tracciato nei 30 task numerati qui sotto: al checkpoint 2026-10-08 ne risultano chiusi 12 e restano 18. Non si aggiungono refactoring cosmetici fuori da bug riproducibili, rischio di sicurezza, regressioni o requisiti espliciti. Dopo WinHubX, i repository pubblici di MrNico98 richiesti dall'utente avranno una roadmap e una verifica distinte.

Il lavoro si considera consegnabile quando ogni task è verificato con evidenze oppure ha un'eccezione motivata e un'alternativa sicura; `main` compila su .NET 10 stabile e il branch separato compila sul target .NET 11 disponibile, con tutti i warning trattati senza soppressioni; test, CI, CodeQL e audit dipendenze sono verdi; avvio e flussi UI pertinenti sono verificati su Windows; ogni dipendenza diretta è alla più recente versione stabile e le transitive sono aggiornate al massimo compatibile, con major incompatibili migrate insieme o sostituite. Restano esplicitamente fuori una certificazione legale NIS2 e la firma Authenticode finché non è disponibile un certificato. Raggiunti questi criteri, si ferma la modernizzazione e si consegna lo stato, non si riapre il ciclo per perfezionismi.

## Fondamenta e toolchain

- [x] 1. Consolidare `.editorconfig`, analizzatori .NET e regole di formattazione (policy comuni in `Directory.Build.props`, warning-as-error e code-style enforcement).
- [x] 2. Mantenere branch separati .NET 10 stabile e .NET 11 preview.
- [x] 3. Centralizzare versioni NuGet e verificare ogni aggiornamento dal registro (`Directory.Packages.props`; audit NuGet 2026-10-08: nessun pacchetto vulnerabile; il branch .NET 11 usa Newtonsoft.Json 14 beta e pin delle dipendenze .NET 11 RC, soggetti a verifica prima del rilascio. Restano due update transitivi: Mono.Posix.NETStandard 5.20.1-preview (preview pubblicata nel 2020, non adottata senza compatibilità verificata) e ApplicationInsights 3.1.2 (richiede migrazione coordinata del provider MTP, resta 2.23.0).
- [x] 4. Configurare CI per restore, build, audit vulnerabilità e `git diff --check`.
- [x] 5. Aggiungere test di smoke dell’avvio e dei servizi condivisi (CI Windows avvia il publish, verifica la finestra principale e termina il processo senza applicare tweak).
- [x] 6. Aggiungere test di regressione per configurazione, lingua e tema (round-trip impostazioni tema/lingua, parità chiavi/cataloghi IT-EN, proprietà JSON duplicate e placeholder).
- [x] 7. Documentare SDK, runtime, RID e processo di rilascio riproducibile in [`RELEASE.md`](RELEASE.md).
- [x] 8. Verificare trimming, single-file e self-contained con report in [`RELEASE.md`](RELEASE.md): framework-dependent single-file e self-contained `win-x64` passano su .NET 11 RC con 0 warning; trimming viene correttamente rifiutato dall'SDK (`NETSDK1175`) per WinForms e resta disabilitato, senza soppressioni.

## Architettura e performance

- [ ] 9. Separare servizi, stato applicativo e presentazione WinForms.
- [ ] 10. Introdurre lifecycle e cancellazione coerenti per tutte le operazioni async.
- [ ] 11. Eliminare scansioni ricorsive e I/O sincrono dal thread UI.
- [ ] 12. Rendere cache e snapshot hardware con scadenza e invalidazione esplicite.
- [ ] 13. Completare la riscrittura del Monitoraggio con un modello di snapshot unico.
- [ ] 14. Profilare startup, CPU, allocazioni e heap su .NET 10 e .NET 11.
- [ ] 15. Ridurre creazione ripetuta di form, immagini e handler.
- [ ] 16. Misurare working set e tempi di apertura per ogni sezione.
- [ ] 17. Applicare double buffering, DPI PerMonitorV2 e layout responsive ai Designer.
- [ ] 18. Verificare accessibilità, contrasto, tastiera e scaling dal 100% al 200%.

## Sicurezza e aggiornamenti

- [ ] 19. Rendere tutti i download HTTPS-only con allowlist e timeout.
- [x] 20. Rendere obbligatori hash SHA-256 per i nuovi artefatti pubblicati dalla CI: `New-ArtifactSha256Manifest.ps1` calcola SHA-256 e dimensione per ogni file, incluso l'eseguibile; il manifest generato è verificato localmente e allegato all'artefatto.
- [x] 21. Verificare Authenticode per i binari pubblicati: ogni firma deve risultare valida; i file non firmati sono esplicitamente indicati come `NotSigned` nel manifest, senza bloccare lo stato attuale privo di certificato.
- [x] 22. Updater con sostituzione atomica staged, backup, verifica SHA-256, attesa finestra avviata e rollback se startup fallisce; coperto da test di regressione.
- [ ] 23. Eliminare quoting fragile nei processi PowerShell/CMD.
- [ ] 24. Verificare privilegi minimi e isolare ogni azione che richiede UAC.
- [ ] 25. Audit completo di script, URL, file binari e risorse esterne.
- [x] 26. Configurare Dependabot per NuGet e GitHub Actions.

La valutazione tecnica del rischio ispirata a NIS2 e le evidenze richieste sono descritte in [`SECURITY-ENGINEERING.md`](SECURITY-ENGINEERING.md); è una metodologia ingegneristica, non una valutazione legale o attestazione di conformità.

## UX, distribuzione e documentazione

- [ ] 27. Completare la palette futuristica centralizzata per tutte le form.
- [ ] 28. Riscrivere Designer e risorse legacy mantenendo localizzazione IT/EN.
- [ ] 29. Preparare pacchetto release, manifest winget e recipe Chocolatey con hash reali.
- [ ] 30. Pubblicare wiki tecnica, guida utenti, troubleshooting e changelog verificabili.

## Repository correlati MrNico98

Inventario pubblico rilevato il 2026-10-08: `PhoenixPlay`, `WinHubX`, `ISODownloader`, `PhoenixPlay-Resources`, `WIMToolkit`, `WinCustomizer`, `MiniCleanerTool`, `WinHubX-Resource`, `Assistente-Aggiornamento`, `ImageDebloat`, `pacman`, oltre a fork di terzi. Verranno importati solo i repository pertinenti, con licenza e dipendenze verificate; i fork di terzi non verranno copiati automaticamente.
