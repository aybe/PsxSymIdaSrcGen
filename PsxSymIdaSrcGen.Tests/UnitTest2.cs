// ReSharper disable StringLiteralTypo

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public class UnitTest2
{
    [TestMethod]
    public void TestMethod1()
    {
        const string path = @"C:\Temp\SLES_001.15.c";

        var source = new Source(path);

        var declarations = source.GetDeclarations();

        var variables = source.GetVariables();

        var functions = source.GetFunctions();

        Console.WriteLine($"Declaration count: {declarations.Count}");
        Console.WriteLine($"Variable block length: {variables.Length}");
        Console.WriteLine($"Function count: {functions.Count}");

        var lookup = functions.ToLookup(s => s.File);

        foreach (var grouping in lookup)
        {
            Console.WriteLine($"Key: {grouping.Key}");

            foreach (var function in grouping)
            {
                Console.WriteLine("\t" + function);
            }
        }
    }
}