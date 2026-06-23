using System.Globalization;
using devDept.Geometry;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Tests;

public class PlyWriterTests
{
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    private static List<SurfaceSample> SampleSet() => new()
    {
        new SurfaceSample(new Point3D(1.2345, 2.3456, 3.4567), new Vector3D(1.0, 0.0, 0.0)),
        new SurfaceSample(new Point3D(-4.5678, 5.6789, -6.7890), new Vector3D(0.0, 1.0, 0.0)),
        new SurfaceSample(new Point3D(7.8901, -8.9012, 9.0123), new Vector3D(0.0, 0.0, 1.0)),
    };

    [Fact]
    public void Write_ProducesValidHeaderAndRoundTripsPoints()
    {
        var samples = SampleSet();
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            new PlyWriter().Write(path, samples);
            string[] lines = File.ReadAllText(path)
                .Split('\n')
                .Where(l => l.Length > 0)
                .ToArray();

            int hi = 0;
            Assert.Equal("ply", lines[hi++]);
            Assert.Equal("format ascii 1.0", lines[hi++]);
            Assert.StartsWith("comment", lines[hi++]);
            Assert.Equal($"element vertex {samples.Count}", lines[hi++]);
            Assert.Equal("property float x", lines[hi++]);
            Assert.Equal("property float y", lines[hi++]);
            Assert.Equal("property float z", lines[hi++]);
            Assert.Equal("property float nx", lines[hi++]);
            Assert.Equal("property float ny", lines[hi++]);
            Assert.Equal("property float nz", lines[hi++]);
            Assert.Equal("end_header", lines[hi++]);

            string[] pointLines = lines.Skip(hi).ToArray();
            Assert.Equal(samples.Count, pointLines.Length);

            AssertPointRoundTrip(samples[0], pointLines[0]);
            AssertPointRoundTrip(samples[^1], pointLines[^1]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void AssertPointRoundTrip(SurfaceSample expected, string line)
    {
        string[] parts = line.Split(' ');
        Assert.Equal(6, parts.Length);
        double x = double.Parse(parts[0], Ci);
        double y = double.Parse(parts[1], Ci);
        double z = double.Parse(parts[2], Ci);
        double nx = double.Parse(parts[3], Ci);
        double ny = double.Parse(parts[4], Ci);
        double nz = double.Parse(parts[5], Ci);

        Assert.Equal(expected.Position.X, x, 1e-3);
        Assert.Equal(expected.Position.Y, y, 1e-3);
        Assert.Equal(expected.Position.Z, z, 1e-3);
        Assert.Equal(expected.Normal.X, nx, 1e-5);
        Assert.Equal(expected.Normal.Y, ny, 1e-5);
        Assert.Equal(expected.Normal.Z, nz, 1e-5);
    }

    [Fact]
    public void Write_EmptyList_ProducesHeaderWithZeroVerticesAndNoPoints()
    {
        var samples = new List<SurfaceSample>();
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            new PlyWriter().Write(path, samples);
            string[] lines = File.ReadAllText(path)
                .Split('\n')
                .Where(l => l.Length > 0)
                .ToArray();

            Assert.Contains("element vertex 0", lines);
            Assert.Equal("end_header", lines[^1]); // nothing after the header
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
