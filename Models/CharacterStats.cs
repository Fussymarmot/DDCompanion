namespace DDCompanion.Models;

public class CharacterStats
{
    public int Strength { get; set; } //сила
    public int StrengthModifier { get; set; }

    public int Dexterity { get; set; } // ловкость
    public int DexterityModifier { get; set; }

    public int Constitution { get; set; } // телосложение
    public int ConstitutionModifier { get; set; }

    public int Intelligence { get; set; } // интелект
    public int IntelligenceModifier { get; set; }

    public int Wisdom { get; set; } //мудрость
    public int WisdomModifier { get; set; }

    public int Charisma { get; set; } //харизма
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
