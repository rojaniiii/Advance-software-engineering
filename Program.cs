
using StarChart;

StarCollection collection = new StarCollection();

collection.LoadStars("data/hygdata_v41.csv");

Console.WriteLine(
    $"Total stars loaded: {collection.GetStarCount()}"
);

Star? sirius = collection.FindStar("Sirius");

if (sirius != null)
{
    Console.WriteLine(
        $"Sirius: X={sirius.X}, Y={sirius.Y}, Z={sirius.Z}"
    );
}
else
{
    Console.WriteLine("Sirius was not found.");
}
