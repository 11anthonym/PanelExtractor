namespace PanelExtractor.Tests;

[TestClass]
public class XPanelLauncherTests
{
    private const string Vtz = @"C:\out\project.vtz";
    private const string FirstCandidate = @"C:\Program Files\Crestron\XPanel\CrestronXPanel\CrestronXpanel.exe";
    private const string SecondCandidate = @"C:\Program Files (x86)\Crestron\XPanel\CrestronXPanel\CrestronXpanel.exe";

    [TestMethod]
    public void Launch_StartsTheFirstInstalledExecutable()
    {
        var started = new List<(string Executable, string Argument)>();
        var launcher = new XPanelLauncher(
            [FirstCandidate, SecondCandidate],
            path => path == SecondCandidate,
            (executable, argument) => started.Add((executable, argument))
        );

        XPanelLaunchResult result = launcher.Launch(Vtz);

        Assert.AreEqual(XPanelLaunchOutcome.Started, result.Outcome);
        Assert.AreEqual(1, started.Count);
        Assert.AreEqual(SecondCandidate, started[0].Executable);
        Assert.AreEqual(Vtz, started[0].Argument);
    }

    [TestMethod]
    public void Launch_PrefersTheEarlierCandidateWhenBothExist()
    {
        var started = new List<string>();
        var launcher = new XPanelLauncher(
            [FirstCandidate, SecondCandidate],
            _ => true,
            (executable, _) => started.Add(executable)
        );

        launcher.Launch(Vtz);

        CollectionAssert.AreEqual(new[] { FirstCandidate }, started);
    }

    [TestMethod]
    public void Launch_ReportsNotInstalledWithoutStartingAnything()
    {
        bool startAttempted = false;
        var launcher = new XPanelLauncher(
            [FirstCandidate, SecondCandidate],
            _ => false,
            (_, _) => startAttempted = true
        );

        XPanelLaunchResult result = launcher.Launch(Vtz);

        Assert.AreEqual(XPanelLaunchOutcome.NotInstalled, result.Outcome);
        Assert.IsFalse(startAttempted);
        StringAssert.Contains(result.Message, "not found");
    }

    [TestMethod]
    public void Launch_ReportsAFailureToStartInsteadOfThrowing()
    {
        var launcher = new XPanelLauncher(
            [FirstCandidate],
            _ => true,
            (_, _) => throw new System.ComponentModel.Win32Exception("Access is denied")
        );

        XPanelLaunchResult result = launcher.Launch(Vtz);

        Assert.AreEqual(XPanelLaunchOutcome.Failed, result.Outcome);
        StringAssert.Contains(result.Message, "Access is denied");
    }

    [TestMethod]
    public void FindInstalledExecutable_ReturnsNullWhenNoCandidateExists()
    {
        var launcher = new XPanelLauncher([FirstCandidate], _ => false, (_, _) => { });

        Assert.IsNull(launcher.FindInstalledExecutable());
    }

    [TestMethod]
    public void DefaultCandidatePaths_CoversBothProgramFolders()
    {
        string[] paths = XPanelLauncher.DefaultCandidatePaths().ToArray();

        Assert.IsTrue(paths.Length >= 2);
        Assert.IsTrue(paths.All(path =>
            path.EndsWith("CrestronXpanel.exe", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(
            paths.Length,
            paths.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            "Candidate paths should not repeat.");
    }
}
