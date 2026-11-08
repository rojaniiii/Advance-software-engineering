
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarChart;

public class StarNavigation
{
    private readonly List<Star> _destinations;
    private int _selectedIndex = 0;

    // All navigation stars for permanent labels
    public IReadOnlyList<Star> Destinations => _destinations;

    // Currently selected star
    public Star? SelectedStar =>
        _destinations.Count > 0
            ? _destinations[_selectedIndex]
            : null;

    public StarNavigation(StarCollection collection)
    {
        string[] preferredNames =
        {
            "Sirius",
            "Vega",
            "Altair",
            "Procyon",
            "Arcturus",
            "Rigel",
            "Betelgeuse",
            "Capella",
            "Deneb",
            "Polaris"
        };

        var namedStars = collection.GetNamedStars();

        _destinations = preferredNames
            .Select(name => namedStars.FirstOrDefault(
                star => string.Equals(
                    star.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase)))
            .Where(star => star != null)
            .Select(star => star!)
            .ToList();

        Console.WriteLine();
        Console.WriteLine("=== STAR NAVIGATION ===");

        if (_destinations.Count == 0)
        {
            Console.WriteLine(
                "No preferred destination stars were found.");
            return;
        }

        Console.WriteLine("Available destinations:");

        for (int i = 0; i < _destinations.Count; i++)
        {
            Console.WriteLine(
                $"{i + 1}. {_destinations[i].Name}");
        }

        ShowSelection();
    }

    public void SelectNext()
    {
        if (_destinations.Count == 0)
            return;

        _selectedIndex =
            (_selectedIndex + 1) % _destinations.Count;

        ShowSelection();
    }

    public void SelectPrevious()
    {
        if (_destinations.Count == 0)
            return;

        _selectedIndex =
            (_selectedIndex - 1 + _destinations.Count)
            % _destinations.Count;

        ShowSelection();
    }

    public void ShowSelection()
    {
        Star? star = SelectedStar;

        if (star == null)
            return;

        Console.WriteLine();
        Console.WriteLine(
            $"Selected destination: {star.Name}");

        Console.WriteLine(
            $"Coordinates: X={star.X:F2}, " +
            $"Y={star.Y:F2}, " +
            $"Z={star.Z:F2}");

        double distance = Math.Sqrt(
            star.X * star.X +
            star.Y * star.Y +
            star.Z * star.Z);

        Console.WriteLine(
            $"Distance from origin: {distance:F2} parsecs");
    }
}
