
namespace StarChart;

public class Star
{
    public string Name { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Star(string name, double x, double y, double z)
    {
        Name = name;
        X = x;
        Y = y;
        Z = z;
    }
}
