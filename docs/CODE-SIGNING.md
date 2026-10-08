# Firma degli artefatti WinHubX

Le release Windows devono essere firmate con Authenticode prima della pubblicazione.
La chiave privata non va mai committata: il workflow usa due secret GitHub:

- `WINHUBX_SIGNING_CERT_BASE64`: certificato PFX codificato Base64;
- `WINHUBX_SIGNING_CERT_PASSWORD`: password del PFX.

Il certificato deve avere EKU **Code Signing**, catena attendibile e validità sufficiente per la release. Il job:

1. pubblica l’EXE self-contained `win-x64`;
2. firma l’EXE con SHA-256;
3. applica un timestamp RFC 3161;
4. verifica la firma con `signtool verify /pa`;
5. crea ZIP e hash SHA-256 dopo la firma.

Per test locali usare un certificato di sviluppo separato. Non usare mai certificati self-signed per release pubbliche e non stampare password o materiale del certificato nei log.
