// ReSharper disable StringLiteralTypo

using System.Text.RegularExpressions;

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public partial class UnitTest5
{
    [TestMethod]
    public void TestMethod1()
    {
        const string path = """C:\Temp\PSX\HiOctane\decls.h""";

        var lines = File.ReadAllLines(path);

        var index1 = Array.FindIndex(lines, s => VariablesBegin().IsMatch(s));
        var index2 = Array.FindIndex(lines, s => VariablesEnd().IsMatch(s));

        var declarations = lines[index1..index2];
        
        var split = Split(declarations, VariableAddress()).ToArray();

        var variables = new List<SourceVariable>();

        foreach (var text in split[1..])
        {
            var varAddress = default(Match);
            var varContent = default(Match);

            foreach (var line in text)
            {
                var match1 = VariableAddress().Match(line);

                if (match1.Success)
                {
                    varAddress = match1;
                }

                var match2 = VariableContent().Match(line);

                if (match2.Success)
                {
                    varContent = match2;
                }
            }

            if (varAddress == null || varContent == null)
            {
                throw new InvalidOperationException();
            }

            var variable = new SourceVariable(
                varAddress.Groups["address"].Value,
                varContent.Groups["storage"].Value,
                varContent.Groups["type"].Value,
                varContent.Groups["name"].Value,
                varContent.Groups["dimensions"].Value
            );

            variables.Add(variable);
        }

        var vars = variables.DistinctBy(s=>s.Name).ToArray();
        Console.WriteLine(vars.Length);
        foreach (var variable in vars)
        {
            Console.WriteLine(variable);
        }
    }

    public static IEnumerable<string[]> Split(string[] array, Regex regex)
    {
        var start = 0;
        var index = 0;

        foreach (var input in array)
        {
            if (regex.IsMatch(input))
            {
                yield return array[start..index];
                start = index;
            }

            index++;
        }

        yield return array[start..];
    }

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\sVariables\s\(begin\)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex VariablesBegin();

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\sVariables\s\(end\)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex VariablesEnd();

    [GeneratedRegex(@"^//\saddress:\s0x(?<address>[A-F0-9]{8})$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex VariableAddress();

    [GeneratedRegex(@"^(?<storage>extern|static)\s(?<type>.*?)\s?(?<name>\w+)(?<dimensions>\[\d*\])*;$")]
    private static partial Regex VariableContent();

    private record SourceVariable(string Address, string Storage, string Type, string Name, string Dimensions)
    {
        public override string ToString()
        {
            return $"{Storage} {Type} {Name}{Dimensions.Trim()};".Replace("  ", " ").Replace("* ", "*");
        }
    }
}