using System.Text.RegularExpressions;

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
        var regex2 = new Regex(@"Set\sSLD\sto\sline\s(\d+)\sof\sfile\s(.*)$");
        var regex3 = new Regex(@"Inc\sSLD\slinenum\sby\sbyte\s(\d+)\s\(to\s(\d+)\)$");
        var regex4 = new Regex(@"Inc\sSLD\slinenum\s\(to\s(\d+)\)$");
        var regex5 = new Regex(@"Set\sSLD\slinenum\sto\s(\d+)$");

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

            var elementOffset = match1.Groups[1].Value;
            var elementAddress = match1.Groups[2].Value;
            var elementType = match1.Groups[3].Value;

            var match2 = regex2.Match(line);
            if (match2.Success)
            {
                var setSldLine = match2.Groups[1].Value;
                var setSldFile = match2.Groups[2].Value;
                Console.WriteLine($"Set SLD to line {setSldLine} of file {setSldFile}");
                continue;
            }

            var match3 = regex3.Match(line);
            if (match3.Success)
            {
                var incSldValue1 = match3.Groups[1].Value;
                var incSldValue2 = match3.Groups[2].Value;
                Console.WriteLine($"Inc SLD linenum by byte {incSldValue1} (to {incSldValue2})");
                continue;
            }

            var match4 = regex4.Match(line);
            if (match4.Success)
            {
                var incSldValue1 = match4.Groups[1].Value;
                Console.WriteLine($"Inc SLD linenum (to {incSldValue1})");
                continue;
            }

            var match5 = regex5.Match(line);
            if (match5.Success)
            {
                var incSldValue1 = match5.Groups[1].Value;
                Console.WriteLine($"Set SLD linenum to {incSldValue1}");
                continue;
            }

            throw new NotImplementedException($"{lineIndex}: {line}");
        }
    }
}