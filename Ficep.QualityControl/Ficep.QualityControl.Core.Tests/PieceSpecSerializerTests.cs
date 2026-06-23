using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Model;

namespace Ficep.QualityControl.Core.Tests;

public class PieceSpecSerializerTests
{
    private static PieceSpec MakePiece() => new()
    {
        Beam = BeamSpec.Ipe300(1234.5),
        Macros = new[]
        {
            new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 80).With("B", 60).With("C", 40).With("D", 60).With("E", 40).With("R", 10),
            new MacroSpec { MacroClassName = "INTC01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 500).With("B", 150).With("C", 40),
        },
    };

    private static void AssertPieceEqual(PieceSpec expected, PieceSpec actual)
    {
        // Beam fields.
        Assert.Equal(expected.Beam.ProfileCode, actual.Beam.ProfileCode);
        Assert.Equal(expected.Beam.SA, actual.Beam.SA);
        Assert.Equal(expected.Beam.TA, actual.Beam.TA);
        Assert.Equal(expected.Beam.SB, actual.Beam.SB);
        Assert.Equal(expected.Beam.TB, actual.Beam.TB);
        Assert.Equal(expected.Beam.Radius, actual.Beam.Radius);
        Assert.Equal(expected.Beam.Length, actual.Beam.Length);

        // Macros (Dictionary has no structural equality, so compare explicitly).
        Assert.Equal(expected.Macros.Count, actual.Macros.Count);
        for (int i = 0; i < expected.Macros.Count; i++)
        {
            MacroSpec em = expected.Macros[i];
            MacroSpec am = actual.Macros[i];
            Assert.Equal(em.MacroClassName, am.MacroClassName);
            Assert.Equal(em.Side, am.Side);
            Assert.Equal(em.Vx, am.Vx);
            Assert.Equal(em.Vy, am.Vy);
            Assert.Equal(em.Parameters.Count, am.Parameters.Count);
            foreach (var (k, v) in em.Parameters)
            {
                Assert.True(am.Parameters.ContainsKey(k), $"missing parameter '{k}'");
                Assert.Equal(v, am.Parameters[k]);
            }
        }
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrips()
    {
        PieceSpec piece = MakePiece();
        string json = PieceSpecSerializer.Serialize(piece);
        PieceSpec back = PieceSpecSerializer.Deserialize(json);
        AssertPieceEqual(piece, back);
    }

    [Fact]
    public void Write_Read_RoundTrips()
    {
        PieceSpec piece = MakePiece();
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            PieceSpecSerializer.Write(path, piece);
            PieceSpec back = PieceSpecSerializer.Read(path);
            AssertPieceEqual(piece, back);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
