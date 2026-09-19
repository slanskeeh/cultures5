using Cultures.Buildings;
using Cultures.Population;
using Cultures.World;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Procedural pixel textures for debug presentation. Not final art.
/// </summary>
public static class SimpleTextures
{
    public const int Tile = 32;

    private static readonly Dictionary<string, ImageTexture> Cache = new();

    public static Texture2D Biome(BiomeId biome) => Get($"biome-{(int)biome}", image => PaintBiome(image, biome));

    public static Texture2D Building(BuildingTypeId type) => Get($"b-{type.Value}", image => PaintBuilding(image, type));

    public static Texture2D Character(CharacterLifeStage stage, bool selected) =>
        Get($"c-{(int)stage}-{(selected ? 1 : 0)}", image => PaintCharacter(image, stage, selected));

    private static Texture2D Get(string key, Action<Image> paint)
    {
        if (Cache.TryGetValue(key, out var existing))
            return existing;
        var image = Image.CreateEmpty(Tile, Tile, false, Image.Format.Rgba8);
        paint(image);
        var texture = ImageTexture.CreateFromImage(image);
        Cache[key] = texture;
        return texture;
    }

    private static void PaintBiome(Image image, BiomeId biome)
    {
        var baseColor = biome switch
        {
            BiomeId.Ocean => new Color(0.14f, 0.32f, 0.58f),
            BiomeId.Ice => new Color(0.82f, 0.90f, 0.95f),
            BiomeId.Tundra => new Color(0.48f, 0.54f, 0.46f),
            BiomeId.TemperateLand => new Color(0.34f, 0.56f, 0.24f),
            BiomeId.Forest => new Color(0.14f, 0.36f, 0.16f),
            BiomeId.Desert => new Color(0.76f, 0.64f, 0.32f),
            BiomeId.Highland => new Color(0.46f, 0.40f, 0.32f),
            BiomeId.Swamp => new Color(0.22f, 0.38f, 0.28f),
            BiomeId.Savanna => new Color(0.62f, 0.58f, 0.28f),
            BiomeId.Taiga => new Color(0.18f, 0.34f, 0.28f),
            _ => new Color(0.22f, 0.22f, 0.22f)
        };
        var speckle = biome switch
        {
            BiomeId.Ocean => new Color(0.20f, 0.42f, 0.68f),
            BiomeId.Forest => new Color(0.08f, 0.26f, 0.10f),
            BiomeId.Desert => new Color(0.86f, 0.74f, 0.42f),
            _ => baseColor.Lightened(0.08f)
        };
        for (var y = 0; y < Tile; y++)
        {
            for (var x = 0; x < Tile; x++)
            {
                var hatch = ((x * 3) + (y * 5) + (int)biome) % 7 == 0;
                image.SetPixel(x, y, hatch ? speckle : baseColor);
            }
        }
    }

    private static void PaintBuilding(Image image, BuildingTypeId type)
    {
        var fill = type.Value switch
        {
            "farm" => new Color(0.58f, 0.74f, 0.28f),
            "storage" => new Color(0.74f, 0.58f, 0.26f),
            "shelter" => new Color(0.64f, 0.48f, 0.36f),
            "hunting_camp" => new Color(0.55f, 0.38f, 0.22f),
            "fishery" => new Color(0.28f, 0.52f, 0.62f),
            _ => new Color(0.50f, 0.50f, 0.60f)
        };
        image.Fill(new Color(0, 0, 0, 0));
        for (var y = 6; y < 28; y++)
        {
            for (var x = 6; x < 26; x++)
                image.SetPixel(x, y, fill);
        }

        var roof = fill.Darkened(0.25f);
        for (var x = 4; x < 28; x++)
        {
            var peak = Math.Abs(x - 16);
            for (var y = 2; y < 10 - peak / 3; y++)
            {
                if (y >= 0 && y < Tile)
                    image.SetPixel(x, Math.Max(0, y), roof);
            }
        }
    }

    private static void PaintCharacter(Image image, CharacterLifeStage stage, bool selected)
    {
        image.Fill(new Color(0, 0, 0, 0));
        var body = stage switch
        {
            CharacterLifeStage.Infant => new Color(0.95f, 0.78f, 0.55f),
            CharacterLifeStage.Child => new Color(0.90f, 0.70f, 0.42f),
            CharacterLifeStage.Elder => new Color(0.72f, 0.68f, 0.58f),
            CharacterLifeStage.Dead => new Color(0.30f, 0.30f, 0.30f),
            _ => new Color(0.86f, 0.62f, 0.38f)
        };
        var cx = 16;
        var cy = 18;
        var radius = stage == CharacterLifeStage.Infant ? 5 : 8;
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            for (var x = cx - radius; x <= cx + radius; x++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                    image.SetPixel(x, y, body);
            }
        }

        if (selected)
        {
            var ring = new Color(1f, 1f, 0.7f);
            for (var i = 0; i < 16; i++)
            {
                var a = i / 16f * MathF.Tau;
                var x = cx + (int)MathF.Round(MathF.Cos(a) * (radius + 3));
                var y = cy + (int)MathF.Round(MathF.Sin(a) * (radius + 3));
                if (x is >= 0 and < Tile && y is >= 0 and < Tile)
                    image.SetPixel(x, y, ring);
            }
        }
    }
}
