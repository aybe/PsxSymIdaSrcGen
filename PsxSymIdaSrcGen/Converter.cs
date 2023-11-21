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
}