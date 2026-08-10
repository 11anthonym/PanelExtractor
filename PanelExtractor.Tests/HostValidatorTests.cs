namespace PanelExtractor.Tests;

[TestClass]
public class HostValidatorTests
{
    [TestMethod]
    [DataRow("192.168.1.50", false)]
    [DataRow("panel.example.test", false)]
    [DataRow("2001:db8::1", false)]
    [DataRow("999.168.1.50", true)]
    [DataRow("192.168.1", true)]
    [DataRow("192..1.50", true)]
    public void IsInvalidIpv4Literal_ClassifiesHostInput(string value, bool expected)
    {
        Assert.AreEqual(expected, HostValidator.IsInvalidIpv4Literal(value));
    }
}
