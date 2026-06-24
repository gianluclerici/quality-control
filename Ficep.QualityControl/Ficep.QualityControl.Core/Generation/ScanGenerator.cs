using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Noise;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Generation;

/// <summary>Summary of one generated scan: counts useful for logging and sanity checks.</summary>
/// <param name="TriangleCount">Triangles in the tessellated mesh.</param>
/// <param name="PointCount">Points written to the PLY cloud.</param>
public readonly record struct ScanResult(int TriangleCount, int PointCount);

/// <summary>
/// Turns a nominal <see cref="Brep"/> into the Step-1 deliverables for one piece:
/// a noisy point cloud (PLY) that simulates a 3D-camera scan, plus the nominal solid
/// exported as STEP for the downstream fitting reference. Orchestrates the
/// tessellate → sample → add-noise → write pipeline; each stage is a swappable strategy.
/// </summary>
public sealed class ScanGenerator
{
    private readonly double _chordDeviation;
    private readonly IPointCloudWriter _cloudWriter;
    private readonly BrepExporter _brepExporter;

    /// <summary>
    /// Creates the generator.
    /// </summary>
    /// <param name="chordDeviation">
    /// Tessellation chord tolerance (mm), typically the Brep build tolerance
    /// (<c>BeamFactory.BrepTolerance</c>).
    /// </param>
    /// <param name="cloudWriter">Point-cloud writer; defaults to <see cref="PlyWriter"/>.</param>
    /// <param name="brepExporter">Nominal-solid exporter; defaults to <see cref="BrepExporter"/>.</param>
    public ScanGenerator(double chordDeviation, IPointCloudWriter? cloudWriter = null, BrepExporter? brepExporter = null)
    {
        _chordDeviation = chordDeviation;
        _cloudWriter = cloudWriter ?? new PlyWriter();
        _brepExporter = brepExporter ?? new BrepExporter();
    }

    /// <summary>
    /// Generates the PLY cloud and STEP reference for <paramref name="brep"/>.
    /// </summary>
    /// <param name="brep">The nominal solid (raw blank or machined part).</param>
    /// <param name="options">Density / noise / seed settings.</param>
    /// <param name="seedOffset">
    /// Added to <see cref="GenerationOptions.Seed"/> so distinct pieces (e.g. grezzo vs
    /// lavorato) get independent-but-reproducible clouds. Ignored when no seed is set.
    /// </param>
    /// <param name="plyPath">Destination PLY path.</param>
    /// <param name="stepPath">Destination STEP path.</param>
    public ScanResult Generate(Brep brep, GenerationOptions options, int seedOffset, string plyPath, string stepPath)
    {
        ArgumentNullException.ThrowIfNull(brep);
        ArgumentNullException.ThrowIfNull(plyPath);
        ArgumentNullException.ThrowIfNull(stepPath);

        Mesh mesh = BrepTessellator.ToMesh(brep, _chordDeviation);
        (IReadOnlyList<SurfaceSample> clean, IReadOnlyList<SurfaceSample> noisy) = SampleMesh(mesh, options, seedOffset);

        string? plyDir = Path.GetDirectoryName(plyPath),
               plyname = Path.GetFileNameWithoutExtension(plyPath),
               plyExt = Path.GetExtension(plyPath);
        
        if (plyDir != null && plyname != null && plyExt != null)
        {
            string plyCleanPath = Path.Combine(plyDir, plyname + "_clean" + plyExt);
            _cloudWriter.Write(plyCleanPath, clean);    
        }
        
        _cloudWriter.Write(plyPath, noisy);
        _brepExporter.Export(stepPath, brep);

        return new ScanResult(mesh.Triangles.Length, noisy.Count);
    }

    /// <summary>
    /// Generates a noisy scan cloud for <paramref name="brep"/> in memory (tessellate → sample → add
    /// noise), without writing any files. Same pipeline as <see cref="Generate"/>; used by the GUI to
    /// synthesise a scan from a Brep the user already imported, with interactively chosen
    /// density/sigma/seed in <paramref name="options"/>.
    /// </summary>
    /// <param name="brep">The nominal solid to scan.</param>
    /// <param name="options">Density / noise / seed settings.</param>
    /// <param name="seedOffset">Added to the master seed for reproducible-but-distinct clouds.</param>
    public IReadOnlyList<SurfaceSample> Sample(Brep brep, GenerationOptions options, int seedOffset = 0)
    {
        ArgumentNullException.ThrowIfNull(brep);
        Mesh mesh = BrepTessellator.ToMesh(brep, _chordDeviation);
        return SampleMesh(mesh, options, seedOffset).Noisy;
    }

    /// <summary>Samples <paramref name="mesh"/> and applies range noise; returns both the clean and noisy clouds.</summary>
    private static (IReadOnlyList<SurfaceSample> Clean, IReadOnlyList<SurfaceSample> Noisy) SampleMesh(
        Mesh mesh, GenerationOptions options, int seedOffset)
    {
        // Derive deterministic per-stage seeds from the master seed (null ⇒ non-deterministic).
        int? samplerSeed = options.Seed.HasValue ? unchecked(options.Seed.Value + seedOffset) : null;
        int? noiseSeed = options.Seed.HasValue ? unchecked(options.Seed.Value + seedOffset + 7919) : null;

        IReadOnlyList<SurfaceSample> clean = new MeshSurfaceSampler(options.DensityPerMm2, samplerSeed).Sample(mesh);
        IReadOnlyList<SurfaceSample> noisy = new GaussianRangeNoise(options.SigmaMm, noiseSeed).Apply(clean);
        return (clean, noisy);
    }
}
