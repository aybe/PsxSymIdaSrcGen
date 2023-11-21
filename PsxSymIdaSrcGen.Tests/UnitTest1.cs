// ReSharper disable StringLiteralTypo

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public class UnitTest1
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public void TestMethod1(string sourceFile, string targetDirectory, string entryPointFile)
    {
        var converter = Converter.Create(sourceFile, entryPointFile);

        Converter.WriteOutput(converter.Files, targetDirectory);
    }

    [TestMethod]
    [DataRow(@"C:\TEMP\PSX\HI-OCTANE\SLES_001.15.c", @"C:\TEMP\PSX\HI-OCTANE", "MAIN.C")]
    public void TestMethod2(string sourceFile, string targetDirectory, string entryPointFile)
    {
        var converter = Converter.Create(sourceFile, entryPointFile);

        Converter.Test(converter);
    }

    private void WriteLine(object? value)
    {
        TestContext.WriteLine(value?.ToString());
    }
}