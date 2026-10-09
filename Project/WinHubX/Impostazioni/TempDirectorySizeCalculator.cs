using System.Diagnostics;

namespace WinHubX.Impostazioni;

public static class TempDirectorySizeCalculator
{
    public static long Calculate(string folderPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        long size = 0;
        var pendingDirectories = new Stack<DirectoryInfo>();
        pendingDirectories.Push(new DirectoryInfo(folderPath));

        while (pendingDirectories.TryPop(out DirectoryInfo? currentDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                foreach (FileSystemInfo entry in currentDirectory.EnumerateFileSystemInfos())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        FileAttributes attributes = entry.Attributes;
                        bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                        bool isReparsePoint = (attributes & FileAttributes.ReparsePoint) != 0;

                        if (isDirectory)
                        {
                            if (!isReparsePoint)
                            {
                                pendingDirectories.Push((DirectoryInfo)entry);
                            }

                            continue;
                        }

                        if (!isReparsePoint && entry is FileInfo file)
                        {
                            size += file.Length;
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"Elemento TEMP non leggibile: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Scansione cartella TEMP non riuscita: {ex.Message}");
            }
        }

        return size;
    }
}
