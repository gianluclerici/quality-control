using Ficep.RobServer.Data;
using Ficep.RobServer.MacroParser;

namespace Ficep.QualityControl.Core.Model;

/// <summary>
/// Nominal definition of one machining macro applied to a beam (e.g. SCAI01, INTC01),
/// together with its parameters. This is exactly the information the quality-control
/// step will receive as input (macro identity + nominal parameters) and try to verify
/// against the scanned point cloud.
/// </summary>
/// <remarks>
/// Parameter letters map 1:1 onto <c>Ficep.RobServer.MacroParser.CopeParam</c> /
/// <c>ICopeParams</c> (A..S plus the chamfer angles ALFA/BETA), and the positioning
/// strings <see cref="Side"/>, <see cref="Vx"/>, <see cref="Vy"/> map onto
/// <c>SIDE</c>/<c>VX</c>/<c>VY</c>. Only the letters actually used by a given macro are
/// relevant; the rest stay at 0. Storing them as a dictionary keeps the model open to
/// any macro without enumerating every letter.
/// </remarks>
public sealed record MacroSpec
{
    /// <summary>Macro class name as known to the macro assembly, e.g. "SCAI01", "INTC01".</summary>
    public required string MacroClassName { get; init; }

    /// <summary>Side / working plane: "A" or "B" (flanges), "C" (web). Default "C".</summary>
    public string Side { get; init; } = "C";

    /// <summary>Length-direction mirror: "I" (initial) or "F" (final). Default "I".</summary>
    public string Vx { get; init; } = "I";

    /// <summary>Vertical mirror: "A" (top) or "B" (bottom). Default "A".</summary>
    public string Vy { get; init; } = "A";

    /// <summary>
    /// Geometric parameters by letter (e.g. "A", "B", "C", "D", "E", "F", "G", "R",
    /// "ALFA", "BETA"). Values are millimetres or degrees depending on the macro.
    /// </summary>
    public Dictionary<string, double> Parameters { get; init; } = new();

    /// <summary>Convenience factory for setting parameters fluently.</summary>
    public MacroSpec With(string letter, double value)
    {
        Parameters[letter] = value;
        return this;
    }

    /// <summary>
    /// Builds the concrete <see cref="ICopeParams"/> instance consumed by the macro
    /// constructors, copying every known letter from <see cref="Parameters"/> and the
    /// positioning strings.
    /// </summary>
    public ICopeParams ToCopeParams()
    {
        var p = new CopeParam { SIDE = Side, VX = Vx, VY = Vy };
        foreach (var (letter, value) in Parameters)
        {
            switch (letter.ToUpperInvariant())
            {
                case "A": p.A = value; break;
                case "B": p.B = value; break;
                case "C": p.C = value; break;
                case "D": p.D = value; break;
                case "E": p.E = value; break;
                case "F": p.F = value; break;
                case "G": p.G = value; break;
                case "H": p.H = value; break;
                case "I": p.I = value; break;
                case "J": p.J = value; break;
                case "K": p.K = value; break;
                case "L": p.L = value; break;
                case "M": p.M = value; break;
                case "N": p.N = value; break;
                case "O": p.O = value; break;
                case "P": p.P = value; break;
                case "Q": p.Q = value; break;
                case "R": p.R = value; break;
                case "S": p.S = value; break;
                case "DA": p.DA = value; break;
                case "DB": p.DB = value; break;
                case "DC": p.DC = value; break;
                case "ALFA": p.ALFA = value; break;
                case "BETA": p.BETA = value; break;
                default:
                    throw new ArgumentException($"Unknown macro parameter letter '{letter}'.");
            }
        }
        return p;
    }
}
