namespace PsxSymIdaSrcGen.Tests;

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