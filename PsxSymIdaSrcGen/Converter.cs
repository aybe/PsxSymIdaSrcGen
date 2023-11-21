using System.Text.RegularExpressions;

namespace PsxSymIdaSrcGen;

public static partial class Converter
{
    public static void Process(string sourceFile, string targetDirectory, string entryPointFile)
    {
        var lines = File.ReadAllLines(sourceFile).ToList();

        var lists = GetLists(lines);

        var entry = GetEntry(lists, entryPointFile);

        var files = GetFiles(lists, entry);

        WriteOutput(files, targetDirectory);
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
            var input = chunk.ElementAtOrDefault(1);

            if (input == null)
            {
                continue;
            }

            var match = RegexFunctionFileComment().Match(input);

            if (match.Success == false)
            {
                continue;
            }

            var path = match.Groups[1].Value;

            var name = Path.GetFileName((string?)path);

            if (string.Equals(entry, name, StringComparison.OrdinalIgnoreCase) == false)
            {
                continue;
            }

            entry = path;

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

    private static void WriteOutput(Dictionary<string, List<string>> sourceFiles, string targetDirectory)
    {
        var directory = Directory.CreateDirectory(targetDirectory);

        foreach (var (key, value) in sourceFiles)
        {
            var path = key.Replace(Path.VolumeSeparatorChar.ToString(), string.Empty);

            path = Path.Combine(directory.FullName, path);

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());

            File.WriteAllLines(path, value);
        }
    }

    [GeneratedRegex(@"^//-{5}\s\([A-Z0-9]{8}\)\s-{56}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex RegexFunctionAddressComment();

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\s(.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex RegexFunctionFileComment();
}