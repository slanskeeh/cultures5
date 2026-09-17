using System.Text;

namespace Cultures.Civilization;

/// <summary>
/// Deterministic invented names from seed + salt. Not a language system (OD-032).
/// </summary>
public static class FictionalName
{
    private static readonly string[] Onsets = ["", "v", "k", "r", "d", "n", "t", "l", "s", "m", "g", "h", "z"];
    private static readonly string[] Nuclei = ["a", "e", "i", "o", "u", "ae", "ar", "el", "or", "un"];
    private static readonly string[] Codas = ["", "n", "r", "l", "k", "th", "s"];

    public static string Word(ulong seed, ulong salt, int syllables)
    {
        syllables = Math.Clamp(syllables, 2, 3);
        var h = Mix(seed, salt);
        var text = new StringBuilder(12);
        for (var i = 0; i < syllables; i++)
        {
            h = Mix(h, (ulong)(i + 1));
            text.Append(Onsets[Index(h, Onsets.Length)]);
            h = Mix(h, 0xA24BAED4963EE407UL);
            text.Append(Nuclei[Index(h, Nuclei.Length)]);
            h = Mix(h, 0x9FB21C651E98DF25UL);
            text.Append(Codas[Index(h, Codas.Length)]);
        }

        if (text.Length == 0)
            return "Ara";

        text[0] = char.ToUpperInvariant(text[0]);
        return text.ToString();
    }

    public static CultureTraits Traits(ulong seed, ulong salt)
    {
        var h = Mix(seed, salt ^ 0xD1B54A32D192ED03UL);
        byte Next()
        {
            h = Mix(h, 0x94D049BB133111EBUL);
            return (byte)(h % (ulong)CivilizationRules.TraitBand);
        }

        return new CultureTraits(Next(), Next(), Next(), Next(), Next());
    }

    private static int Index(ulong hash, int length) => (int)(hash % (ulong)length);

    private static ulong Mix(ulong seed, ulong salt)
    {
        unchecked
        {
            var h = seed ^ (salt * 0x9E3779B97F4A7C15UL);
            h = (h ^ (h >> 30)) * 0xBF58476D1CE4E5B9UL;
            h = (h ^ (h >> 27)) * 0x94D049BB133111EBUL;
            return h ^ (h >> 31);
        }
    }
}
