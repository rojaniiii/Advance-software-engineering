
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarChart;

public class StarNavigation
{
    private readonly List<Star> _destinations;
    private readonly List<Star> _searchableStars;

    private int _selectedIndex = 0;
    private Star? _searchedStar;

    private const double LightYearsPerParsec = 3.26156;

    public IReadOnlyList<Star> Destinations => _destinations;

    // Includes searched stars as well as regular destinations.
    public Star? SelectedStar =>
        _searchedStar ??
        (_destinations.Count > 0
            ? _destinations[_selectedIndex]
            : null);

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

        _searchableStars = collection.Stars
            .Where(star =>
                !string.IsNullOrWhiteSpace(star.Name))
            .ToList();

        _destinations = preferredNames
            .Select(name => _searchableStars.FirstOrDefault(
                star => string.Equals(
                    star.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase)))
            .Where(star => star != null)
            .Select(star => star!)
            .ToList();

        Console.WriteLine();
        Console.WriteLine("=== STAR NAVIGATION ===");
        Console.WriteLine(
            $"Searchable stars: {_searchableStars.Count}");

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

        _searchedStar = null;

        _selectedIndex =
            (_selectedIndex + 1) % _destinations.Count;

        ShowSelection();
    }

    public void SelectPrevious()
    {
        if (_destinations.Count == 0)
            return;

        _searchedStar = null;

        _selectedIndex =
            (_selectedIndex - 1 + _destinations.Count)
            % _destinations.Count;

        ShowSelection();
    }

    // Search by exact name first, then by name prefix.
    public bool SearchStar(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Console.WriteLine("Please enter a star name.");
            return false;
        }

        string query = searchText.Trim();

        Star? found = _searchableStars.FirstOrDefault(star =>
            string.Equals(
                star.Name,
                query,
                StringComparison.OrdinalIgnoreCase));

        found ??= _searchableStars.FirstOrDefault(star =>
            star.Name.StartsWith(
                query,
                StringComparison.OrdinalIgnoreCase));

        found ??= _searchableStars.FirstOrDefault(star =>
            star.Name.Contains(
                query,
                StringComparison.OrdinalIgnoreCase));

        if (found == null)
        {
            Console.WriteLine(
                $"No star found matching '{query}'.");
            return false;
        }

        _searchedStar = found;

        Console.WriteLine(
            $"Search result: {found.Name}");

        ShowSelection();

        return true;
    }

    public void ShowSelection()
    {
        ShowSelection(0, 0, 0);
    }

    public void ShowSelection(
        double cameraX,
        double cameraY,
        double cameraZ)
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

        double dx = star.X - cameraX;
        double dy = star.Y - cameraY;
        double dz = star.Z - cameraZ;

        double distanceParsecs = Math.Sqrt(
            dx * dx + dy * dy + dz * dz);

        double distanceLightYears =
            distanceParsecs * LightYearsPerParsec;

        double bearing =
            Math.Atan2(dx, dy) * 180.0 / Math.PI;

        bearing = (bearing + 360.0) % 360.0;

        double horizontalDistance = Math.Sqrt(
            dx * dx + dy * dy);

        double pathAngle = Math.Atan2(
            dz,
            horizontalDistance) * 180.0 / Math.PI;

        Console.WriteLine(
            $"Distance: {distanceParsecs:F2} parsecs " +
            $"({distanceLightYears:F2} light-years)");

        Console.WriteLine(
            $"Bearing: {bearing:F2} degrees");

        Console.WriteLine(
            $"Path/elevation: {pathAngle:F2} degrees");
    }
}
