using System.IO;
using System.Text;

namespace GymSys.App.Services;

/// <summary>
/// Penulis CSV untuk ekspor laporan. Pemisah titik koma (;) dan UTF-8 dengan BOM supaya langsung
/// terbuka benar di Excel dengan pengaturan regional Indonesia (FR11).
/// </summary>
public static class CsvExporter
{
    public static void Write(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', headers.Select(Escape)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(';', row.Select(Escape)));

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(string? value)
    {
        value ??= "";

        // Cegah "CSV injection": sel yang diawali = + @ akan dianggap rumus oleh Excel.
        if (value.Length > 0 && (value[0] == '=' || value[0] == '+' || value[0] == '@'))
            value = "'" + value;

        if (value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }
}
