// ReSharper disable StringLiteralTypo

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1(string sourceFile, string targetDirectory, string entryPointFile)
    {
        var converter = Converter.Create(sourceFile, entryPointFile);

        Converter.WriteOutput(converter.Files, targetDirectory);
    }
}