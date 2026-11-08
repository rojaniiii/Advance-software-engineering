
using StarChart;

var starCollection = new StarCollection();

starCollection.LoadStars("data/hygdata_v41.csv");

Console.WriteLine(
    $"Loaded {starCollection.GetStarCount()} stars."
);

using var window = new StarChartWindow(starCollection);

window.Run();
