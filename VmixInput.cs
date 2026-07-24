namespace VmixScheduler;

public class VmixInput
{
    public string Key { get; set; } = "";
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public string ShortTitle { get; set; } = "";
    public string Type { get; set; } = "";
    public string State { get; set; } = "";
    public int Position { get; set; }
    public int Duration { get; set; }
    public List<string> ListItems { get; set; } = new();
    public int SelectedIndex { get; set; } = -1;

    /// <summary>The name an operator actually sets when renaming an input in vMix (falls back to the full title if unset).</summary>
    public string Name => string.IsNullOrWhiteSpace(ShortTitle) ? Title : ShortTitle;

    public string? CurrentSongTitle => FileDisplayName(SelectedIndex >= 0 && SelectedIndex < ListItems.Count ? ListItems[SelectedIndex] : null);
    public string? NextSongTitle => FileDisplayName(SelectedIndex >= 0 && SelectedIndex + 1 < ListItems.Count ? ListItems[SelectedIndex + 1] : null);

    /// <summary>The underlying media file name, extension stripped — vMix's raw "title" is always
    /// the original file/media name, unaffected by an operator's rename (ShortTitle).</summary>
    public string? FileName => FileDisplayName(Title);

    /// <summary>vMix list items are full file paths; show just the filename without extension.</summary>
    private static string? FileDisplayName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        try { return Path.GetFileNameWithoutExtension(path); }
        catch (ArgumentException) { return path; }
    }

    public override string ToString() => $"{Number}: {Name} ({Type})";
}
