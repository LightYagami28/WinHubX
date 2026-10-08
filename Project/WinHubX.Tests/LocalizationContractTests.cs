using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace WinHubX.Tests;

public sealed class LocalizationContractTests
{
    private static readonly Dictionary<string, Dictionary<string, string>> Italian = LoadCatalog("it");
    private static readonly Dictionary<string, Dictionary<string, string>> English = LoadCatalog("en");

    [Fact]
    public void Catalogs_ContainTheSameSectionsAndKeys()
    {
        string[] missingSections = Italian.Keys.Except(English.Keys, StringComparer.Ordinal).ToArray();
        string[] extraSections = English.Keys.Except(Italian.Keys, StringComparer.Ordinal).ToArray();
        var missingKeys = new List<string>();
        var extraKeys = new List<string>();

        foreach (string section in Italian.Keys.Intersect(English.Keys, StringComparer.Ordinal))
        {
            missingKeys.AddRange(Italian[section].Keys
                .Except(English[section].Keys, StringComparer.Ordinal)
                .Select(key => $"{section}.{key}"));
            extraKeys.AddRange(English[section].Keys
                .Except(Italian[section].Keys, StringComparer.Ordinal)
                .Select(key => $"{section}.{key}"));
        }

        Assert.True(missingSections.Length == 0 && extraSections.Length == 0 &&
                    missingKeys.Count == 0 && extraKeys.Count == 0,
            $"Localization catalogs differ. Missing sections: {string.Join(", ", missingSections)}; " +
            $"extra sections: {string.Join(", ", extraSections)}; " +
            $"missing keys: {string.Join(", ", missingKeys)}; extra keys: {string.Join(", ", extraKeys)}.");
    }

    [Fact]
    public void Catalogs_DoNotContainDuplicateJsonProperties()
    {
        string[] duplicates = new[] { FindDuplicateProperties("it"), FindDuplicateProperties("en") }
            .Where(result => result.Length > 0)
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Catalogs_PreserveFormattingPlaceholdersAndNonEmptyTranslations()
    {
        var failures = new List<string>();

        foreach ((string section, Dictionary<string, string> italianEntries) in Italian)
        {
            foreach ((string key, string italianValue) in italianEntries)
            {
                string englishValue = English[section][key];
                if (!string.IsNullOrWhiteSpace(italianValue) && string.IsNullOrWhiteSpace(englishValue))
                    failures.Add($"{section}.{key} has no English translation");

                string[] italianPlaceholders = GetPlaceholders(italianValue);
                string[] englishPlaceholders = GetPlaceholders(englishValue);
                if (!italianPlaceholders.SequenceEqual(englishPlaceholders, StringComparer.Ordinal))
                    failures.Add($"{section}.{key} placeholders differ: IT [{string.Join(",", italianPlaceholders)}], EN [{string.Join(",", englishPlaceholders)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static Dictionary<string, Dictionary<string, string>> LoadCatalog(string language)
    {
        string resourceName = $"WinHubX.Tests.Resources.{language}.json";
        using Stream stream = typeof(LocalizationContractTests).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded localization resource '{resourceName}' was not found.");

        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)
            ?? throw new InvalidDataException($"Localization resource '{resourceName}' is empty or invalid.");
    }

    private static string FindDuplicateProperties(string language)
    {
        string resourceName = $"WinHubX.Tests.Resources.{language}.json";
        using Stream stream = typeof(LocalizationContractTests).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded localization resource '{resourceName}' was not found.");
        using JsonDocument document = JsonDocument.Parse(stream);
        var duplicates = new List<string>();

        void Visit(JsonElement element, string path)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                        duplicates.Add($"{path}.{property.Name}");

                    Visit(property.Value, $"{path}.{property.Name}");
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement item in element.EnumerateArray())
                    Visit(item, $"{path}[{index++}]");
            }
        }

        Visit(document.RootElement, language);
        return string.Join(", ", duplicates);
    }

    private static string[] GetPlaceholders(string value) => Regex.Matches(value, @"\{\d+\}")
        .Select(match => match.Value)
        .OrderBy(placeholder => placeholder, StringComparer.Ordinal)
        .ToArray();
}
