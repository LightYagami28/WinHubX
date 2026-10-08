# Roadmap di modernizzazione WinHubX

Roadmap tecnica verificabile per portare WinHubX a uno standard moderno Windows/.NET. Ogni task va chiuso con build `-warnaserror`, test pertinenti, audit dipendenze e commit GPG.

## Fondamenta e toolchain

- [x] 1. Consolidare `.editorconfig`, analizzatori .NET e regole di formattazione (policy comuni in `Directory.Build.props`, warning-as-error e code-style enforcement).
- [x] 2. Mantenere branch separati .NET 10 stabile e .NET 11 preview.
- [x] 3. Centralizzare versioni NuGet e verificare ogni aggiornamento dal registro (`Directory.Packages.props`; audit NuGet 2026-10-08: nessun pacchetto vulnerabile o update stabile diretto dell’app; ApplicationInsights 2.23.0 resta transitivo dal provider MTP, la 3.x richiede upgrade coordinato).
- [x] 4. Configurare CI per restore, build, audit vulnerabilità e `git diff --check`.
- [ ] 5. Aggiungere test di smoke dell’avvio e dei servizi condivisi.
- [ ] 6. Aggiungere test di regressione per configurazione, lingua e tema.
- [ ] 7. Documentare SDK, runtime, RID e processo di rilascio riproducibile.
- [ ] 8. Verificare trimming, single-file e self-contained con report di pubblicazione.

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
- [ ] 20. Rendere obbligatori hash SHA-256 per i nuovi artefatti di release.
- [ ] 21. Aggiungere verifica Authenticode quando l’artefatto è firmato.
- [ ] 22. Rendere updater atomico con backup, rollback e controllo del processo avviato.
- [ ] 23. Eliminare quoting fragile nei processi PowerShell/CMD.
- [ ] 24. Verificare privilegi minimi e isolare ogni azione che richiede UAC.
- [ ] 25. Audit completo di script, URL, file binari e risorse esterne.
- [x] 26. Configurare Dependabot per NuGet e GitHub Actions.

## UX, distribuzione e documentazione

- [ ] 27. Completare la palette futuristica centralizzata per tutte le form.
- [ ] 28. Riscrivere Designer e risorse legacy mantenendo localizzazione IT/EN.
- [ ] 29. Preparare pacchetto release, manifest winget e recipe Chocolatey con hash reali.
- [ ] 30. Pubblicare wiki tecnica, guida utenti, troubleshooting e changelog verificabili.

## Repository correlati MrNico98

Inventario pubblico rilevato il 2026-10-08: `PhoenixPlay`, `WinHubX`, `ISODownloader`, `PhoenixPlay-Resources`, `WIMToolkit`, `WinCustomizer`, `MiniCleanerTool`, `WinHubX-Resource`, `Assistente-Aggiornamento`, `ImageDebloat`, `pacman`, oltre a fork di terzi. Verranno importati solo i repository pertinenti, con licenza e dipendenze verificate; i fork di terzi non verranno copiati automaticamente.
