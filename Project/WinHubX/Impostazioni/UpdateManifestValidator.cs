namespace WinHubX.Impostazioni;

public sealed record ValidatedUpdateManifest(string Version, string UpdateUrl, string Sha256);

public static class UpdateManifestValidator
{
    public static ValidatedUpdateManifest Validate(string? version, string? updateUrl, string? sha256)
    {
        if (string.IsNullOrWhiteSpace(version) || !System.Version.TryParse(version, out _))
        {
            throw new InvalidDataException("La versione nel manifest aggiornamenti non è valida.");
        }

        EnsureTrustedHttpsUrl(updateUrl, "pacchetto aggiornamento");
        if (!Uri.TryCreate(updateUrl, UriKind.Absolute, out Uri? uri)
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Il pacchetto aggiornamento deve essere un eseguibile di una release GitHub.");
        }

        if (!IsValidSha256(sha256))
        {
            throw new InvalidDataException("Il manifest aggiornamenti deve contenere un SHA-256 valido.");
        }

        return new ValidatedUpdateManifest(version, updateUrl!, sha256!);
    }

    public static bool IsValidSha256(string? sha256) =>
        sha256 is { Length: 64 } && sha256.All(Uri.IsHexDigit);

    public static void EnsureTrustedHttpsUrl(string? value, string description)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.UserInfo.Length != 0
            || !IsTrustedHost(uri.Host))
        {
            throw new InvalidDataException($"URL non attendibile per {description}.");
        }
    }

    private static bool IsTrustedHost(string host) =>
        host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
        || host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        || host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
}
