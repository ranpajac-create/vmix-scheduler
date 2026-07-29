using System.Text;

namespace VmixScheduler;

/// <summary>Shared CSV encode/decode so the as-run log writer (Form1) and viewer
/// (AsRunLogViewerForm) always agree on the exact same format.</summary>
public static class CsvUtil
{
    /// <summary>Wraps a field in quotes (doubling any internal quotes) only if it contains a
    /// comma, quote, or newline; otherwise returns it unchanged.</summary>
    public static string EscapeField(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>Parses one CSV line — the inverse of EscapeField/joining fields with commas —
    /// back into its individual fields.</summary>
    public static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = false;
                }
                else current.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }
}
