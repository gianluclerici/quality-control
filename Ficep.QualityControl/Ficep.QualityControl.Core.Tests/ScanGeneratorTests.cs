using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;

namespace Ficep.QualityControl.Core.Tests;

public class ScanGeneratorTests
{
    [Fact]
    public void Generate_WritesNonEmptyPlyAndStep_WithPositiveCounts()
    {
        var factory = new BeamFactory();
        Brep raw = factory.BuildRaw(BeamSpec.Ipe300(1000.0));

        var generator = new ScanGenerator(factory.BrepTolerance);
        // Low density keeps the cloud small on the real beam; fixed seed for determinism.
        var options = new GenerationOptions(DensityPerMm2: 0.05, SigmaMm: 0.1, Seed: 2024);

        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        string plyPath = Path.Combine(dir, "grezzo.ply");
        string stepPath = Path.Combine(dir, "grezzo.step");
        try
        {
            ScanResult result = generator.Generate(raw, options, seedOffset: 0, plyPath, stepPath);

            Assert.True(File.Exists(plyPath), "PLY file should exist.");
            Assert.True(File.Exists(stepPath), "STEP file should exist.");
            Assert.True(new FileInfo(plyPath).Length > 0, "PLY file should be non-empty.");
            Assert.True(new FileInfo(stepPath).Length > 0, "STEP file should be non-empty.");

            Assert.True(result.PointCount > 0, "Expected a positive point count.");
            Assert.True(result.TriangleCount > 0, "Expected a positive triangle count.");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
