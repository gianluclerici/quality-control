using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;

namespace Ficep.QualityControl.Core.Io;

/// <summary>
/// Loads the nominal solids from a STEP file (<c>.step</c>) as <see cref="Brep"/> entities, the
/// inverse of <see cref="BrepExporter"/>. Works headless (no viewport) by driving the Eyeshot
/// <see cref="ReadSTEP"/> translator and harvesting its result.
/// <para>
/// STEP solids do not always land directly in <see cref="ReadFileAsync.Entities"/>: a solid written
/// through a <see cref="BlockReference"/> (as <see cref="BrepExporter"/> does) lives inside a
/// referenced <see cref="Block"/>, and some files leave <c>Entities</c> empty altogether. This
/// importer resolves those references so the caller always gets the actual Breps.
/// </para>
/// <para>
/// The returned Breps are <b>not yet tessellated</b>: callers that need a display mesh or a volume
/// must regenerate them first (a Brep throws on <c>GetVolume</c> until it has been meshed). When
/// the Breps are added to a <c>Design</c> control, the control regenerates them on add.
/// </para>
/// </summary>
public sealed class BrepImporter
{
    /// <summary>
    /// Reads every <see cref="Brep"/> solid contained in the STEP file at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Source <c>.step</c> path.</param>
    /// <returns>The Breps found in the file, in document order (empty if the file holds no solids).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="IOException">The translator reported an error reading the file.</exception>
    public IReadOnlyList<Brep> Import(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Input path must be non-empty.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("STEP file not found.", path);

        var reader = new ReadSTEP(path);
        reader.DoWork();
        if (!reader.Result)
            throw new IOException($"STEP import failed for '{path}'.");

        var breps = new List<Brep>();
        HarvestBreps(reader.Entities, reader.Blocks, breps);

        // Fallback for files that keep every solid inside a Block and leave the top-level Entities
        // empty: take the Breps straight from the block definitions. We do NOT re-expand block
        // references here, so a solid is never counted twice (once via its reference, once directly).
        if (breps.Count == 0)
            foreach (Block block in reader.Blocks)
                foreach (Entity entity in block.Entities)
                    if (entity is Brep brep)
                        breps.Add(brep);

        return breps;
    }

    /// <summary>
    /// Flattens an entity list to its <see cref="Brep"/> solids, resolving every
    /// <see cref="BlockReference"/> against the file's block table and recursing into nested blocks.
    /// A solid reached through a reference is placed by that reference's transformation; we clone
    /// before transforming only when the placement is not the identity, so the common case (a single
    /// reference at the origin, as <see cref="BrepExporter"/> writes) copies no heavy geometry.
    /// </summary>
    private static void HarvestBreps(IEnumerable<Entity>? entities, BlockKeyedCollection blocks, List<Brep> sink)
    {
        if (entities is null)
            return;

        foreach (Entity entity in entities)
        {
            switch (entity)
            {
                case Brep brep:
                    sink.Add(brep);
                    break;

                case BlockReference reference:
                    int start = sink.Count;
                    HarvestBreps(reference.GetEntities(blocks, blocks), blocks, sink);

                    Transformation placement = reference.GetFullTransformation(blocks, blocks);
                    if (!placement.IsIdentity())
                        for (int i = start; i < sink.Count; i++)
                        {
                            var placed = (Brep)sink[i].Clone();
                            placed.TransformBy(placement);
                            sink[i] = placed;
                        }
                    break;
            }
        }
    }
}
