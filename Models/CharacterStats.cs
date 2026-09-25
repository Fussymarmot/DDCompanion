// D&D ability scores (STR/DEX/CON/INT/WIS/CHA) with derived modifiers.

namespace DDCompanion.Models;

public class CharacterStats
{
    public int Strength { get; set; }
    public int StrengthModifier { get; set; }

    public int Dexterity { get; set; }
    public int DexterityModifier { get; set; }

    public int Constitution { get; set; }
    public int ConstitutionModifier { get; set; }

    public int Intelligence { get; set; }
    public int IntelligenceModifier { get; set; }

    public int Wisdom { get; set; }
    public int WisdomModifier { get; set; }

    public int Charisma { get; set; }
    public int CharismaModifier { get; set; }

    public void RecalculateModifiers()
    {
        StrengthModifier = CalculateModifier(Strength);
        DexterityModifier = CalculateModifier(Dexterity);
        ConstitutionModifier = CalculateModifier(Constitution);
        IntelligenceModifier = CalculateModifier(Intelligence);
        WisdomModifier = CalculateModifier(Wisdom);
        CharismaModifier = CalculateModifier(Charisma);
    }

    private static int CalculateModifier(int value) => (value - 10) / 2;
}
