using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Model;

namespace Ficep.QualityControl.Core.Tests;

public class BrepImporterTests
{
    /// <summary>
    /// BrepExporter writes the solid through a BlockReference, so the importer only works if it
    /// resolves that reference back to the Brep inside the referenced Block (the bug this guards:
    /// a blocked solid used to come back as "no Breps in file").
    /// </summary>
    [Fact]
    public void Import_RoundTripsBlockedSolidWrittenByExporter()
    {
        Brep solid = new BeamFactory().BuildRaw(BeamSpec.Ipe300(1000.0));

        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        string stepPath = Path.Combine(dir, "part.step");
        try
        {
            new BrepExporter().Export(stepPath, solid);

            IReadOnlyList<Brep> imported = new BrepImporter().Import(stepPath);

            Assert.Single(imported);
            Assert.NotNull(imported[0]);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Import_NullPath_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new BrepImporter().Import(null!));

    [Fact]
    public void Import_EmptyPath_Throws() =>
        Assert.Throws<ArgumentException>(() => new BrepImporter().Import("   "));

    [Fact]
    public void Import_MissingFile_Throws() =>
        Assert.Throws<FileNotFoundException>(() => new BrepImporter().Import(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".step")));
}
