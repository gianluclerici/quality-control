using System.Linq;
using devDept.Eyeshot.Entities;
using Ficep.MacroGra;
using Ficep.QualityControl.Core.Model;
using Ficep.RobServer.Utility3D;

namespace Ficep.QualityControl.Core.Generation;

/// <summary>
/// Outcome of building a machined beam: the resulting solid plus a short diagnostic
/// trail of which macros were applied and how many cutting features each produced.
/// </summary>
public sealed record MachinedBeam
{
    /// <summary>The machined solid (raw blank minus all macro features).</summary>
    public required Brep Solid { get; init; }

    /// <summary>One line per macro describing what happened (applied / skipped / feature count).</summary>
    public required IReadOnlyList<string> Trace { get; init; }
}

/// <summary>
/// Builds nominal beam solids from a <see cref="BeamSpec"/>, reusing the RobServer
/// geometry pipeline: a raw extruded blank (<see cref="BuildRaw"/>) and a machined part
/// obtained by subtracting each macro's features from the blank
/// (<see cref="BuildMachined(PieceSpec)"/>).
/// </summary>
public sealed class BeamFactory
{
    private readonly EyeParam _eyeParam;
    private readonly double _brepTol;

    /// <summary>
    /// Creates the factory with the given tolerance/parameter set. When omitted, the
    /// RobServer defaults are used (<c>EyeParam()</c>: brep tol 0.001 mm, surplus 1 mm).
    /// </summary>
    public BeamFactory(EyeParam? eyeParam = null)
    {
        _eyeParam = eyeParam ?? new EyeParam();
        _brepTol = _eyeParam.Tol.Brep;
    }

    /// <summary>Brep construction tolerance in use (mm).</summary>
    public double BrepTolerance => _brepTol;

    /// <summary>
    /// Builds the raw blank ("grezzo"): the profile region extruded to the beam length,
    /// with no machining applied.
    /// </summary>
    /// <exception cref="InvalidOperationException">The solid could not be built.</exception>
    public Brep BuildRaw(BeamSpec spec)
    {
        Brep solid = BuildRawInternal(spec);
        return (Brep)solid.Clone();
    }

    /// <summary>
    /// Builds the machined part: starts from the raw blank and subtracts the features of
    /// every macro in <paramref name="piece"/> in order. Macros that produce no feature or
    /// whose boolean subtraction fails are recorded in the returned <see cref="MachinedBeam.Trace"/>
    /// but do not abort the build.
    /// </summary>
    public MachinedBeam BuildMachined(PieceSpec piece)
    {
        var workpiece = new EyeWorkPiece(
            piece.Beam.ProfileCode, piece.Beam.SA, piece.Beam.TA,
            piece.Beam.SB, piece.Beam.TB, piece.Beam.Radius, piece.Beam.Length);
        workpiece.CreateSolidRawPart(_brepTol);
        if (workpiece.Solid is null)
            throw new InvalidOperationException("Failed to build the raw solid for the beam.");

        Brep finalPart = (Brep)workpiece.Solid.Clone();
        var trace = new List<string>();

        foreach (MacroSpec macroSpec in piece.Macros)
        {
            EyeMacro macro = MacroBuilder.Create(macroSpec, workpiece, _eyeParam);

            if (!macro.CreateMacro())
            {
                trace.Add($"{macroSpec.MacroClassName}: skipped (CreateMacro returned false).");
                continue;
            }

            int features = macro.Features.Count;
            int subtracted = 0;
            foreach (EyeFeature feature in macro.Features)
            {
                Brep? diff = Brep.Difference(finalPart, feature.Solid)?.FirstOrDefault();
                if (diff is not null)
                {
                    finalPart = diff;
                    subtracted++;
                }
            }

            trace.Add($"{macroSpec.MacroClassName}: {subtracted}/{features} feature(s) subtracted.");
        }

        return new MachinedBeam { Solid = finalPart, Trace = trace };
    }

    private Brep BuildRawInternal(BeamSpec spec)
    {
        var workpiece = new EyeWorkPiece(
            spec.ProfileCode, spec.SA, spec.TA, spec.SB, spec.TB, spec.Radius, spec.Length);
        workpiece.CreateSolidRawPart(_brepTol);
        if (workpiece.Solid is null)
            throw new InvalidOperationException("Failed to build the raw solid for the beam.");
        return workpiece.Solid;
    }
}
