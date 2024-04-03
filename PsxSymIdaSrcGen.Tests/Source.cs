using System.Text.RegularExpressions;

// ReSharper disable StringLiteralTypo

namespace PsxSymIdaSrcGen.Tests;

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

    public static string? GetMatchGroupValue(IEnumerable<string> lines, int group, Regex regex)
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
        var lines = new List<int>();

        var index = LineFirstFunction;

        foreach (var line in TextFunctions)
        {
            if (RegexFunctionAddress().Match(line) is { Success: true })
            {
                lines.Add(index);
            }

            index++;
        }

        lines.Add(LineEndOfFile);

        var texts = Text.Split(lines).ToArray();

        texts = texts[1..^1];

        var functions = new List<SourceFunction>();

        foreach (var text in texts)
        {
            var file = GetMatchGroupValue(text, 1, RegexFunctionFile()) ??
                       string.Empty;

            var name = GetMatchGroupValue(text, 1, RegexFunctionName()) ??
                       GetMatchGroupValue(text, 1, RegexFunctionAddress()) ??
                       throw new InvalidOperationException();

            functions.Add(new SourceFunction(file, name, text));
        }

        var main = functions
            .FirstOrDefault(s => string.Equals(Path.GetFileName(s.File), "MAIN.C", StringComparison.OrdinalIgnoreCase))?
            .File ?? "MAIN.C";

        foreach (var function in functions.Where(s => string.IsNullOrEmpty(s.File)))
        {
            function.File = main;
        }

        return functions;
    }
}