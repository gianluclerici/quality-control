using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Tests;

public class MeshSurfaceSamplerTests
{
    // A deterministic box: 10 x 20 x 30 mm.
    private const double W = 10.0;
    private const double D = 20.0;
    private const double H = 30.0;
    // Surface area = 2*(w*d + d*h + w*h).
    private static double BoxArea => 2.0 * (W * D + D * H + W * H);

    private static Mesh MakeBox() => Mesh.CreateBox(W, D, H);

    private static (Point3D min, Point3D max) Extents(Mesh mesh)
    {
        Point3D[] v = mesh.Vertices;
        double minX = v[0].X, minY = v[0].Y, minZ = v[0].Z;
        double maxX = v[0].X, maxY = v[0].Y, maxZ = v[0].Z;
        foreach (Point3D p in v)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
        }
        return (new Point3D(minX, minY, minZ), new Point3D(maxX, maxY, maxZ));
    }

    [Fact]
    public void Sample_Count_MatchesDensityTimesArea()
    {
        const double density = 2.0;
        Mesh mesh = MakeBox();

        // Verify CreateBox really produced the area we expect, so the count assertion is meaningful.
        double area = mesh.GetArea(out _);
        Assert.Equal(BoxArea, area, 3);

        var sampler = new MeshSurfaceSampler(density, seed: 42);
        IReadOnlyList<SurfaceSample> samples = sampler.Sample(mesh);

        int expected = (int)Math.Round(density * area, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, samples.Count);
    }

    [Fact]
    public void Sample_AllNormals_AreUnitLength()
    {
        var sampler = new MeshSurfaceSampler(1.0, seed: 7);
        IReadOnlyList<SurfaceSample> samples = sampler.Sample(MakeBox());

        Assert.NotEmpty(samples);
        foreach (SurfaceSample s in samples)
            Assert.Equal(1.0, s.Normal.Length, 6);
    }

    [Fact]
    public void Sample_AllPoints_LieOnBoxSurface()
    {
        Mesh mesh = MakeBox();
        (Point3D min, Point3D max) = Extents(mesh);
        const double tol = 1e-6;

        var sampler = new MeshSurfaceSampler(1.0, seed: 123);
        IReadOnlyList<SurfaceSample> samples = sampler.Sample(mesh);

        Assert.NotEmpty(samples);
        foreach (SurfaceSample s in samples)
        {
            Point3D p = s.Position;

            // Point must lie within the box bounds (allow tiny tolerance).
            Assert.InRange(p.X, min.X - tol, max.X + tol);
            Assert.InRange(p.Y, min.Y - tol, max.Y + tol);
            Assert.InRange(p.Z, min.Z - tol, max.Z + tol);

            // And at least one coordinate must be on a face plane.
            bool onX = Math.Abs(p.X - min.X) <= tol || Math.Abs(p.X - max.X) <= tol;
            bool onY = Math.Abs(p.Y - min.Y) <= tol || Math.Abs(p.Y - max.Y) <= tol;
            bool onZ = Math.Abs(p.Z - min.Z) <= tol || Math.Abs(p.Z - max.Z) <= tol;
            Assert.True(onX || onY || onZ,
                $"Point ({p.X},{p.Y},{p.Z}) is not on any face of the box [{min.X}..{max.X}, {min.Y}..{max.Y}, {min.Z}..{max.Z}].");
        }
    }

    [Fact]
    public void Sample_SameSeed_IsDeterministic()
    {
        Mesh mesh = MakeBox();
        var a = new MeshSurfaceSampler(1.5, seed: 99).Sample(mesh);
        var b = new MeshSurfaceSampler(1.5, seed: 99).Sample(mesh);

        Assert.Equal(a.Count, b.Count);
        Assert.NotEmpty(a);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Position.X, b[i].Position.X, 12);
            Assert.Equal(a[i].Position.Y, b[i].Position.Y, 12);
            Assert.Equal(a[i].Position.Z, b[i].Position.Z, 12);
            Assert.Equal(a[i].Normal.X, b[i].Normal.X, 12);
            Assert.Equal(a[i].Normal.Y, b[i].Normal.Y, 12);
            Assert.Equal(a[i].Normal.Z, b[i].Normal.Z, 12);
        }
    }

    [Fact]
    public void Sample_DifferentSeeds_ProduceDifferentSequences()
    {
        Mesh mesh = MakeBox();
        var a = new MeshSurfaceSampler(1.5, seed: 1).Sample(mesh);
        var b = new MeshSurfaceSampler(1.5, seed: 2).Sample(mesh);

        Assert.Equal(a.Count, b.Count);
        Assert.NotEmpty(a);

        bool anyDifferent = false;
        for (int i = 0; i < a.Count && !anyDifferent; i++)
        {
            if (a[i].Position.X != b[i].Position.X ||
                a[i].Position.Y != b[i].Position.Y ||
                a[i].Position.Z != b[i].Position.Z)
                anyDifferent = true;
        }
        Assert.True(anyDifferent, "Different seeds produced identical point sequences.");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsNonPositiveDensity(double density)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeshSurfaceSampler(density, seed: 0));
    }

    [Fact]
    public void Sample_EmptyMesh_ReturnsEmpty_NoThrow()
    {
        var sampler = new MeshSurfaceSampler(1.0, seed: 0);
        IReadOnlyList<SurfaceSample> samples = sampler.Sample(new Mesh());
        Assert.Empty(samples);
    }
}
