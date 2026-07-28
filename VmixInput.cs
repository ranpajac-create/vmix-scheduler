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

    /// <summary>The underlying media file name, extension stripped, when vMix's "title" actually
    /// carries one. For list-type inputs vMix composes title as "{ShortTitle} - {current file}" —
    /// that prefix is stripped off here. For a plain single-file input, once renamed, vMix's title
    /// becomes identical to the rename and there's no original file name left anywhere in the API
    /// — in that case this returns null rather than the rename itself.</summary>
    public string? FileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Title)) return null;
            if (string.Equals(Title, ShortTitle, StringComparison.Ordinal)) return null;

            var text = Title;
            if (!string.IsNullOrEmpty(ShortTitle))
            {
                var prefix = $"{ShortTitle} - ";
                if (text.StartsWith(prefix, StringComparison.Ordinal))
                    text = text.Substring(prefix.Length);
            }
            return FileDisplayName(text);
        }
    }

    /// <summary>vMix list items are full file paths; show just the filename without extension.</summary>
    private static string? FileDisplayName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        try { return Path.GetFileNameWithoutExtension(path); }
        catch (ArgumentException) { return path; }
    }

    public override string ToString() => $"{Number}: {Name} ({Type})";
}
