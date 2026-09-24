namespace DDCompanion.Models;

public class Character
{
    public string ImageBase64 { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Worldview { get; set; } = string.Empty;

    public string BodyType { get; set; } = string.Empty;
    public string Backstory { get; set; } = string.Empty;

    public CharacterStats Stats { get; set; } = new();
    public CharacterCombat Combat { get; set; } = new();

    public List<CharacterSkill> Skills { get; set; } = [];
    public List<CharacterAbility> Abilities { get; set; } = [];
    public List<CharacterItem> Inventory { get; set; } = [];
}