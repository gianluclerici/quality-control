using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;

namespace Ficep.QualityControl.Core.Io;

/// <summary>
/// Exports a nominal <see cref="Brep"/> solid to a STEP file (<c>.step</c>). STEP is the
/// agreed nominal-reference format: the quality-control step loads it as the CAD model the
/// scan is fitted against. Works headless (no viewport) by assembling a minimal
/// <see cref="DesignDocument"/> and driving the Eyeshot <see cref="WriteSTEP"/> translator,
/// mirroring the RobServer export path.
/// </summary>
public sealed class BrepExporter
{
    /// <summary>
    /// Writes <paramref name="brep"/> to <paramref name="path"/> as STEP, overwriting any existing file.
    /// </summary>
    /// <param name="path">Destination <c>.step</c> path.</param>
    /// <param name="brep">The solid to export.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="IOException">The translator reported a failure writing the file.</exception>
    public void Export(string path, Brep brep)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(brep);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Output path must be non-empty.", nameof(path));

        const string blockName = "part";
        var block = new Block(blockName);
        block.Entities.Add((Brep)brep.Clone());

        var doc = new DesignDocument();
        doc.Blocks.Add(block);
        doc.Entities.Add(new BlockReference(blockName));

        var writer = new WriteSTEP(doc, path);
        writer.DoWork();

        if (!File.Exists(path))
            throw new IOException($"STEP export did not produce a file at '{path}'.");
    }
}
