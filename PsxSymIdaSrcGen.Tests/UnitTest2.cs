// ReSharper disable StringLiteralTypo

using System.Globalization;
using System.Text.RegularExpressions;

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public partial class UnitTest2
{
    public required TestContext TestContext { get; set; }

    private void WriteLine(object? value = null)
    {
        TestContext.WriteLine(value?.ToString());
    }

    [TestMethod]
    public void TestMethod1()
    {
        var sources = File.ReadAllLines(@"C:\Temp\SLES_001.15.c");

        var ranges = GetFunctionsRanges(sources);

        var functions = GetFunctions(ranges, sources);
    }

    private static object GetFunctions(List<Range> ranges, string[] sources)
    {
        foreach (var range in ranges)
        {
            var lines = sources.AsSpan(range).ToArray();

            var address = default(IdaAddress);
            var name = default(string);
            var signature = default(string);
            var file = default(string);
            for (var index = 0; index < lines.Length; index++)
            {
                var input = lines[index];
                var match = RegexImplementation().Match(input);

                if (match.Success)
                {
                    var value = match.Groups[1].Value;
                    var parse = int.Parse(value, NumberStyles.HexNumber);
                    address = new IdaAddress(parse);
                    continue;
                }

                var nameMatch = RegexImplementationName().Match(input);
                if (nameMatch.Success)
                {
                    name = nameMatch.Groups[1].Value;
                    continue;
                }

                var signatureMatch = RegexImplementationSignature().Match(input);
                if (signatureMatch.Success)
                {
                    signature = signatureMatch.Groups[1].Value;
                    continue;
                }

                var addressMatch = RegexImplementationAddress().Match(input);
                if (addressMatch.Success)
                {
                    var value = addressMatch.Groups[1].Value;
                    var parse = int.Parse(value, NumberStyles.HexNumber);
                    address = new IdaAddress(parse);
                    continue;
                }

                var fileMatch = RegexImplementationFile().Match(input);
                if (fileMatch.Success)
                {
                    file = fileMatch.Groups[1].Value;
                    continue;
                }

                break;
            }

            Console.WriteLine($"{address}, {name}, {signature}, {file}");
        }

        return 0;
    }

    private static List<Range> GetFunctionsRanges(string[] sources)
    {
        var ranges = new List<Range>();

        var length = sources.Length;

        for (var i = 0; i < length; i++)
        {
            var start = Array.FindIndex(sources, i, RegexImplementation().IsMatch);

            if (start is -1)
            {
                break;
            }

            var end = Array.FindIndex(sources, start + 1, RegexImplementation().IsMatch);

            if (end is -1)
            {
                end = Array.FindIndex(sources, start + 1, RegexEndOfFile().IsMatch);
            }

            if (end is -1)
            {
                end = length - 1;
            }

            ranges.Add(new Range(start, end));

            i = start;
        }

        return ranges;
    }

    private static List<IdaFunction> ParseImplementationsSource(
        List<IdaText> texts)
    {
        // bug add function text
        // bug add function line
        // bug some functions have no name

        var functions = new List<IdaFunction>(texts.Count);

        foreach (var text in texts)
        {
            var function = new IdaFunction { Text = text };

            foreach (var line in text.Lines)
            {
                var match0 = RegexImplementation().Match(line);

                if (match0.Success)
                {
                    function.Address = match0.Groups[1].Value;
                    continue;
                }

                var match1 = RegexImplementationName().Match(line);

                if (match1.Success)
                {
                    function.Name = match1.Groups[1].Value;
                    continue;
                }

                var match2 = RegexImplementationSignature().Match(line);

                if (match2.Success)
                {
                    function.Signature = match2.Groups[1].Value;
                    continue;
                }

                var match3 = RegexImplementationAddress().Match(line);

                if (match3.Success)
                {
                    function.Address = match3.Groups[1].Value;
                    continue;
                }

                var match4 = RegexImplementationFile().Match(line);

                if (match4.Success)
                {
                    function.File = match4.Groups[1].Value;
                }
            }

            functions.Add(function);
        }

        return functions;
    }

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\sFunction\sname\s=\s(.*)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexImplementationName();

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\sFunction\ssignature\s=\s(.*)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexImplementationSignature();

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\sFunction\saddress\s=\s([A-F0-9]{8})$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexImplementationAddress();

    [GeneratedRegex(@"^//\s\[PSX-MND-SYM\]\sFunction\sfile\s=\s(.*)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexImplementationFile();

    [GeneratedRegex(@"^//-{5}\s\(([A-F0-9]{8})\)\s-{56}$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexImplementation();

    [GeneratedRegex(@"^//\snfuncs=\d+", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RegexEndOfFile();

    public record struct IdaAddress(int Offset)
    {
        public readonly override string ToString()
        {
            return $"0x{Offset:X8}";
        }

        public static implicit operator int(IdaAddress address)
        {
            return address.Offset;
        }
    }
}

public sealed class IdaText
{
    public int Line { get; init; }

    public List<string> Lines { get; init; }
}

public sealed class IdaFunction
{
    public IdaText Text { get; set; }

    public string? Name { get; set; }

    public string? Signature { get; set; }

    public string? Address { get; set; }

    public string? File { get; set; }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}";
    }
}