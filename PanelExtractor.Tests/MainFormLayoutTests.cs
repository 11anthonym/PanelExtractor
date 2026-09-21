using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Drawing.Imaging;
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
            ToolStripMenuItem fileMenu = form.MainMenuStrip!.Items
                .OfType<ToolStripMenuItem>()
                .Single(item => item.Text == "File");

            Assert.AreEqual("PanelExtractor", typeof(MainForm).Assembly.GetName().Name);
            Assert.AreEqual("Panel Extractor", form.Text);
            using System.Drawing.Icon? executableIcon = System.Drawing.Icon.ExtractAssociatedIcon(
                Application.ExecutablePath);
            Assert.IsNotNull(executableIcon);
            CollectionAssert.AreEqual(ToPng(executableIcon!), ToPng(form.Icon!));
            Assert.AreEqual("Allow legacy FTP fallback (unencrypted)", legacyFtp.Text);
            CollectionAssert.AreEqual(
                new[] { "Exit" },
                fileMenu.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text).ToArray());
            Assert.IsTrue(form.ClientRectangle.Contains(legacyFtp.Bounds));
            Assert.IsFalse(legacyFtp.Bounds.IntersectsWith(outputFolder.Bounds));
            Assert.IsGreaterThanOrEqualTo(legacyFtp.Width, legacyFtp.PreferredSize.Width);

            toggleDetails.PerformClick();

            Assert.AreEqual("Hide Details", toggleDetails.Text);
            Assert.IsTrue(form.ClientRectangle.Contains(log.Bounds));
        });
    }

    [TestMethod]
    public void OpenInXPanel_IsOffUntilChosenAndFitsBesideTheOtherControls()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            CheckBox openInXPanel = FindControl<CheckBox>(form, "chkOpenInXPanel");
            CheckBox legacyFtp = FindControl<CheckBox>(form, "chkAllowLegacyFtp");
            TextBox outputFolder = FindControl<TextBox>(form, "txtOutputFolder");
            Button extract = FindControl<Button>(form, "btnExtract");

            // Extraction must not launch anything unless the operator asks for it.
            Assert.IsFalse(openInXPanel.Checked);
            Assert.AreEqual("Open in Crestron XPanel after extracting", openInXPanel.Text);

            Assert.IsTrue(form.ClientRectangle.Contains(openInXPanel.Bounds));
            Assert.IsGreaterThanOrEqualTo(openInXPanel.Width, openInXPanel.PreferredSize.Width);
            Assert.IsFalse(openInXPanel.Bounds.IntersectsWith(outputFolder.Bounds));
            Assert.IsFalse(openInXPanel.Bounds.IntersectsWith(legacyFtp.Bounds));
            Assert.IsFalse(openInXPanel.Bounds.IntersectsWith(extract.Bounds));

            typeof(MainForm)
                .GetMethod("SetBusyState", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, [true]);

            Assert.IsFalse(openInXPanel.Enabled);
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

    private static byte[] ToPng(System.Drawing.Icon icon)
    {
        using var bitmap = icon.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
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
