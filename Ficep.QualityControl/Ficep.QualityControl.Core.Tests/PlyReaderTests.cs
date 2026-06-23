using System.Globalization;
using devDept.Geometry;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Tests;

public class PlyReaderTests
{
    private static List<SurfaceSample> SampleSet() => new()
    {
        new SurfaceSample(new Point3D(1.2345, 2.3456, 3.4567), new Vector3D(1.0, 0.0, 0.0)),
        new SurfaceSample(new Point3D(-4.5678, 5.6789, -6.7890), new Vector3D(0.0, 1.0, 0.0)),
        new SurfaceSample(new Point3D(7.8901, -8.9012, 9.0123), new Vector3D(0.0, 0.0, 1.0)),
    };

    [Fact]
    public void Read_RoundTripsEveryPointWrittenByPlyWriter()
    {
        var samples = SampleSet();
        var sw = new StringWriter();
        new PlyWriter().WriteTo(sw, samples);

        IReadOnlyList<SurfaceSample> read = new PlyReader().ReadFrom(new StringReader(sw.ToString()));

        Assert.Equal(samples.Count, read.Count);
        for (int i = 0; i < samples.Count; i++)
        {
            // Writer keeps 4 decimals on position, 6 on normals — assert within that precision.
            Assert.Equal(samples[i].Position.X, read[i].Position.X, 1e-3);
            Assert.Equal(samples[i].Position.Y, read[i].Position.Y, 1e-3);
            Assert.Equal(samples[i].Position.Z, read[i].Position.Z, 1e-3);
            Assert.Equal(samples[i].Normal.X, read[i].Normal.X, 1e-5);
            Assert.Equal(samples[i].Normal.Y, read[i].Normal.Y, 1e-5);
            Assert.Equal(samples[i].Normal.Z, read[i].Normal.Z, 1e-5);
        }
    }

    [Fact]
    public void Read_EmptyCloud_ReturnsNoPoints()
    {
        var sw = new StringWriter();
        new PlyWriter().WriteTo(sw, new List<SurfaceSample>());

        IReadOnlyList<SurfaceSample> read = new PlyReader().ReadFrom(new StringReader(sw.ToString()));

        Assert.Empty(read);
    }

    [Fact]
    public void Read_ToleratesPropertyOrderAndSkipsExtraColumns()
    {
        // Normals before positions, plus an extra 'intensity' property the reader must ignore.
        const string ply =
            "ply\n" +
            "format ascii 1.0\n" +
            "element vertex 2\n" +
            "property float nx\nproperty float ny\nproperty float nz\n" +
            "property float x\nproperty float y\nproperty float z\n" +
            "property uchar intensity\n" +
            "end_header\n" +
            "0 0 1 10 20 30 255\n" +
            "1 0 0 -1 -2 -3 128\n";

        IReadOnlyList<SurfaceSample> read = new PlyReader().ReadFrom(new StringReader(ply));

        Assert.Equal(2, read.Count);
        Assert.Equal(new Point3D(10, 20, 30), read[0].Position);
        Assert.Equal(new Vector3D(0, 0, 1), read[0].Normal);
        Assert.Equal(new Point3D(-1, -2, -3), read[1].Position);
        Assert.Equal(new Vector3D(1, 0, 0), read[1].Normal);
    }

    [Fact]
    public void Read_PositionsOnly_DefaultsNormalToAxisZ()
    {
        const string ply =
            "ply\nformat ascii 1.0\nelement vertex 1\n" +
            "property float x\nproperty float y\nproperty float z\nend_header\n" +
            "5 6 7\n";

        IReadOnlyList<SurfaceSample> read = new PlyReader().ReadFrom(new StringReader(ply));

        Assert.Single(read);
        Assert.Equal(new Point3D(5, 6, 7), read[0].Position);
        Assert.Equal(Vector3D.AxisZ, read[0].Normal);
    }

    [Theory]
    [InlineData("not a ply\n")]                                   // missing magic
    [InlineData("ply\nformat binary_little_endian 1.0\n")]        // unsupported format
    public void Read_RejectsInvalidInput(string content)
    {
        Assert.Throws<FormatException>(() => new PlyReader().ReadFrom(new StringReader(content)));
    }

    [Fact]
    public void Read_TruncatedBody_Throws()
    {
        const string ply =
            "ply\nformat ascii 1.0\nelement vertex 3\n" +
            "property float x\nproperty float y\nproperty float z\nend_header\n" +
            "1 2 3\n"; // declares 3, supplies 1

        Assert.Throws<FormatException>(() => new PlyReader().ReadFrom(new StringReader(ply)));
    }
}
