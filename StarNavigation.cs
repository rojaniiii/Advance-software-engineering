
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarChart;

public class StarNavigation
{
    private readonly List<Star> _destinations;
    private readonly StarCollection _collection;
    private int _selectedIndex;
    private Star? _searchedStar;

    private const double LightYearsPerParsec = 3.26156;

    public IReadOnlyList<Star> Destinations => _destinations;

    public Star? SelectedStar => _searchedStar ??
        (_destinations.Count > 0
            ? _destinations[_selectedIndex]
            : null);

    public StarNavigation(StarCollection collection)
    {
        _collection = collection;

        string[] preferredNames =
        {
            "Sirius", "Vega", "Altair", "Procyon",
            "Arcturus", "Rigel", "Betelgeuse",
            "Capella", "Deneb", "Polaris"
        };

        var namedStars = collection.GetNamedStars();

        _destinations = preferredNames
            .Select(name => namedStars.FirstOrDefault(star =>
                string.Equals(star.Name, name,
                    StringComparison.OrdinalIgnoreCase)))
            .Where(star => star != null)
            .Select(star => star!)
            .ToList();

        Console.WriteLine("\n=== STAR NAVIGATION ===");
        ShowDestinationList();
    }

    public void ShowDestinationList()
    {
        for (int i = 0; i < _destinations.Count; i++)
        {
            Console.WriteLine(
                $"{i + 1}. {_destinations[i].Name}");
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

    public void SelectStar(Star star)
    {
        int index = _destinations.FindIndex(s =>
            string.Equals(s.Name, star.Name,
                StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            _destinations.Insert(0, star);

            if (_destinations.Count > 10)
                _destinations.RemoveAt(
                    _destinations.Count - 1);

            index = 0;
        }

        _selectedIndex = index;
        _searchedStar = null;

        Console.WriteLine($"Clicked star: {star.Name}");
        ShowDestinationList();
        ShowSelection();
    }

    public bool SearchStar(string search)
    {
        return SearchByName(search);
    }

    public bool SearchByName(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return false;

        string query = search.Trim();

        Star? match = _collection.Stars.FirstOrDefault(s =>
            !string.IsNullOrWhiteSpace(s.Name) &&
            string.Equals(s.Name, query,
                StringComparison.OrdinalIgnoreCase));

        match ??= _collection.Stars.FirstOrDefault(s =>
            !string.IsNullOrWhiteSpace(s.Name) &&
            s.Name.StartsWith(query,
                StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return false;

        _searchedStar = match;
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
        if (star == null) return;

        double dx = star.X - cameraX;
        double dy = star.Y - cameraY;
        double dz = star.Z - cameraZ;

        double distance = Math.Sqrt(
            dx * dx + dy * dy + dz * dz);

       // Bearing: 0° points toward negative Z.
// 90° points toward positive X.
double bearing =
    (Math.Atan2(dx, -dz) * 180.0 / Math.PI + 360) % 360;

// Elevation: Y is the vertical axis.
double elevation = Math.Atan2(
    dy, Math.Sqrt(dx * dx + dz * dz))
    * 180.0 / Math.PI;
        Console.WriteLine(
            $"\nSelected destination: {star.Name}");

        Console.WriteLine(
            $"Coordinates: X={star.X:F2}, " +
            $"Y={star.Y:F2}, Z={star.Z:F2}");

        Console.WriteLine(
            $"Distance: {distance:F2} pc " +
            $"({distance * LightYearsPerParsec:F2} light-years)");

        Console.WriteLine(
            $"Bearing: {bearing:F2} degrees");

        Console.WriteLine(
            $"Path/elevation: {elevation:F2} degrees");
    }
}
