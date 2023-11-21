using System.Text.RegularExpressions;

namespace PsxSymIdaSrcGen;

public sealed partial class Converter
{
    private Converter()
    {
    }

    public required string Entry { get; init; }

    public required Dictionary<string, List<string>> Files { get; init; }

    public required List<string> Lines { get; init; }

    public required List<List<string>> Lists { get; init; }

    public static Converter Create(string sourceFile, string entryPointFile)
    {
        var lines = File.ReadAllLines(sourceFile).ToList();

        var lists = GetLists(lines);

        var entry = GetEntry(lists, entryPointFile);

        var files = GetFiles(lists, entry);

        var converter = new Converter
        {
            Entry = entry,
            Files = files,
            Lines = lines,
            Lists = lists
        };

        return converter;
    }

    private static List<List<string>> GetLists(List<string> lines)
    {
        var ranges = new List<Range>();

        var offset = 0;

        for (var index = 0; index < lines.Count; index++)
        {
            var input = lines[index];

            var match = RegexFunctionAddressComment().Match(input);

            if (match.Success == false)
            {
                continue;
            }

            ranges.Add(new Range(offset, index));

            offset = index;
        }

        ranges.Add(new Range(ranges[^1].End, lines.Count));

        var chunks = ranges.Select(s => lines[s.Start.Value..s.End.Value]).ToList();

        return chunks;
    }

    private static string GetEntry(List<List<string>> chunks, string function)
    {
        var entry = function;

        foreach (var chunk in chunks)
        {
            var input = chunk[1];

            var match = RegexFunctionFileComment().Match(input);

            if (match.Success == false)
            {
                continue;
            }

            entry = match.Groups[1].Value;

            break;
        }

        return entry;
    }

    private static Dictionary<string, List<string>> GetFiles(List<List<string>> chunks, string mainFile)
    {
        var dictionary = new Dictionary<string, List<string>>();

        foreach (var chunk in chunks)
        {
            var input = chunk[1];

            var match = RegexFunctionFileComment().Match(input);

            var key = match.Success ? match.Groups[1].Value : mainFile;

            if (dictionary.ContainsKey(key) is false)
            {
                dictionary.Add(key, new List<string>());
            }

            var list = dictionary[key];

            list.AddRange(chunk);
        }

        return dictionary;
    }

    public static void WriteOutput(Dictionary<string, List<string>> sourceFiles, string targetDirectory)
    {
        var directory = Directory.CreateDirectory(targetDirectory);

        var headers = sourceFiles
            .Select(s => $@"#include ""{Path.ChangeExtension(s.Key, ".H").Replace(Path.VolumeSeparatorChar.ToString(), string.Empty)}""")
            .ToList();

        foreach (var (path, text) in sourceFiles)
        {
            var sourcePath = Path.Combine(directory.FullName, path.Replace(Path.VolumeSeparatorChar.ToString(), string.Empty));
            var headerPath = Path.ChangeExtension(sourcePath, ".H");

            Directory.CreateDirectory(directory.FullName);

            using var sourceWriter = new StreamWriter(File.Create(sourcePath));

            headers.ForEach(sourceWriter.WriteLine);

            sourceWriter.WriteLine();

            text.ForEach(sourceWriter.WriteLine);

            using var headerWriter = new StreamWriter(File.Create(headerPath));

            headerWriter.WriteLine(); // TODO add declarations
        }
    }

    [GeneratedRegex(@"^//-{5}\s\([A-Z0-9]{8}\)\s-{56}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex RegexFunctionAddressComment();

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\s(.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex RegexFunctionFileComment();

    [GeneratedRegex(@"(?:[\w\*]+\s)*(\*?\w+)(?:\(.*\);)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionDeclaration();

    [GeneratedRegex(@"^(.{2,}?)(?=\s*[=;])", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexVariableDeclaration();

    public static void Test(Converter converter)
    {
        var lines = converter.Lines;

        var line1 = lines.FindIndex(0, s => s.StartsWith("// Function declarations"));
        var line2 = lines.FindIndex(0, s => s.StartsWith("// Data declarations"));
        var line3 = lines.FindIndex(0, s => s.StartsWith("//----- (")); // function #1

        var slice1 = lines[line1..line2];
        var slice2 = lines[line2..line3];
        var slice3 = lines[line3..];

        var functions = GetMatches(slice1, RegexFunctionDeclaration()).ToArray();
        var variables = GetMatches(slice2, RegexVariableDeclaration()).ToArray();

        Console.WriteLine(line1);
        Console.WriteLine(line2);
        Console.WriteLine(line3);

        Console.WriteLine();

        Console.WriteLine(slice1.Count);
        Console.WriteLine(slice2.Count);
        Console.WriteLine(slice3.Count);

        Console.WriteLine();
        
        foreach (var match in functions)
        {
            Console.WriteLine(match.Groups[1].Value);
        }

        Console.WriteLine();

        foreach (var match in variables)
        {
            Console.WriteLine(match.Groups[1].Value);
        }
    }

    private static IEnumerable<Match> GetMatches(IEnumerable<string> list, Regex regex)
    {
        return list.Select(s => regex.Match(s)).Where(s => s.Success);
    }
}