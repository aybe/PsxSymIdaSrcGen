using System.Diagnostics;
using System.Text.RegularExpressions;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace PsxSymIdaSrcGen;

public sealed partial class Converter
{
    public required string Entry { get; init; }

    public required Dictionary<string, List<string>> Files { get; init; }

    public required Dictionary<string, List<string>> Funcs { get; init; }

    public required List<string> Lines { get; init; }

    public required List<List<string>> Lists { get; init; }
    
    public static Converter Create(string sourceFile, string entryPointFile)
    {
        var lines = File.ReadAllLines(sourceFile).ToList();

        var lists = GetLists(lines);

        var entry = GetEntry(lists, entryPointFile);

        var files = GetFiles(lists, entry);

        var line1 = lines.FindIndex(0, s => s.StartsWith("// Function declarations"));
        var line2 = lines.FindIndex(0, s => s.StartsWith("// Data declarations"));
        var line3 = lines.FindIndex(0, s => s.StartsWith("//----- (")); // function #1

        var list1 = lines[line1..line2];
        var list2 = lines[line2..line3];
        var list3 = lines[line3..];

        var funcs = new Dictionary<string, List<string>>();

        foreach (var list in lists.Skip(1))
        {
            var input1 = list[0];
            var input2 = list[1];
            var input3 = list[2];

            var match1 = RegexFunctionAddressComment().Match(input1);

            var match2 = Regex.Match(input2, @"(?<=^//\s\[PSX-MND-SYM\]\s)[\w\:\.\\]+(?=$)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

            if (match2.Success is false)
            {
                continue; // function without file info
            }

            var match3 = Regex.Match(input3, @"\w+(?=\()", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

            Debug.Assert(match1.Success, input1);
            Debug.Assert(match2.Success, input2);
            Debug.Assert(match3.Success, input3);

            Console.WriteLine(match2.Value + " | " + match3.Value);
            if (funcs.ContainsKey(match2.Value) == false)
            {
                funcs.Add(match2.Value, new List<string>());
            }

            funcs[match2.Value].Add(match3.Value);
        }

        var converter = new Converter
        {
            Entry = entry,
            Files = files,
            Funcs = funcs,
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

    public static void WriteOutput(Converter converter, string targetDirectory)
    {
        var directory = Directory.CreateDirectory(targetDirectory);

        var files = converter.Files;

        var lines = converter.Lines;

        var line1 = lines.FindIndex(0, s => s.StartsWith("// Function declarations"));
        var line2 = lines.FindIndex(0, s => s.StartsWith("// Data declarations"));
        var line3 = lines.FindIndex(0, s => s.StartsWith("//----- (")); // function #1

        var slice1 = lines[line1..line2];
        var slice2 = lines[line2..line3];
        var slice3 = lines[line3..];

        var functions = GetMatches(slice1, RegexFunctionDeclaration()).ToArray();
        var variables = GetMatches(slice2, RegexVariableDeclaration()).ToArray();

        var headers = files
            .Select(s => $@"#include ""{Path.ChangeExtension(s.Key, ".H").Replace(Path.VolumeSeparatorChar.ToString(), string.Empty)}""")
            .ToList();

        foreach (var (path, text) in files)
        {
            var sourcePath = Path.Combine(directory.FullName, path.Replace(Path.VolumeSeparatorChar.ToString(), string.Empty));
            var headerPath = Path.ChangeExtension(sourcePath, ".H");

            Directory.CreateDirectory(directory.FullName);

            using (var header = new StringWriter())
            {
                header.WriteLine("#pragma once");
                header.WriteLine();

                if (path == converter.Entry) // OK
                {
                    header.WriteLine("#include \"defs.h\"");
                    header.WriteLine("#include \"types.h\"");
                    header.WriteLine();

                    header.WriteLine("#pragma region Variables");
                    header.WriteLine();

                    var regex = RegexVariableDeclaration();

                    foreach (var line in slice2)
                    {
                        var match = regex.Match(line);

                        if (match.Success == false)
                        {
                            continue;
                        }

                        var value = match.Groups[1].Value;

                        header.WriteLine($"extern {value};");
                    }

                    header.WriteLine();
                    header.WriteLine("#pragma endregion");
                }

                header.WriteLine();

                header.WriteLine("#pragma region Functions");
                header.WriteLine();

                if (text.Any(s => s.Contains("move_displacement_set")))
                {
                    //throw new NotImplementedException();
                }

                foreach (var line in text.Where(s => RegexFunctionImplementation().IsMatch(s)))
                {
                    header.WriteLine($"{line};"); // TODO BUG is probably here, missing declarations
                }

                header.WriteLine();
                header.WriteLine("#pragma endregion");

                File.WriteAllText(headerPath, header.ToString());
            }

            using (var source = new StringWriter())
            {
                headers.ForEach(source.WriteLine);

                source.WriteLine();

                text.ForEach(source.WriteLine);

                File.WriteAllText(sourcePath, source.ToString());
            }
        }
    }

    [Obsolete]
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

    #region Regex

    [GeneratedRegex(@"^/{2}-{5}\s\([A-Z0-9]{8}\)\s-{56}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex RegexFunctionAddressComment();

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\s(.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex RegexFunctionFileComment();

    [GeneratedRegex(@"(?:[\w\*]+\s)*(\*?\w+)(?:\(.*\);)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionDeclaration();

    [GeneratedRegex(@"^\S.*\(.*\)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexFunctionImplementation();

    [GeneratedRegex(@"^(.{2,}?)(?=\s*[=;])", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexVariableDeclaration();

    #endregion
}