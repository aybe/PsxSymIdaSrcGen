// ReSharper disable StringLiteralTypo

using System.Text.RegularExpressions;

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

public sealed partial class Source
{
    public Source(string path)
    {
        Text = File.ReadAllLines(path);

        LineFirstDeclaration =
            Array.FindIndex(Text, RegexDeclaration1().IsMatch);

        LineFirstVariable =
            Array.FindIndex(Text, RegexVariable1().IsMatch);

        LineFirstFunction =
            Array.FindIndex(Text, RegexFunctionAddress().IsMatch);

        LineEndOfFile =
            Array.FindIndex(Text, RegexFunctionBlockEnd().IsMatch);

        TextDeclarations =
            Text.AsSpan(new Range(LineFirstDeclaration, LineFirstVariable)).ToArray();

        TextVariables =
            Text.AsSpan(new Range(LineFirstVariable, LineFirstFunction)).ToArray();

        TextFunctions =
            Text.AsSpan(new Range(LineFirstFunction, LineEndOfFile)).ToArray();
    }

    private int LineFirstDeclaration { get; }

    private int LineFirstVariable { get; }

    private int LineFirstFunction { get; }

    private int LineEndOfFile { get; }

    private string[] Text { get; }

    private string[] TextDeclarations { get; }

    private string[] TextFunctions { get; }

    private string[] TextVariables { get; }

    [GeneratedRegex(@"^//\sFunction\sdeclarations$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexDeclaration1();

    [GeneratedRegex(@"^(?![\s/]+).*?(\w+)\(.*\);", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexDeclarationName();

    [GeneratedRegex(@"^/{2}-{5}\s\(([A-Z0-9]{8})\)\s-{56}$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionAddress();

    [GeneratedRegex(@"^/{2}\s\[PSX-MND-SYM\]\sFunction\sfile\s=\s(\S+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionFile();

    [GeneratedRegex(@"^/{2}\s\[PSX-MND-SYM\]\sFunction\sname\s=\s(\w+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionName();

    [GeneratedRegex(@"^/{2}\snfuncs=\d+", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionBlockEnd();

    [GeneratedRegex(@"^//\sData\sdeclarations$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexVariable1();

    [GeneratedRegex(@"^(?!\s+)(?:\w+\s)+\(?(?:__fastcall\s)?\**(\w+)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexVariableName();

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

    public Dictionary<string, string> GetDeclarations()
    {
        var declarations = new Dictionary<string, string>();

        foreach (var line in TextDeclarations)
        {
            if (RegexDeclarationName().Match(line) is { Success: true } match)
            {
                declarations.Add(match.Groups[1].Value, line);
            }
        }

        return declarations;
    }

    public string[] GetVariables()
    {
        var variables = TextVariables.ToArray();

        return variables;
    }

    public List<SourceFunction> GetFunctions()
    {
        var start = LineFirstFunction;
        var lines = new List<int>();

        foreach (var line in TextFunctions)
        {
            if (RegexFunctionAddress().Match(line) is { Success: true })
            {
                lines.Add(start);
            }

            start++;
        }

        lines.Add(LineEndOfFile);

        var ranges = new List<Range>();

        for (var i = 0; i < lines.Count - 1; i++)
        {
            ranges.Add(new Range(lines[i], lines[i + 1]));
        }

        var functions = ranges.Select(s => Text.AsSpan(s).ToArray()).ToList();

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

        return sourceFunctions;
    }
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