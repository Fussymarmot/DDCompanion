using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace DDCompanion.Models;

/// <summary>
/// Editable quick-pick suggestions shown under the "Раса" and "Класс" fields.
/// User changes are stored locally in MAUI Preferences.
/// </summary>
public static class CharacterOptions
{
    private const string RacesKey = "character_options_races";
    private const string ClassesKey = "character_options_classes";

    private static readonly string[] DefaultRaces =
    {
        "Ааракокра", "Аасимар", "Автогном", "Астральный эльф", "Багбир", 
        "Ведалкен", "Гибрид Симиков", "Гит", "Гифф", "Гном", "Гоблин", 
        "Голиаф", "Грунг", "Дварф", "Дженази", "Драконорождённый", 
        "Зайцегон", "Калаштар", "Кендер", "Кенку", "Кентавр", "Кобольд", 
        "Кованый", "Леонин", "Локата", "Локсодон", "Людоящер", "Минотавр", 
        "Орк", "Плазмоид", "Полуорк", "Полурослик", "Полуэльф", "Сатир", 
        "Совлин", "Табакси", "Тифлинг", "Тортл", "Три-крин", "Тритон", 
        "Фирболг", "Фейри", "Хадози", "Хобгоблин", "Чейнджлинг", "Человек", 
        "Шифтер", "Эльф", "Юань-ти"
    };

    private static readonly string[] DefaultClasses =
    {
        "Бард", "Варвар", "Воин", "Волшебник", "Друид", "Жрец",
        "Изобретатель", "Колдун", "Монах", "Паладин", "Плут", "Следопыт",
        "Чародей"
    };

    public static IReadOnlyList<string> Races => Load(RacesKey, DefaultRaces);
    public static IReadOnlyList<string> Classes => Load(ClassesKey, DefaultClasses);

    public static bool AddRace(string value) => Add(RacesKey, DefaultRaces, value);
    public static bool AddClass(string value) => Add(ClassesKey, DefaultClasses, value);
    public static void RemoveRace(string value) => Remove(RacesKey, DefaultRaces, value);
    public static void RemoveClass(string value) => Remove(ClassesKey, DefaultClasses, value);
    public static void ReplaceRaces(IEnumerable<string> values) => Replace(RacesKey, values);
    public static void ReplaceClasses(IEnumerable<string> values) => Replace(ClassesKey, values);

    private static IReadOnlyList<string> Load(string key, string[] defaults)
    {
        var json = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return Sort(defaults);

        try
        {
            return Sort(JsonSerializer.Deserialize<string[]>(json) ?? defaults);
        }
        catch
        {
            return Sort(defaults);
        }
    }

    private static bool Add(string key, string[] defaults, string value)
    {
        value = NormalizeOption(value);
        if (string.IsNullOrWhiteSpace(value)) return false;

        var items = Load(key, defaults).ToList();
        if (items.Any(x => string.Equals(x, value, StringComparison.CurrentCultureIgnoreCase)))
            return false;

        items.Add(value);
        Save(key, items);
        return true;
    }


    private static string NormalizeOption(string value)
    {
        value = value.Trim();
        if (value.Length == 0) return value;

        var culture = CultureInfo.GetCultureInfo("ru-RU");
        return char.ToUpper(value[0], culture) + value[1..];
    }

    private static void Remove(string key, string[] defaults, string value)
    {
        var items = Load(key, defaults)
            .Where(x => !string.Equals(x, value, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        Save(key, items);
    }

    private static void Replace(string key, IEnumerable<string> values)
    {
        var normalized = values
            .Select(NormalizeOption)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        Save(key, normalized);
    }

    private static void Save(string key, IEnumerable<string> values) =>
        Preferences.Default.Set(key, JsonSerializer.Serialize(Sort(values.ToArray())));

    private static IReadOnlyList<string> Sort(string[] values)
    {
        Array.Sort(values, StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: false));
        return values;
    }
}
