using System.Globalization;
using System.Text.RegularExpressions;

// ReSharper disable NotAccessedPositionalProperty.Global

// ReSharper disable StringLiteralTypo

namespace PsxSymIdaSrcGen.Tests;

[TestClass]
public class UnitTest4
{
    [TestMethod]
    public void TestParseDumpSymOutput()
    {
        const string path = @"C:\GitHub\psx_mnd_sym\cmd\sym_dump\MAIN.TXT";

        using var reader = new StreamReader(File.OpenRead(path));

        var lineIndex = 0;

        var regex1 = new Regex(@"^([a-f0-9]{6}):\s\$([a-f0-9]{8})\s(\d{2})");

        var symbols = new List<Symbol>();

        var parsers = new List<SymbolParser>
        {
            new SymbolSetSldToLineOfLineParser(),
            new SymbolIncSldLineNumByByteParser(),
            new SymbolIncSldLineNumToParser(),
            new SymbolSetSldLineNumToParser(),
            new SymbolDef1Parser(),
            new SymbolDef2Parser()
        };

        while (true)
        {
            var line = reader.ReadLine();

            if (line == null)
            {
                break;
            }

            lineIndex++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var match1 = regex1.Match(line);

            if (match1.Success is false)
            {
                continue;
            }

            var offset = int.Parse(match1.Groups[1].Value, NumberStyles.HexNumber);

            var header = new SymbolHeader(
                int.Parse(match1.Groups[2].Value, NumberStyles.HexNumber),
                int.Parse(match1.Groups[3].Value));

            var result = default(Symbol);

            foreach (var parser in parsers)
            {
                if (parser.TryParse(line, out result))
                {
                    break;
                }
            }

            if (result == null)
            {
                throw new NotImplementedException($"{lineIndex}: {line}");
            }

            symbols.Add(result);
        }
    }
}

public abstract record Symbol;

public sealed record SymbolHeader(int Address, int Type)
{
    public override string ToString()
    {
        return $"${Address:x8} {Type:D2}";
    }
}

public abstract class SymbolParser
{
    public abstract bool TryParse(string input, out Symbol result);
}

public sealed record SymbolSetSldToLineOfLine(int Line, string File) : Symbol
{
    public override string ToString()
    {
        return $"{File}, {Line}";
    }
}

public sealed partial class SymbolSetSldToLineOfLineParser : SymbolParser
{
    public override bool TryParse(string input, out Symbol result)
    {
        result = default!;

        var match = MyRegex().Match(input);

        if (match.Success is false)
        {
            return false;
        }

        var line = int.Parse(match.Groups[1].Value);

        var file = match.Groups[2].Value;

        result = new SymbolSetSldToLineOfLine(line, file);

        return true;
    }

    [GeneratedRegex(@"Set\sSLD\sto\sline\s(\d+)\sof\sfile\s(.*)$")]
    private static partial Regex MyRegex();
}

public sealed record SymbolIncSldLineNumByByte(int Value1, int Value2) : Symbol
{
    public override string ToString()
    {
        return $"{nameof(Value1)}: {Value1}, {nameof(Value2)}: {Value2}";
    }
}

public sealed partial class SymbolIncSldLineNumByByteParser : SymbolParser
{
    public override bool TryParse(string input, out Symbol result)
    {
        result = default!;

        var match = MyRegex().Match(input);

        if (match.Success is false)
        {
            return false;
        }

        var value1 = int.Parse(match.Groups[1].Value);

        var value2 = int.Parse(match.Groups[2].Value);

        result = new SymbolIncSldLineNumByByte(value1, value2);

        return true;
    }

    [GeneratedRegex(@"Inc\sSLD\slinenum\sby\sbyte\s(\d+)\s\(to\s(\d+)\)$")]
    private static partial Regex MyRegex();
}

public sealed record SymbolIncSldLineNumTo(int Line) : Symbol;

public sealed partial class SymbolIncSldLineNumToParser : SymbolParser
{
    public override bool TryParse(string input, out Symbol result)
    {
        result = default!;

        var match = MyRegex().Match(input);

        if (match.Success is false)
        {
            return false;
        }

        var line = int.Parse(match.Groups[1].Value);

        result = new SymbolIncSldLineNumTo(line);

        return true;
    }

    [GeneratedRegex(@"Inc\sSLD\slinenum\s\(to\s(\d+)\)$")]
    private static partial Regex MyRegex();
}

public sealed record SymbolSetSldLineNumTo(int Line) : Symbol;

public sealed partial class SymbolSetSldLineNumToParser : SymbolParser
{
    public override bool TryParse(string input, out Symbol result)
    {
        result = default!;

        var match = MyRegex().Match(input);

        if (match.Success is false)
        {
            return false;
        }

        var line = int.Parse(match.Groups[1].Value);

        result = new SymbolSetSldLineNumTo(line);

        return true;
    }

    [GeneratedRegex(@"Set\sSLD\slinenum\sto\s(\d+)$")]
    private static partial Regex MyRegex();
}

public sealed record SymbolDef1(string Kind, string Type, int Size, string Name) : Symbol;

public sealed partial class SymbolDef1Parser : SymbolParser
{
    public override bool TryParse(string input, out Symbol result)
    {
        result = default!;

        var match = MyRegex().Match(input);

        if (match.Success is false)
        {
            return false;
        }

        var kind = match.Groups[1].Value;
        var type = match.Groups[2].Value;
        var size = match.Groups[3].Value;
        var name = match.Groups[4].Value;

        result = new SymbolDef1(kind, type, int.Parse(size), name);

        return true;
    }

    [GeneratedRegex(@"Def\sclass\s(\w+)\stype\s(\w+(?:\s\w+)*)\ssize\s(\d+)\sname\s(\S+)$")]
    private static partial Regex MyRegex();
}

public sealed record SymbolDef2(string Kind, string Type, int Size, int[] Dims, string Tag, string Name) : Symbol;

public sealed partial class SymbolDef2Parser : SymbolParser
{
    public override bool TryParse(string input, out Symbol result)
    {
        result = default!;

        var match = MyRegex().Match(input);

        if (match.Success is false)
        {
            return false;
        }

        var kind = match.Groups[1].Value;
        var type = match.Groups[2].Value;
        var size = int.Parse(match.Groups[3].Value);
        var dims = match.Groups[4].Captures.Select(s => int.Parse(s.Value)).ToArray();
        var tag = match.Groups[5].Value;
        var name = match.Groups[6].Value;

        result = new SymbolDef2(kind, type, size, dims, tag, name);

        return true;
    }

    [GeneratedRegex(@"Def2\sclass\s(\w+)\stype\s(\w+(?:\s\w+)*)\ssize\s(\d+)\sdims\s(?:\s?(\d+))+\stag\s(\S*)\sname\s(\S+)$")]
    private static partial Regex MyRegex();
}