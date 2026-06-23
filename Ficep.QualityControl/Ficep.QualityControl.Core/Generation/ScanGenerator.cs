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

        // Derive deterministic per-stage seeds from the master seed (null ⇒ non-deterministic).
        int? samplerSeed = options.Seed.HasValue ? unchecked(options.Seed.Value + seedOffset) : null;
        int? noiseSeed = options.Seed.HasValue ? unchecked(options.Seed.Value + seedOffset + 7919) : null;

        var sampler = new MeshSurfaceSampler(options.DensityPerMm2, samplerSeed);
        IReadOnlyList<SurfaceSample> clean = sampler.Sample(mesh);

        var noise = new GaussianRangeNoise(options.SigmaMm, noiseSeed);
        IReadOnlyList<SurfaceSample> noisy = noise.Apply(clean);
        
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
}
