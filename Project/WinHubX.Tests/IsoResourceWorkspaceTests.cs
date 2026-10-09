using System.Runtime.Versioning;
using WinHubX.Impostazioni;
using Xunit;

namespace WinHubX.Tests;

public sealed class IsoResourceWorkspaceTests
{
    [Fact]
    [SupportedOSPlatform("windows")]
    public void CreateSession_CreatesPrivateGuidDirectoryAndCanDeleteOnlyThatSession()
    {
        string sessionPath = IsoResourceWorkspace.CreateSession();

        try
        {
            Assert.Equal(sessionPath, IsoResourceWorkspace.ValidateSessionPath(sessionPath));
            Assert.True(Directory.Exists(sessionPath));
        }
        finally
        {
            IsoResourceWorkspace.DeleteSession(sessionPath);
        }

        Assert.False(Directory.Exists(sessionPath));
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void ValidateSessionPath_RejectsPathsOutsideWorkspace()
    {
        Assert.Throws<InvalidDataException>(() =>
            IsoResourceWorkspace.ValidateSessionPath(Path.GetTempPath()));
    }
}
