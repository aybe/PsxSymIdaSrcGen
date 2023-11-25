// ReSharper disable StringLiteralTypo

using System.Text.RegularExpressions;

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public partial class UnitTest2
{
    [TestMethod]
    public void TestMethod1()
    {
        const string path = @"C:\Temp\SLES_001.15.c";

        var source = File.ReadAllLines(path);

        var index1 = Array.FindIndex(source, RegexFunctionDeclarations().IsMatch);
        var index2 = Array.FindIndex(source, RegexDataDeclarations().IsMatch);
        var index3 = Array.FindIndex(source, RegexFunctionAddress().IsMatch);
        var index4 = source.Length;
        var index5 = Array.FindIndex(source, RegexEndOfFile().IsMatch);

        var range1 = source.AsSpan(new Range(index1, index2));
        var range2 = source.AsSpan(new Range(index2, index3));
        var range3 = source.AsSpan(new Range(index3, index4));

        // declarations

        var declarations = new Dictionary<string, string>();

        foreach (var line in range1)
        {
            if (RegexDeclarationName().Match(line) is { Success: true } match)
            {
                declarations.Add(match.Groups[1].Value, line);
            }
        }

        Console.WriteLine($"Declarations count: {declarations.Count}");

        // variables

        var variables = range2.ToArray();

        Console.WriteLine($"Variables block length: {variables.Length}");

        // implementations

        var start = index3;
        var lines = new List<int>();

        foreach (var line in range3)
        {
            if (RegexFunctionAddress().Match(line) is { Success: true })
            {
                lines.Add(start);
            }

            start++;
        }

        lines.Add(index5);

        var ranges = new List<Range>();

        for (var i = 0; i < lines.Count - 1; i++)
        {
            ranges.Add(new Range(lines[i], lines[i + 1]));
        }

        var functions = ranges.Select(s => source.AsSpan(s).ToArray()).ToList();

        var sourceFunctions = new List<SourceFunction>(functions.Count);

        foreach (var functionText in functions)
        {
            var functionFile = GetMatchGroupValue(functionText, 1, RegexFunctionFile());

            var functionName = GetMatchGroupValue(functionText, 1, RegexFunctionName());

            functionFile ??= string.Empty;

            functionName ??= GetMatchGroupValue(functionText, 1, RegexFunctionAddress());

            if (functionName == null)
            {
                throw new InvalidOperationException();
            }

            var sourceFunction = new SourceFunction(functionFile, functionName, functionText);

            sourceFunctions.Add(sourceFunction);
        }

        var mainFile = sourceFunctions
            .FirstOrDefault(s => string.Equals(Path.GetFileName(s.File), "MAIN.C", StringComparison.OrdinalIgnoreCase))?
            .File;

        mainFile ??= @"C:\MAIN.C";

        foreach (var function in sourceFunctions.Where(s => string.IsNullOrEmpty(s.File)))
        {
            function.File = mainFile;
        }

        var lookup = sourceFunctions.ToLookup(s => s.File);

        foreach (var grouping in lookup)
        {
            Console.WriteLine($"Key: {grouping.Key}");
            foreach (var function in grouping)
            {
                Console.WriteLine("\t" + function);
            }
        }
    }

    private static string? GetMatchGroupValue(IEnumerable<string> lines, int group, Regex regex)
    {
        foreach (var input in lines)
        {
            var match = regex.Match(input);

            if (!match.Success)
            {
                continue;
            }

            var groups = match.Groups;

            if (group < 0 || group > groups.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(group));
            }

            var value = groups[group].Value;

            return value;
        }

        return null;
    }

    [GeneratedRegex(@"^/{2}-{5}\s\(([A-Z0-9]{8})\)\s-{56}$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionAddress();

    [GeneratedRegex(@"^/{2}\s\[PSX-MND-SYM\]\sFunction\sname\s=\s(\w+)$")]
    private static partial Regex RegexFunctionName();

    [GeneratedRegex(@"^/{2}\s\[PSX-MND-SYM\]\sFunction\sfile\s=\s(\S+)$")]
    private static partial Regex RegexFunctionFile();

    [GeneratedRegex(@"^//\sFunction\sdeclarations$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionDeclarations();

    [GeneratedRegex(@"^//\sData\sdeclarations$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexDataDeclarations();

    [GeneratedRegex(@"^(?![\s/]+).*?(\w+)\(.*\);", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexDeclarationName();

    [GeneratedRegex(@"^(?!\s+)(?:\w+\s)+\(?(?:__fastcall\s)?\**(\w+)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexVariableName();

    [GeneratedRegex(@"^/{2}\snfuncs=\d+", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexEndOfFile();
}

public class Source
{
    public Dictionary<string, string> Declarations { get; set; } = null!;
}

public sealed class SourceFunction(string file, string name, string[] text)
{
    public string File { get; set; } = file;

    public string Name { get; set; } = name;

    public string[] Text { get; set; } = text;

    public override string ToString()
    {
        return Name;
    }
}