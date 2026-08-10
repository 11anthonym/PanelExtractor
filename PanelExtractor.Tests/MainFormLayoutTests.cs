using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

namespace PanelExtractor.Tests;

[TestClass]
public class MainFormLayoutTests
{
    [TestMethod]
    public void LegacyFtpConsentAndDetailLayouts_FitWithoutOverlap()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            CheckBox legacyFtp = FindControl<CheckBox>(form, "chkAllowLegacyFtp");
            TextBox outputFolder = FindControl<TextBox>(form, "txtOutputFolder");
            Button toggleDetails = FindControl<Button>(form, "btnToggleDetails");
            TextBox log = FindControl<TextBox>(form, "txtLog");

            Assert.AreEqual("PanelExtractor", typeof(MainForm).Assembly.GetName().Name);
            Assert.AreEqual("Panel Extractor", form.Text);
            Assert.AreEqual("Allow legacy FTP fallback (unencrypted)", legacyFtp.Text);
            Assert.IsTrue(form.ClientRectangle.Contains(legacyFtp.Bounds));
            Assert.IsFalse(legacyFtp.Bounds.IntersectsWith(outputFolder.Bounds));
            Assert.IsGreaterThanOrEqualTo(legacyFtp.Width, legacyFtp.PreferredSize.Width);

            toggleDetails.PerformClick();

            Assert.AreEqual("Hide Details", toggleDetails.Text);
            Assert.IsTrue(form.ClientRectangle.Contains(log.Bounds));
        });
    }

    [TestMethod]
    public void DetailsToggle_RemainsInteractiveWhileBusy()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            Button extract = FindControl<Button>(form, "btnExtract");
            Button toggleDetails = FindControl<Button>(form, "btnToggleDetails");
            TextBox log = FindControl<TextBox>(form, "txtLog");

            typeof(MainForm)
                .GetMethod("SetBusyState", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, [true]);

            Assert.IsFalse(extract.Enabled);
            Assert.IsTrue(toggleDetails.Enabled);

            toggleDetails.PerformClick();
            Assert.AreEqual("Hide Details", toggleDetails.Text);
            Assert.IsTrue(log.Visible);

            toggleDetails.PerformClick();
            Assert.AreEqual("Show Details", toggleDetails.Text);
            Assert.IsFalse(log.Visible);
        });
    }

    private static T FindControl<T>(Control parent, string name) where T : Control
    {
        return parent.Controls.Find(name, searchAllChildren: true).Single() as T
            ?? throw new AssertFailedException($"Control {name} was not a {typeof(T).Name}.");
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
