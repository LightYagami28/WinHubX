using System.Runtime.Versioning;
using WinHubX.Impostazioni;
using Xunit;

namespace WinHubX.Tests;

[SupportedOSPlatform("windows")]
public sealed class ElevatedProcessCommandValidatorTests
{
    private const string WorkspaceRoot = @"C:\WinHubX\IsoSessions\0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData("/mount-image")]
    [InlineData("/unmount-image")]
    [InlineData("/export-image")]
    public void Validate_AcceptsRequiredDismOperations(string operation)
    {
        string[] arguments = operation switch
        {
            "/mount-image" => [operation, $"/imagefile:{WorkspaceRoot}\\ISO\\WinISO\\sources\\install.wim", "/index:1", $"/mountdir:{WorkspaceRoot}\\Mount\\mount"],
            "/unmount-image" => [operation, $"/mountdir:{WorkspaceRoot}\\Mount\\mount", "/commit"],
            _ => [operation, $"/SourceImageFile:{WorkspaceRoot}\\ISO\\WinISO\\sources\\install.esd", "/SourceIndex:1",
                $"/DestinationImageFile:{WorkspaceRoot}\\ISO\\WinISO\\sources\\install.wim", "/Compress:max", "/CheckIntegrity"]
        };

        ElevatedProcessCommandValidator.Validate(ElevatedProcessKind.Dism, arguments, WorkspaceRoot);
    }

    [Fact]
    public void Validate_RejectsUnapprovedDismOperation()
    {
        Assert.Throws<ArgumentException>(() => ElevatedProcessCommandValidator.Validate(
            ElevatedProcessKind.Dism, ["/online", "/cleanup-image", "/startcomponentcleanup"], WorkspaceRoot));
    }

    [Fact]
    public void Validate_AcceptsDriverIntegrationOnlyForSessionMounts()
    {
        ElevatedProcessCommandValidator.Validate(ElevatedProcessKind.Dism,
            [$"/Image:{WorkspaceRoot}\\Mount\\mount", "/Add-Driver", @"/Driver:C:\Drivers", "/Recurse"], WorkspaceRoot);
    }

    [Fact]
    public void Validate_AcceptsPackageInventoryAndOnlyApprovedPackageRemoval()
    {
        ElevatedProcessCommandValidator.Validate(ElevatedProcessKind.Dism,
            [$"/Image:{WorkspaceRoot}\\Mount\\mount", "/English", "/Get-Packages", "/Format:List"], WorkspaceRoot);
        ElevatedProcessCommandValidator.Validate(ElevatedProcessKind.Dism,
            [$"/Image:{WorkspaceRoot}\\Mount\\mount", "/English", "/Remove-Package",
                "/PackageName:Microsoft-Windows-MediaPlayer-Package~31bf3856ad364e35~amd64~~10.0.1.0", "/NoRestart"], WorkspaceRoot);
    }

    [Fact]
    public void ParseInstalledPackageIdentities_ReturnsOnlyInstalledAllowlistedPackages()
    {
        const string output = """
            Package Identity : Microsoft-Windows-MediaPlayer-Package~31bf3856ad364e35~amd64~~10.0.1.0
            State : Installed

            Package Identity : Microsoft-Windows-MediaPlayer-Package~31bf3856ad364e35~amd64~~10.0.0.0
            State : Superseded

            Package Identity : Microsoft-Windows-Other-Package~31bf3856ad364e35~amd64~~10.0.1.0
            State : Installed
            """;

        IReadOnlyList<string> packages = ElevatedProcessCommandValidator.ParseInstalledPackageIdentities(output);

        Assert.Equal(["Microsoft-Windows-MediaPlayer-Package~31bf3856ad364e35~amd64~~10.0.1.0"], packages);
    }

    [Theory]
    [InlineData(@"/Image:C:\Windows", "/Add-Driver", @"/Driver:C:\Drivers", "/Recurse")]
    [InlineData(@"/Image:C:\WinHubX\IsoSessions\0123456789abcdef0123456789abcdef\Mount\mount", "/Add-Driver", @"/Driver:\\server\share", "/Recurse")]
    [InlineData(@"/Image:C:\WinHubX\IsoSessions\0123456789abcdef0123456789abcdef\Mount\mount", "/Add-Driver", @"/Driver:C:\Drivers", "/Recurse", "/Online")]
    public void Validate_RejectsUnsafeDriverIntegrationArguments(params string[] arguments)
    {
        Assert.Throws<ArgumentException>(() => ElevatedProcessCommandValidator.Validate(
            ElevatedProcessKind.Dism, arguments, WorkspaceRoot));
    }

    [Fact]
    public void Validate_AcceptsRegistryCommandsOnlyForTemporaryHives()
    {
        ElevatedProcessCommandValidator.Validate(ElevatedProcessKind.Registry,
            ["load", "HKLM\\TK_SOFTWARE", $"{WorkspaceRoot}\\Mount\\mount\\Windows\\System32\\config\\SOFTWARE"], WorkspaceRoot);
        ElevatedProcessCommandValidator.Validate(ElevatedProcessKind.Registry,
            ["add", @"HKLM\TK_SYSTEM\Setup\LabConfig", "/v", "BypassTPMCheck", "/t", "REG_DWORD", "/d", "1", "/f"], WorkspaceRoot);
        ElevatedProcessCommandValidator.Validate(ElevatedProcessKind.Registry,
            ["unload", "HKLM\\TK_SOFTWARE"], WorkspaceRoot);
    }

    [Theory]
    [InlineData("HKLM\\SOFTWARE\\Microsoft\\Windows")]
    [InlineData("HKCU\\Software\\WinHubX")]
    public void Validate_RejectsRegistryCommandsOutsideTemporaryHives(string keyPath)
    {
        Assert.Throws<ArgumentException>(() => ElevatedProcessCommandValidator.Validate(
            ElevatedProcessKind.Registry, ["add", keyPath, "/v", "Unsafe", "/t", "REG_DWORD", "/d", "1", "/f"], WorkspaceRoot));
    }

    [Fact]
    public void Validate_RejectsRegistryHiveFilesOutsideImageMounts()
    {
        Assert.Throws<ArgumentException>(() => ElevatedProcessCommandValidator.Validate(
            ElevatedProcessKind.Registry,
            ["load", "HKLM\\TK_SOFTWARE", $"{WorkspaceRoot}\\Windows\\System32\\config\\SOFTWARE"], WorkspaceRoot));
    }
}
