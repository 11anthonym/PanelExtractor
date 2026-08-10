namespace PanelExtractor.Tests;

internal sealed class TestDirectory : IDisposable
{
    public TestDirectory()
    {
        RootPath = Path.Combine(Path.GetTempPath(), nameof(PanelExtractor), Guid.NewGuid().ToString());
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public string CreateSubdirectory(string name)
    {
        string path = Path.Combine(RootPath, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Dispose()
    {
        Directory.Delete(RootPath, recursive: true);
    }
}
