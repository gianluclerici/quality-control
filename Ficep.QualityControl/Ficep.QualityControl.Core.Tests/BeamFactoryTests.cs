using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Tests;

public class BeamFactoryTests
{
    private static double MeshVolume(Brep brep, double tol)
    {
        Mesh mesh = BrepTessellator.ToMesh(brep, tol);
        return Math.Abs(mesh.GetVolume(out _));
    }

    private static (double dx, double dy, double dz) Extents(Brep brep)
    {
        Point3D[] v = brep.Vertices;
        Assert.NotNull(v);
        Assert.NotEmpty(v);
        double minX = v[0].X, minY = v[0].Y, minZ = v[0].Z;
        double maxX = v[0].X, maxY = v[0].Y, maxZ = v[0].Z;
        foreach (Point3D p in v)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
        }
        return (maxX - minX, maxY - minY, maxZ - minZ);
    }

    [Fact]
    public void BuildRaw_ProducesSolid_WithExpectedLength()
    {
        var factory = new BeamFactory();
        Brep raw = factory.BuildRaw(BeamSpec.Ipe300(1000.0));

        Assert.NotNull(raw);
        Assert.True(raw.Faces.Length > 0, "Raw Brep should have faces.");

        // The beam is extruded along X; its longest extent should be ~1000 mm.
        (double dx, _, _) = Extents(raw);
        Assert.InRange(dx, 1000.0 - 2.0, 1000.0 + 2.0);
    }

    [Fact]
    public void BuildMachined_RemovesMaterial_AndTracesEveryMacro()
    {
        var factory = new BeamFactory();
        BeamSpec beam = BeamSpec.Ipe300(1000.0);

        var piece = new PieceSpec
        {
            Beam = beam,
            Macros = new[]
            {
                new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
                    .With("A", 80).With("B", 60).With("C", 40).With("D", 60).With("E", 40).With("R", 10),
                new MacroSpec { MacroClassName = "INTC01", Side = "C", Vx = "I", Vy = "A" }
                    .With("A", 500).With("B", 150).With("C", 40),
            },
        };

        // Volume via the tessellated mesh (same code path the production pipeline uses);
        // Brep.GetVolume requires an internal face triangulation that a freshly built,
        // un-regenerated Brep does not have, so we measure on the mesh instead.
        Brep raw = factory.BuildRaw(beam);
        double rawVolume = MeshVolume(raw, factory.BrepTolerance);

        MachinedBeam machined = factory.BuildMachined(piece);
        Assert.NotNull(machined.Solid);
        double machinedVolume = MeshVolume(machined.Solid, factory.BrepTolerance);

        Assert.True(machinedVolume < rawVolume,
            $"Machined volume {machinedVolume} should be strictly less than raw {rawVolume}.");

        // One trace line per macro.
        Assert.Equal(piece.Macros.Count, machined.Trace.Count);
    }
}
