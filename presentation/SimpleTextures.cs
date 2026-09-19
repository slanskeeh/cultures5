using Cultures.Buildings;
using Cultures.Population;
using Cultures.World;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Procedural 2.5D hex textures. Not final art.
/// </summary>
public static class SimpleTextures
{
    public const int Tile = 64;
    private const float Size = 22f;
    private const float VerticalScale = 0.8660254f;

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
        image.Fill(new Color(0, 0, 0, 0));
        paint(image);
        var texture = ImageTexture.CreateFromImage(image);
        Cache[key] = texture;
        return texture;
    }

    private static void PaintBiome(Image image, BiomeId biome)
    {
        var top = biome switch
        {
            BiomeId.Ocean => new Color(0.16f, 0.38f, 0.64f),
            BiomeId.Ice => new Color(0.84f, 0.91f, 0.96f),
            BiomeId.Tundra => new Color(0.50f, 0.56f, 0.48f),
            BiomeId.TemperateLand => new Color(0.36f, 0.58f, 0.26f),
            BiomeId.Forest => new Color(0.16f, 0.38f, 0.18f),
            BiomeId.Desert => new Color(0.78f, 0.66f, 0.34f),
            BiomeId.Highland => new Color(0.48f, 0.42f, 0.34f),
            BiomeId.Swamp => new Color(0.24f, 0.40f, 0.30f),
            BiomeId.Savanna => new Color(0.64f, 0.60f, 0.30f),
            BiomeId.Taiga => new Color(0.20f, 0.36f, 0.30f),
            _ => new Color(0.24f, 0.24f, 0.24f)
        };
        var side = top.Darkened(0.35f);
        var speckle = top.Lightened(0.10f);
        PaintSolidHex(image, top, side, speckle, (int)biome);
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
        PaintSolidHex(image, fill, fill.Darkened(0.40f), fill.Lightened(0.12f), type.Value.Length, size: 16, cy: 30);
        for (var x = 24; x < 40; x++)
        {
            for (var y = 14; y < 22; y++)
                image.SetPixel(x, y, fill.Darkened(0.15f));
        }
    }

    private static void PaintCharacter(Image image, CharacterLifeStage stage, bool selected)
    {
        var body = stage switch
        {
            CharacterLifeStage.Infant => new Color(0.95f, 0.78f, 0.55f),
            CharacterLifeStage.Child => new Color(0.90f, 0.70f, 0.42f),
            CharacterLifeStage.Elder => new Color(0.72f, 0.68f, 0.58f),
            CharacterLifeStage.Dead => new Color(0.30f, 0.30f, 0.30f),
            _ => new Color(0.86f, 0.62f, 0.38f)
        };
        var cx = 32;
        var cy = 30;
        for (var y = cy + 8; y <= cy + 12; y++)
        {
            for (var x = cx - 8; x <= cx + 10; x++)
            {
                if ((x - cx - 2) * (x - cx - 2) / 4 + (y - cy - 10) * (y - cy - 10) <= 8)
                    image.SetPixel(x, y, new Color(0, 0, 0, 0.35f));
            }
        }

        var radius = stage == CharacterLifeStage.Infant ? 5 : 8;
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            for (var x = cx - radius; x <= cx + radius; x++)
            {
                var dx = x - cx;
                var dy = (y - cy) * 1.15f;
                if (dx * dx + dy * dy <= radius * radius)
                    image.SetPixel(x, y, y > cy ? body.Darkened(0.18f) : body);
            }
        }

        if (!selected)
            return;
        var ring = new Color(1f, 1f, 0.7f);
        for (var i = 0; i < 16; i++)
        {
            var a = i / 16f * MathF.Tau;
            var x = cx + (int)MathF.Round(MathF.Cos(a) * (radius + 3));
            var y = cy + (int)MathF.Round(MathF.Sin(a) * (radius + 3) * 0.72f);
            if (x is >= 0 and < Tile && y is >= 0 and < Tile)
                image.SetPixel(x, y, ring);
        }
    }

    private static void PaintSolidHex(
        Image image,
        Color top,
        Color side,
        Color speckle,
        int salt,
        float size = Size,
        int cy = 26)
    {
        const int cx = 32;
        const int extrude = 8;
        var verts = HexVerts(cx, cy, size);
        var shadow = HexVerts(cx + 4, cy + 6, size);
        FillPolygon(image, shadow, new Color(0f, 0f, 0f, 0.32f));

        var bottom = HexVerts(cx, cy + extrude, size);
        for (var i = 2; i <= 4; i++)
        {
            var a = verts[i];
            var b = verts[(i + 1) % 6];
            var c = bottom[(i + 1) % 6];
            var d = bottom[i];
            FillPolygon(image, [a, b, c, d], i == 3 ? side.Darkened(0.12f) : side);
        }

        FillPolygon(image, verts, top);
        for (var y = 0; y < Tile; y++)
        {
            for (var x = 0; x < Tile; x++)
            {
                if (image.GetPixel(x, y).A < 0.5f || image.GetPixel(x, y).A < 0.9f && image.GetPixel(x, y).R < 0.05f)
                    continue;
                if (((x * 3) + (y * 5) + salt) % 11 != 0)
                    continue;
                var pixel = image.GetPixel(x, y);
                if (pixel.A > 0.9f && Math.Abs(pixel.R - top.R) < 0.2f)
                    image.SetPixel(x, y, speckle);
            }
        }

        FillPolygon(image, [verts[5], verts[0], verts[1]], new Color(top.Lightened(0.14f), 0.35f), blend: true);
    }

    private static Vector2[] HexVerts(float cx, float cy, float size)
    {
        var verts = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = (60 * i - 30) * MathF.PI / 180f;
            verts[i] = new Vector2(
                cx + size * MathF.Cos(angle),
                cy + size * MathF.Sin(angle) * VerticalScale);
        }

        return verts;
    }

    private static void FillPolygon(Image image, Vector2[] verts, Color color, bool blend = false)
    {
        var minX = Math.Max(0, (int)MathF.Floor(verts.Min(v => v.X)));
        var maxX = Math.Min(Tile - 1, (int)MathF.Ceiling(verts.Max(v => v.X)));
        var minY = Math.Max(0, (int)MathF.Floor(verts.Min(v => v.Y)));
        var maxY = Math.Min(Tile - 1, (int)MathF.Ceiling(verts.Max(v => v.Y)));
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (!PointInPolygon(x + 0.5f, y + 0.5f, verts))
                    continue;
                if (blend)
                {
                    var previous = image.GetPixel(x, y);
                    image.SetPixel(x, y, previous.Lerp(color, color.A));
                    continue;
                }

                image.SetPixel(x, y, color);
            }
        }
    }

    private static bool PointInPolygon(float x, float y, Vector2[] verts)
    {
        var inside = false;
        for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
        {
            var yi = verts[i].Y;
            var yj = verts[j].Y;
            var xi = verts[i].X;
            var xj = verts[j].X;
            if ((yi > y) == (yj > y))
                continue;
            var intersect = (xj - xi) * (y - yi) / (yj - yi + 0.0001f) + xi;
            if (x < intersect)
                inside = !inside;
        }

        return inside;
    }
}
