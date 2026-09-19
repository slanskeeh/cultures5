namespace Cultures.Application.Persistence;

public interface ISaveStore
{
    void Write(string slot, string json);
    bool TryRead(string slot, out string json);
}

/// <summary>
/// In-memory slots for tests and headless sessions.
/// </summary>
public sealed class MemorySaveStore : ISaveStore
{
    private readonly Dictionary<string, string> _slots = new(StringComparer.Ordinal);

    public int Count => _slots.Count;

    public void Write(string slot, string json)
    {
        if (string.IsNullOrWhiteSpace(slot))
            throw new ArgumentException("Save slot is required.", nameof(slot));
        _slots[slot] = json ?? throw new ArgumentNullException(nameof(json));
    }

    public bool TryRead(string slot, out string json) => _slots.TryGetValue(slot, out json!);
}

/// <summary>
/// Atomic JSON files on disk. Presentation supplies the directory.
/// </summary>
public sealed class FileSaveStore : ISaveStore
{
    public FileSaveStore(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Save directory is required.", nameof(directory));
        Directory = directory;
        System.IO.Directory.CreateDirectory(Directory);
    }

    public string Directory { get; }

    public void Write(string slot, string json)
    {
        if (string.IsNullOrWhiteSpace(slot))
            throw new ArgumentException("Save slot is required.", nameof(slot));
        ArgumentNullException.ThrowIfNull(json);
        var path = PathFor(slot);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }

    public bool TryRead(string slot, out string json)
    {
        json = "";
        var path = PathFor(slot);
        if (!File.Exists(path))
            return false;
        json = File.ReadAllText(path);
        return true;
    }

    public string PathFor(string slot) => Path.Combine(Directory, $"{Sanitize(slot)}.json");

    private static string Sanitize(string slot)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = slot.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}

public static class SaveSlots
{
    public const string Default = "slot1";
    public const string Autosave = "autosave";
}
