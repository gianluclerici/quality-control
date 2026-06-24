using System.Globalization;
using System.Linq;
using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Renders a <see cref="PieceInspectionReport"/> as plain text lines: an alignment summary, a row per
/// measured parameter (nominal / measured / deviation / tolerance / PASS-FAIL) and the overall verdict.
/// Shared by the headless demo and any other front-end so the report reads identically everywhere.
/// </summary>
public static class InspectionReportFormatter
{
    /// <summary>Formats <paramref name="report"/> as a sequence of text lines (no trailing newlines).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is null.</exception>
    public static IReadOnlyList<string> Format(PieceInspectionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var lines = new List<string>();
        CultureInfo c = CultureInfo.InvariantCulture;

        if (report.Aligned)
        {
            RegistrationResult a = report.Alignment;
            lines.Add(string.Format(c,
                "Alignment (ICP): RMS {0:F3} mm, {1} iter, converged={2}.",
                a.RmsErrorMm, a.Iterations, a.Converged));
        }
        else
        {
            lines.Add("Alignment: skipped (scan used as-is).");
        }

        if (report.Features.Count == 0)
        {
            lines.Add("No measurable features found.");
            return lines;
        }

        lines.Add("");
        lines.Add(string.Format(c, "{0,-22} {1,-10} {2,9} {3,9} {4,9} {5,7}  {6}",
            "Feature", "Parameter", "Nominal", "Measured", "Dev", "Tol", "Verdict"));
        lines.Add(new string('-', 86));

        foreach (FeatureInspectionReport f in report.Features)
        {
            string label = f.Feature.Label;
            bool first = true;
            foreach (FeatureParameter p in f.Parameters)
            {
                lines.Add(string.Format(c, "{0,-22} {1,-10} {2,9:F3} {3,9:F3} {4,9:+0.000;-0.000} {5,7:F3}  {6}",
                    first ? label : string.Empty,
                    p.Name, p.NominalMm, p.MeasuredMm, p.DeviationMm, p.ToleranceMm,
                    p.InTolerance ? "PASS" : "FAIL"));
                first = false;
            }
            lines.Add(string.Format(c, "{0,-22} ({1} pts) -> {2}",
                string.Empty, f.PointCount, f.InTolerance ? "PASS" : "FAIL"));
        }

        lines.Add(new string('-', 86));
        lines.Add(string.Format(c, "Overall: {0}  ({1}/{2} features in tolerance)",
            report.InTolerance ? "CONFORME" : "NON CONFORME",
            report.Features.Count(f => f.InTolerance), report.Features.Count));
        return lines;
    }

    /// <summary>Formats <paramref name="report"/> as a single multi-line string.</summary>
    public static string FormatText(PieceInspectionReport report) =>
        string.Join(Environment.NewLine, Format(report));
}
