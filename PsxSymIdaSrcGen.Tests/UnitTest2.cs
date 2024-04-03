// ReSharper disable StringLiteralTypo

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public class UnitTest2
{
    [TestMethod]
    public void TestMethod1()
    {
        const string sourceFile = @"C:\Temp\SLES_001.15.c";

        const string targetDirectory = @"C:\Temp\PSX\HiOctane";

        var source = new Source(sourceFile);

        var declarations = source.GetDeclarations();

        var variables = source.GetVariables();

        var functions = source.GetFunctions();

        Console.WriteLine($"Declaration count: {declarations.Count}");
        Console.WriteLine($"Variable block length: {variables.Length}");
        Console.WriteLine($"Function count: {functions.Count}");

        var directory = Directory.CreateDirectory(targetDirectory);

        var lookup = functions.ToLookup(s => s.File);

        foreach (var grouping in lookup)
        {
            var key = grouping.Key;

            Console.WriteLine($"Key: {key}");

            using var sourceWriter = new StringWriter();
            using var headerWriter = new StringWriter();

            var keyFileName = Path.GetFileName(key);

            sourceWriter.WriteLine($"#include \"{Path.ChangeExtension(keyFileName, ".H")}\"");
            sourceWriter.WriteLine();

            headerWriter.WriteLine("#pragma once");
            headerWriter.WriteLine();
            headerWriter.WriteLine("#define __MIPS__");
            headerWriter.WriteLine("#include \"defs.h\"");
            headerWriter.WriteLine("#include \"decls.h\""); // TODO only grab extern
            headerWriter.WriteLine("#include \"types.h\"");
            headerWriter.WriteLine();

            if (string.Equals(keyFileName, "MAIN.C", StringComparison.OrdinalIgnoreCase))
            {
                variables.ForEach(sourceWriter.WriteLine);
            }

            foreach (var function in grouping)
            {
                function.Text.ForEach(sourceWriter.WriteLine);

                if (!declarations.TryGetValue(function.Name, out var declaration))
                    continue;

                headerWriter.WriteLine(declaration);

                declarations.Remove(function.Name);
            }

            var path = key.Replace($"{Path.VolumeSeparatorChar}", string.Empty);

            if (Path.GetDirectoryName(path) is { } directoryName)
            {
                directory.CreateSubdirectory(directoryName);
            }

            var sourceFilePath = Path.Combine(targetDirectory, path);
            var headerFilePath = Path.ChangeExtension(sourceFilePath, ".H");

            File.WriteAllText(sourceFilePath, sourceWriter.ToString());
            File.WriteAllText(headerFilePath, headerWriter.ToString());
        }
    }
}