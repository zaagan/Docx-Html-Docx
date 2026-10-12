using System;
using System.IO;

namespace Docx.TestSupport;

/// <summary>
/// A unique scratch directory that deletes itself at the end of a test.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "docx-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>Full path of a file inside this directory; the file is not created.</summary>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <summary>Full path of a uniquely named file with the given extension.</summary>
    public string NewFile(string extension) => File(Guid.NewGuid().ToString("N") + extension);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A leftover scratch directory must never fail a test.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
