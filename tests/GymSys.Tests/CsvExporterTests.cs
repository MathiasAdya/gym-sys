using System.Text;
using GymSys.Core.Services;

namespace GymSys.Tests;

public class CsvExporterTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"gymsys-{Guid.NewGuid():N}.csv");

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private string WriteAndReadText(string[] headers, params string[][] rows)
    {
        CsvExporter.Write(_path, headers, rows);
        return File.ReadAllText(_path, Encoding.UTF8);
    }

    [Fact]
    public void Write_UsesSemicolonSeparator()
    {
        var text = WriteAndReadText(new[] { "A", "B" }, new[] { "1", "2" });

        Assert.Contains("A;B", text);
        Assert.Contains("1;2", text);
    }

    [Fact]
    public void Write_StartsWithUtf8Bom_SoExcelReadsItCorrectly()
    {
        CsvExporter.Write(_path, new[] { "A" }, new[] { new[] { "x" } });

        var bytes = File.ReadAllBytes(_path);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+62812")]
    [InlineData("@SUM(A1)")]
    public void Write_NeutralizesFormulaInjection(string input)
    {
        var text = WriteAndReadText(new[] { "Nama" }, new[] { input });

        Assert.Contains("'" + input, text);
    }

    [Fact]
    public void Write_QuotesValuesContainingSeparatorOrQuotes()
    {
        var text = WriteAndReadText(new[] { "Nama" }, new[] { "Budi; \"Jr\"" });

        Assert.Contains("\"Budi; \"\"Jr\"\"\"", text);
    }

    [Fact]
    public void Write_WritesHeaderEvenWhenThereAreNoRows()
    {
        var text = WriteAndReadText(new[] { "Kode", "Nama" });

        Assert.StartsWith("Kode;Nama", text.TrimStart('﻿'));
    }
}
