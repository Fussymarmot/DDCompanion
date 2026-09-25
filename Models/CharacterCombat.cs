// Combat-related character data: health, armor class, weapon and armor.

namespace DDCompanion.Models;

public class CharacterCombat
{
    public int CurrentHealth { get; set; } 
    public int MaxHealth { get; set; }

    public int ArmorClass { get; set; }
    public int Protection { get; set; }

    public string Weapon { get; set; } = string.Empty;
    public string Armor { get; set; } = string.Empty;
}