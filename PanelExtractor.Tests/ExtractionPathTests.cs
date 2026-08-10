namespace PanelExtractor.Tests;

[TestClass]
public class ExtractionPathTests
{
    [TestMethod]
    [DataRow("project.vtx")]
    [DataRow("image 01.png")]
    [DataRow("café.png")]
    [DataRow("project-name_1.vtx")]
    public void Combine_AllowsSingleSafeFileName(string name)
    {
        using var testDirectory = new TestDirectory();
        string root = testDirectory.CreateSubdirectory("extract");

        string result = ExtractionPath.Combine(root, name);

        Assert.AreEqual(Path.Combine(Path.GetFullPath(root), name), result);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("..")]
    [DataRow(@"..\outside.txt")]
    [DataRow("../outside.txt")]
    [DataRow(@"C:\outside.txt")]
    [DataRow(@"C:outside.txt")]
    [DataRow(@"\\server\share\outside.txt")]
    [DataRow(@"subdir\asset.png")]
    [DataRow("subdir/asset.png")]
    [DataRow("file.txt:stream")]
    [DataRow("trailing.")]
    [DataRow("trailing ")]
    [DataRow("CON")]
    [DataRow("CON..txt")]
    [DataRow("con .txt")]
    [DataRow("nul.txt")]
    [DataRow("COM1.log")]
    [DataRow("LPT9")]
    public void Combine_RejectsUnsafePanelEntryName(string name)
    {
        using var testDirectory = new TestDirectory();
        string root = testDirectory.CreateSubdirectory("extract");

        InvalidDataException exception =
            Assert.ThrowsExactly<InvalidDataException>(() => ExtractionPath.Combine(root, name));

        Assert.AreEqual(
            "The panel returned an unsafe file or directory name. Extraction was stopped.",
            exception.Message);
    }
}
