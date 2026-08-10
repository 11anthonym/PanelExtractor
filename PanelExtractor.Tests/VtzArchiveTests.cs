using System.IO.Compression;

namespace PanelExtractor.Tests;

[TestClass]
public class VtzArchiveTests
{
    [TestMethod]
    public async Task CreateAsync_WithOneVtx_CreatesExpectedArchive()
    {
        using var testDirectory = new TestDirectory();
        string source = testDirectory.CreateSubdirectory("source");
        string output = testDirectory.CreateSubdirectory("output");
        string assets = Path.Combine(source, "assets");
        Directory.CreateDirectory(assets);
        File.WriteAllText(Path.Combine(source, "Project.vtx"), "project");
        File.WriteAllText(Path.Combine(assets, "image.png"), "asset");

        string archivePath = await VtzArchive.CreateAsync(source, output);

        Assert.AreEqual("Project.vtz", Path.GetFileName(archivePath));
        Assert.IsTrue(File.Exists(archivePath));

        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        string[] entries = archive.Entries.Select(entry => entry.FullName).ToArray();
        CollectionAssert.Contains(entries, "Project.vtx");
        CollectionAssert.Contains(entries, "assets/image.png");
    }

    [TestMethod]
    public async Task CreateAsync_WhenArchiveExists_UsesNumberedName()
    {
        using var testDirectory = new TestDirectory();
        string source = testDirectory.CreateSubdirectory("source");
        string output = testDirectory.CreateSubdirectory("output");
        File.WriteAllText(Path.Combine(source, "Project.vtx"), "project");
        File.WriteAllText(Path.Combine(output, "Project.vtz"), "existing");

        string archivePath = await VtzArchive.CreateAsync(source, output);

        Assert.AreEqual("Project_1.vtz", Path.GetFileName(archivePath));
    }

    [TestMethod]
    public async Task CreateAsync_WithoutVtx_ThrowsHelpfulError()
    {
        using var testDirectory = new TestDirectory();
        string source = testDirectory.CreateSubdirectory("source");
        string output = testDirectory.CreateSubdirectory("output");

        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => VtzArchive.CreateAsync(source, output)
        );

        StringAssert.Contains(exception.Message, "No .vtx file");
    }

    [TestMethod]
    public async Task CreateAsync_WithMultipleVtxFiles_ThrowsHelpfulError()
    {
        using var testDirectory = new TestDirectory();
        string source = testDirectory.CreateSubdirectory("source");
        string output = testDirectory.CreateSubdirectory("output");
        File.WriteAllText(Path.Combine(source, "First.vtx"), "first");
        File.WriteAllText(Path.Combine(source, "Second.vtx"), "second");

        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => VtzArchive.CreateAsync(source, output)
        );

        StringAssert.Contains(exception.Message, "Multiple .vtx files");
    }
}
