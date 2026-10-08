# Sicurezza dei download e delle risorse legacy

## Regole applicative

- WinHubX non scarica né esegue script di attivazione da URL remoti.
- I download applicativi devono usare HTTPS, file temporanei univoci e pubblicazione atomica al termine.
- I driver vengono aperti dalla pagina ufficiale del produttore; non vengono estratti o avviati automaticamente.
- Le funzioni di attivazione aprono esclusivamente le impostazioni/account ufficiali Microsoft.
- Nessuna esclusione di Microsoft Defender viene richiesta o configurata dall’applicazione.

## Script legacy conservati nel sorgente

I seguenti file sono conservati per tracciabilità storica, ma sono esclusi dalle risorse compilate e dal flusso runtime. Gli hash sono riferimenti locali e non costituiscono una firma digitale né una garanzia di sicurezza:

| File | SHA-256 |
|---|---|
| `Resources/KMS38_Activation.cmd` | `22CCDD5ACD5324715CBA8E219D2DE0A73D9E73F78674EC443148FFB8F11B64FA` |
| `Resources/Ohook_Activation_AIO.cmd` | `609AA604CB3EBBCC8C7D4902876096F8634BF1D87829F610919E9B173181AA29` |
| `Resources/TSforge_Activation_Office.cmd` | `48ABECF8DFDA7537F06828AFF9C2A7119BBC532BBDA6165C3420C4E7B10BC6F3` |
| `Resources/TSforge_Activation.cmd` | `ED39D5ADE26F255392A05E90C9F204426028D53832CFECC89EB0BF2D946B6BF0` |
| `Resources/WinHubXStatoAttivazione.cmd` | `0C593B8CC48A249F584097C104F6562F3415D15580655058E9BD7BA4DC52E266` |

Questi file possono essere classificati dai prodotti di sicurezza come strumenti di modifica dell’attivazione. Non vanno usati per eludere licenze o protezioni. Per Windows e Office usare una licenza valida e i canali Microsoft ufficiali.

## Verifica locale

```powershell
Get-FileHash .\Resources\KMS38_Activation.cmd -Algorithm SHA256
dotnet build .\WinHubX.csproj --configuration Release
```

La presenza dei file nel repository non implica che siano distribuiti: il progetto non li include più come `EmbeddedResource`.
