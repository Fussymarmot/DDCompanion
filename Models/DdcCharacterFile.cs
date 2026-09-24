namespace DDCompanion.Models;

public sealed class DdcCharacterFile
{
    public string Format { get; set; } = "DDC";
    public int Version { get; set; } = 2;
    public Character Character { get; set; } = new();
}
