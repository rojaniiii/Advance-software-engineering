using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace StarChart;

public class StarCollection
{
    public List<Star> Stars { get; set; } = new List<Star>();

    public void AddStar(Star star)
    {
        Stars.Add(star);
    }

    public int GetStarCount()
    {
        return Stars.Count;
    }

    public Star? FindStar(string name)
    {
        return Stars.Find(star => star.Name == name);
    }

public void LoadStars(string filePath)
{
    if (!File.Exists(filePath))
    {
        Console.WriteLine("Star data file not found.");
        return;
    }

    using var reader = new StreamReader(filePath);

    string? headerLine = reader.ReadLine();
    if (headerLine == null)
        return;

    string[] headers = headerLine.Split(',')
    .Select(header => header.Trim().Trim('"'))
    .ToArray();

    int nameIndex = Array.IndexOf(headers, "proper");
    int idIndex = Array.IndexOf(headers, "id");
    int xIndex = Array.IndexOf(headers, "x");
    int yIndex = Array.IndexOf(headers, "y");
    int zIndex = Array.IndexOf(headers, "z");

    if (new[] { nameIndex, idIndex, xIndex, yIndex, zIndex }
        .Any(index => index < 0))
    {
        throw new InvalidDataException("Missing CSV columns.");
    }

    int skipped = 0;

    while (!reader.EndOfStream)
    {
        string? line = reader.ReadLine();

        if (string.IsNullOrWhiteSpace(line))
            continue;

        string[] values = line.Split(',');

        if (values.Length <= new[]
            { nameIndex, idIndex, xIndex, yIndex, zIndex }.Max())
        {
            skipped++;
            continue;
        }

        if (!double.TryParse(values[xIndex],
                NumberStyles.Float, CultureInfo.InvariantCulture,
                out double x) ||
            !double.TryParse(values[yIndex],
                NumberStyles.Float, CultureInfo.InvariantCulture,
                out double y) ||
            !double.TryParse(values[zIndex],
                NumberStyles.Float, CultureInfo.InvariantCulture,
                out double z))
        {
            skipped++;
            continue;
        }

        string name = values[nameIndex].Trim('"');

        if (string.IsNullOrWhiteSpace(name))
            name = "HYG-" + values[idIndex];

        AddStar(new Star(name, x, y, z));
    }

    Console.WriteLine($"Skipped rows: {skipped}");
}

}
