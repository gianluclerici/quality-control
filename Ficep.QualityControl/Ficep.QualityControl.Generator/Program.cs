using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;

// TEMPORARY validation harness: build a raw and a machined IPE300 beam and print
// geometry sanity (bounding box, volume) to confirm the macro pipeline works.
// This will be replaced by the real CLI once sampling/export are in place.

var factory = new BeamFactory();

BeamSpec beam = BeamSpec.Ipe300(length: 1000.0);

Brep raw = factory.BuildRaw(beam);
raw.Regen(factory.BrepTolerance);
PrintBrep("RAW (grezzo)", raw);

var piece = new PieceSpec
{
    Beam = beam,
    Macros = new[]
    {
        // SCAI01: end notch on the web (plane C). Conservative geometry.
        new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
            .With("A", 80).With("B", 60).With("C", 40).With("D", 60).With("E", 40).With("R", 10),
        // INTC01: a circular hole through the web (plane C), diameter C=40 at (A,B).
        new MacroSpec { MacroClassName = "INTC01", Side = "C", Vx = "I", Vy = "A" }
            .With("A", 500).With("B", 150).With("C", 40),
    },
};

MachinedBeam machined = factory.BuildMachined(piece);
machined.Solid.Regen(factory.BrepTolerance);
foreach (var line in machined.Trace)
    Console.WriteLine("  macro> " + line);
PrintBrep("MACHINED (lavorato)", machined.Solid);

static void PrintBrep(string label, Brep brep)
{
    var min = brep.BoxMin;
    var max = brep.BoxMax;
    Console.WriteLine($"{label}: faces={brep.Faces.Length}, vertices={brep.Vertices.Length}");
    Console.WriteLine($"  bbox min=({min.X:F1},{min.Y:F1},{min.Z:F1}) max=({max.X:F1},{max.Y:F1},{max.Z:F1})");
}
