
namespace StarChart;

public class Star
{
    public string Name { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    // Spectral classification, e.g. O, B, A, F, G, K, M
    public string SpectralType { get; set; }

    public Star(
        string name,
        double x,
        double y,
        double z,
        string spectralType = "Unknown")
    {
        Name = name;
        X = x;
        Y = y;
        Z = z;
        SpectralType = spectralType;
    }
}
