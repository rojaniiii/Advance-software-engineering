using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
    // Return stars that have proper names rather than generated HYG IDs
public List<Star> GetNamedStars()
{
    return Stars
        .Where(star =>
            !string.IsNullOrWhiteSpace(star.Name) &&
            !star.Name.StartsWith(
                "HYG-",
                StringComparison.OrdinalIgnoreCase))
        .ToList();
}

    public Star? FindStar(string name)
    {
        return Stars.Find(star =>
            star.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
        int spectralIndex = Array.IndexOf(headers, "spect");

        if (new[]
            {
                nameIndex,
                idIndex,
                xIndex,
                yIndex,
                zIndex,
                spectralIndex
            }.Any(index => index < 0))
        {
            throw new InvalidDataException(
                "Missing required CSV columns.");
        }

        int skipped = 0;

        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] values = line.Split(',');

            if (values.Length <= new[]
                {
                    nameIndex,
                    idIndex,
                    xIndex,
                    yIndex,
                    zIndex,
                    spectralIndex
                }.Max())
            {
                skipped++;
                continue;
            }

            if (!double.TryParse(
                    values[xIndex],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double x) ||

                !double.TryParse(
                    values[yIndex],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double y) ||

                !double.TryParse(
                    values[zIndex],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double z))
            {
                skipped++;
                continue;
            }

            string name = values[nameIndex].Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "HYG-" + values[idIndex].Trim().Trim('"');
            }

            // Read spectral classification
            string spectralType =
                values[spectralIndex].Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(spectralType))
            {
                spectralType = "Unknown";
            }

            AddStar(new Star(
                name,
                x,
                y,
                z,
                spectralType
            ));
        }

        Console.WriteLine($"Skipped rows: {skipped}");
        Console.WriteLine($"Loaded {Stars.Count} stars with spectral data.");
    }
}
