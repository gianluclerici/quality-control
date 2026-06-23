namespace Ficep.QualityControl.Core.Model;

/// <summary>
/// Nominal definition of a steel beam: the cross-section profile plus its length.
/// </summary>
/// <remarks>
/// Field meanings mirror the RobServer profile model
/// (<c>Ficep.RobServer.Utility3D.EyeProfile</c> / <c>IProfile</c>):
/// <list type="bullet">
/// <item><see cref="ProfileCode"/> — single-letter profile family: I, U, L, Q, F (plate), R (tube).</item>
/// <item><see cref="SA"/> — section height (web length), in mm.</item>
/// <item><see cref="TA"/> — web thickness, in mm.</item>
/// <item><see cref="SB"/> — flange width, in mm.</item>
/// <item><see cref="TB"/> — flange thickness, in mm.</item>
/// <item><see cref="Radius"/> — root fillet radius, in mm.</item>
/// <item><see cref="Length"/> — beam length along its axis, in mm.</item>
/// </list>
/// The beam is extruded along X; the cross-section lies in the YZ plane.
/// </remarks>
public sealed record BeamSpec
{
    /// <summary>Profile family code (I, U, L, Q, F, R).</summary>
    public required string ProfileCode { get; init; }

    /// <summary>Section height / web length (mm).</summary>
    public required double SA { get; init; }

    /// <summary>Web thickness (mm).</summary>
    public required double TA { get; init; }

    /// <summary>Flange width (mm).</summary>
    public required double SB { get; init; }

    /// <summary>Flange thickness (mm).</summary>
    public required double TB { get; init; }

    /// <summary>Root fillet radius (mm). Use 0 for a sharp section.</summary>
    public double Radius { get; init; }

    /// <summary>Beam length along its axis (mm).</summary>
    public required double Length { get; init; }

    /// <summary>
    /// IPE 300 standard European I-beam (h=300, b=150, tw=7.1, tf=10.7, r=15),
    /// a representative section for testing. Length defaults to 1000 mm.
    /// </summary>
    public static BeamSpec Ipe300(double length = 1000.0) => new()
    {
        ProfileCode = "I",
        SA = 300.0,
        TA = 7.1,
        SB = 150.0,
        TB = 10.7,
        Radius = 15.0,
        Length = length,
    };
}
