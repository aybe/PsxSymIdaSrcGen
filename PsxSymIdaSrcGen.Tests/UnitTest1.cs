// ReSharper disable StringLiteralTypo

using System.Text.RegularExpressions;

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public partial class UnitTest1
{
    public required TestContext TestContext { get; set; }

    private void WriteLine(object? value)
    {
        TestContext.WriteLine(value?.ToString());
    }

    [TestMethod]
    [TestCategory("SkipWhenLiveUnitTesting")]
    [DataRow(@"C:\TEMP\PSX\HI-OCTANE\SLES_001.15.c", @"C:\TEMP\PSX\HI-OCTANE", "MAIN.C")]
    public void TestMethod1(string sourceFile, string targetDirectory, string entryPointFile)
    {
        var converter = Converter.Create(sourceFile, entryPointFile);

        Converter.WriteOutput(converter, targetDirectory);
    }

    [TestMethod]
    [TestCategory("SkipWhenLiveUnitTesting")]
    [DataRow(@"C:\TEMP\PSX\HI-OCTANE\SLES_001.15.c", @"C:\TEMP\PSX\HI-OCTANE", "MAIN.C")]
    public void TestMethod2(string sourceFile, string targetDirectory, string entryPointFile)
    {
        var converter = Converter.Create(sourceFile, entryPointFile);

        Converter.Test(converter);
    }
}

public partial class UnitTest1
{
    [TestMethod]
    [DataRow(@"C:\TEMP\PSX\HI-OCTANE\SLES_001.15.c", @"C:\TEMP\PSX\HI-OCTANE", "MAIN.C")]
    public void TestMethod3(string path, string targetDirectory, string entryPointFile)
    {
        var lines = File.ReadAllLines(path).ToList();

        var line0 = lines.FindIndex(s => s.StartsWith("// Function declarations"));
        var line1 = lines.FindIndex(s => s.StartsWith("// Data declarations"));
        var line2 = lines.FindIndex(s => s.StartsWith("// [PSX-MND-SYM]"));

        {
            // parse declarations

            var declarations = new List<FunctionDeclaration>();

            var list = lines[line0..line1];

            for (var i = 0; i < list.Count; i++)
            {
                var input = list[i];

                var match = RegexDeclaration().Match(input);

                if (match.Success)
                {
                    declarations.Add(new FunctionDeclaration(i + line0, match.Groups[1].Value, input));
                }
            }

            const bool printDeclarations = false;

            if (printDeclarations)
            {
                WriteLine("Declarations:");

                foreach (var declaration in declarations)
                {
                    WriteLine(declaration);
                }
            }
        }

        {
            // parse variables

            var variables = new List<SourceVariable>();

            var list = lines[line1..line2];

            var positions = new List<int>();

            for (var i = 0; i < list.Count; i++)
            {
                var input = list[i];

                var match = RegexVariable().Match(input);

                if (match.Success)
                {
                    WriteLine($"{line1 + i}, {match.Groups[1].Value}, {input}");
                }
            }
        }
        return;
        using var stream = File.OpenRead(path);

        using var workspace = new Workspace(stream);

        Console.WriteLine(workspace.LineDeclarations);
        Console.WriteLine(workspace.LineVariables);
        Console.WriteLine(workspace.LineImplementations);
    }


    [GeneratedRegex(@"^(?!//).+?(\w+)(?=\(.*\))", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexDeclaration();

    [GeneratedRegex(@"^(.{2,}?)(?=\s*[=;])", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexVariable();
}

public class SourceVariable
{
}

public sealed class FunctionDeclaration
{
    public FunctionDeclaration(int line, string name, string text)
    {
        Line = line;
        Name = name;
        Text = text;
    }

    public int Line { get; }

    public string Name { get; }

    public string Text { get; }

    public override string ToString()
    {
        return $"{nameof(Line)}: {Line}, {nameof(Name)}: {Name}, {nameof(Text)}: {Text}";
    }
}

internal sealed class Workspace : IDisposable
{
    public Workspace(Stream stream)
    {
        Reader = new StreamReader(stream);

        LineDeclarations =
            FindFirstIndex(Reader, s => s.StartsWith("// Function declarations"));

        LineVariables =
            FindFirstIndex(Reader, s => s.StartsWith("// Data declarations"));

        LineImplementations =
            FindFirstIndex(Reader, s => s.StartsWith("// [PSX-MND-SYM]"));
    }

    private StreamReader Reader { get; }

    public int LineDeclarations { get; }

    public int LineVariables { get; }

    public int LineImplementations { get; }

    public void Dispose()
    {
        Reader.Dispose();
    }

    private static void SeekToLine(StreamReader reader, int line)
    {
        var result = 0;
    }

    private static int FindFirstIndex(StreamReader reader, Func<string, bool> predicate)
    {
        reader.BaseStream.Position = 0;

        reader.DiscardBufferedData();

        var index = -1;

        while (true)
        {
            var line = reader.ReadLine();

            if (line == null)
            {
                break;
            }

            index++;

            if (predicate(line))
            {
                break;
            }
        }


        if (index is -1)
        {
            throw new InvalidOperationException();
        }

        return index;
    }
}