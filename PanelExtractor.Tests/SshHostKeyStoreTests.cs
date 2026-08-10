namespace PanelExtractor.Tests;

[TestClass]
public class SshHostKeyStoreTests
{
    private const string Host = "panel.example.test";
    private const int Port = 22;

    [TestMethod]
    public void Verifier_RemembersFirstKeyOnlyAfterConnectionCompletes()
    {
        using var testDirectory = new TestDirectory();
        var store = CreateStore(testDirectory);
        var verifier = new SshHostKeyVerifier(Host, Port, store);

        Assert.IsTrue(verifier.CanTrust("first-key"));
        Assert.IsFalse(verifier.CanTrust("different-key"));
        Assert.AreEqual(SshHostKeyStatus.Unknown, store.Check(Host, Port, "first-key"));

        verifier.Remember();

        Assert.AreEqual(SshHostKeyStatus.Trusted, store.Check(Host, Port, "first-key"));
    }

    [TestMethod]
    public void Verifier_RejectsChangedKeyWithoutReplacingSavedKey()
    {
        using var testDirectory = new TestDirectory();
        var store = CreateStore(testDirectory);
        store.Remember(Host, Port, "first-key");
        var verifier = new SshHostKeyVerifier(Host, Port, store);

        Assert.IsFalse(verifier.CanTrust("different-key"));
        Assert.AreEqual(SshHostKeyStatus.Trusted, store.Check(Host, Port, "first-key"));
    }

    [TestMethod]
    public void Verifier_RejectsKeyChangeDuringFirstConnection()
    {
        using var testDirectory = new TestDirectory();
        var store = CreateStore(testDirectory);
        var verifier = new SshHostKeyVerifier(Host, Port, store);
        Assert.IsTrue(verifier.CanTrust("first-key"));
        store.Remember(Host, Port, "different-key");

        Assert.ThrowsExactly<InvalidDataException>(verifier.Remember);
        Assert.AreEqual(SshHostKeyStatus.Trusted, store.Check(Host, Port, "different-key"));
    }

    [TestMethod]
    public void Store_MatchesHostsCaseInsensitivelyAndKeepsPortsSeparate()
    {
        using var testDirectory = new TestDirectory();
        var store = CreateStore(testDirectory);
        store.Remember("PANEL.EXAMPLE.TEST", Port, "first-key");

        Assert.AreEqual(SshHostKeyStatus.Trusted, store.Check(Host, Port, "first-key"));
        Assert.AreEqual(SshHostKeyStatus.Unknown, store.Check(Host, 2222, "first-key"));
    }

    [TestMethod]
    public void Store_CorruptFileFailsClosedAndCanBeCleared()
    {
        using var testDirectory = new TestDirectory();
        string filePath = Path.Combine(testDirectory.RootPath, "ssh-host-keys.json");
        File.WriteAllText(filePath, "{");
        var store = new SshHostKeyStore(filePath);

        Assert.ThrowsExactly<InvalidDataException>(() => store.Check(Host, Port, "first-key"));
        Assert.IsTrue(store.Clear());
        Assert.AreEqual(SshHostKeyStatus.Unknown, store.Check(Host, Port, "first-key"));
    }

    private static SshHostKeyStore CreateStore(TestDirectory testDirectory) =>
        new(Path.Combine(testDirectory.RootPath, "ssh-host-keys.json"));
}
