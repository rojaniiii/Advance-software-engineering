
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

    private const int MaxDestinations = 10;
    private const double LightYearsPerParsec = 3.26156;

    public IReadOnlyList<Star> Destinations => _destinations;

    public Star? SelectedStar =>
        _searchedStar ??
        (_destinations.Count > 0
            ? _destinations[_selectedIndex]
            : null);

    public StarNavigation(StarCollection collection)
    {
        string[] preferredNames =
        {
            "Sirius", "Vega", "Altair", "Procyon",
            "Arcturus", "Rigel", "Betelgeuse",
            "Capella", "Deneb", "Polaris"
        };

        _searchableStars = collection.Stars
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .ToList();

        _destinations = preferredNames
            .Select(name => _searchableStars.FirstOrDefault(s =>
                string.Equals(s.Name, name,
                    StringComparison.OrdinalIgnoreCase)))
            .Where(s => s != null)
            .Select(s => s!)
            .ToList();

        Console.WriteLine("=== STAR NAVIGATION ===");
        Console.WriteLine($"Searchable stars: {_searchableStars.Count}");
        ShowDestinationList();
    }

    public void ShowDestinationList()
    {
        Console.WriteLine();
        Console.WriteLine("=== NAVIGATION DESTINATIONS ===");

        for (int i = 0; i < _destinations.Count; i++)
        {
            string selected =
                _searchedStar == null && i == _selectedIndex
                    ? " < SELECTED"
                    : "";

            Console.WriteLine(
                $"{i + 1}. {_destinations[i].Name}{selected}");
        }

        if (_searchedStar != null)
        {
            Console.WriteLine(
                $"Current selection: {_searchedStar.Name}");
        }
    }

    public void SelectNext()
    {
        if (_destinations.Count == 0) return;

        _searchedStar = null;
        _selectedIndex =
            (_selectedIndex + 1) % _destinations.Count;

        ShowSelection();
    }

    public void SelectPrevious()
    {
        if (_destinations.Count == 0) return;

        _searchedStar = null;
        _selectedIndex =
            (_selectedIndex - 1 + _destinations.Count)
            % _destinations.Count;

        ShowSelection();
    }

    // Select a clicked star and update the navigation list.
    public void SelectStar(Star star)
    {
        int index = _destinations.FindIndex(s =>
            string.Equals(s.Name, star.Name,
                StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            _selectedIndex = index;
            _searchedStar = null;
        }
        else
        {
            _destinations.Insert(0, star);

            if (_destinations.Count > MaxDestinations)
                _destinations.RemoveAt(_destinations.Count - 1);

            _selectedIndex = 0;
            _searchedStar = null;
        }

        Console.WriteLine($"Clicked star: {star.Name}");
        ShowDestinationList();
        ShowSelection();
    }

    public bool SearchStar(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return false;

        string query = searchText.Trim();

        Star? found = _searchableStars.FirstOrDefault(s =>
            string.Equals(s.Name, query,
                StringComparison.OrdinalIgnoreCase));

        found ??= _searchableStars.FirstOrDefault(s =>
            s.Name.StartsWith(query,
                StringComparison.OrdinalIgnoreCase));

        found ??= _searchableStars.FirstOrDefault(s =>
            s.Name.Contains(query,
                StringComparison.OrdinalIgnoreCase));

        if (found == null)
        {
            Console.WriteLine($"No star found matching '{query}'.");
            return false;
        }

        _searchedStar = found;
        Console.WriteLine($"Search result: {found.Name}");
        ShowSelection();
        return true;
    }

    public void ShowSelection()
    {
        ShowSelection(0, 0, 0);
    }

    public void ShowSelection(
        double cameraX, double cameraY, double cameraZ)
    {
        Star? star = SelectedStar;
        if (star == null) return;

        double dx = star.X - cameraX;
        double dy = star.Y - cameraY;
        double dz = star.Z - cameraZ;

        double distance = Math.Sqrt(
            dx * dx + dy * dy + dz * dz);

        double bearing =
            (Math.Atan2(dx, dy) * 180 / Math.PI + 360) % 360;

        double path = Math.Atan2(
            dz, Math.Sqrt(dx * dx + dy * dy))
            * 180 / Math.PI;

        Console.WriteLine();
        Console.WriteLine($"Selected destination: {star.Name}");
        Console.WriteLine(
            $"Coordinates: X={star.X:F2}, Y={star.Y:F2}, Z={star.Z:F2}");
        Console.WriteLine(
            $"Distance: {distance:F2} pc " +
            $"({distance * LightYearsPerParsec:F2} light-years)");
        Console.WriteLine($"Bearing: {bearing:F2} degrees");
        Console.WriteLine($"Path/elevation: {path:F2} degrees");
    }
}
