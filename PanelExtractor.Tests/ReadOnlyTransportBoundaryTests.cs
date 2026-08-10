using System.Reflection;

namespace PanelExtractor.Tests;

[TestClass]
public class ReadOnlyTransportBoundaryTests
{
    [TestMethod]
    [DynamicData(nameof(ClientTypes))]
    public void Client_ExposesOnlyApprovedReadOperations(Type clientType)
    {
        string[] expectedMethods =
        [
            "CanReadDirectoryAsync",
            "ConnectAsync",
            "Dispose",
            "DownloadFileAsync",
            "ListDirectoryAsync"
        ];

        MethodInfo[] exposedMethods = clientType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsPrivate && !method.IsSpecialName)
            .ToArray();

        string[] actualMethods = exposedMethods.Select(method => method.Name).OrderBy(name => name).ToArray();

        Assert.IsTrue(clientType.IsSealed);
        CollectionAssert.AreEqual(expectedMethods.OrderBy(name => name).ToArray(), actualMethods);
        Assert.IsFalse(exposedMethods.Any(ExposesTransportLibraryType));
    }

    [TestMethod]
    public void DirectoryEntry_ExposesOnlyReadMetadata()
    {
        string[] expectedProperties = ["Kind", "Length", "Name"];
        string[] actualProperties = typeof(PanelFileEntry)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        CollectionAssert.AreEqual(expectedProperties, actualProperties);
    }

    [TestMethod]
    public void Interface_ContainsNoMutationOperations()
    {
        string[] expectedMethods =
        [
            "CanReadDirectoryAsync",
            "ConnectAsync",
            "DownloadFileAsync",
            "ListDirectoryAsync"
        ];

        string[] actualMethods = typeof(IReadOnlyPanelFileClient)
            .GetMethods()
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();

        CollectionAssert.AreEqual(expectedMethods.OrderBy(name => name).ToArray(), actualMethods);
    }

    public static IEnumerable<object[]> ClientTypes =>
    [
        [typeof(ReadOnlySftpClient)],
        [typeof(ReadOnlyFtpClient)]
    ];

    private static bool ExposesTransportLibraryType(MethodInfo method)
    {
        return IsTransportLibraryType(method.ReturnType) ||
            method.GetParameters().Any(parameter => IsTransportLibraryType(parameter.ParameterType));
    }

    private static bool IsTransportLibraryType(Type type)
    {
        string? fullName = type.FullName;

        return fullName?.StartsWith("Renci.SshNet", StringComparison.Ordinal) == true ||
            fullName?.StartsWith("FluentFTP", StringComparison.Ordinal) == true ||
            (type.IsGenericType && type.GetGenericArguments().Any(IsTransportLibraryType));
    }
}
