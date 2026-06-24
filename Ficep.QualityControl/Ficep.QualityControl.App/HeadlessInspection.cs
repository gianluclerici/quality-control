using System.Globalization;
using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Features;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.App;

/// <summary>
/// The headless (no-GUI) quality-control demo: it runs the same Core inspection pipeline the GUI drives
/// (<see cref="PieceInspector"/>) and prints the verdict table to the console, so the one executable
/// serves both as the WinForms viewport and as a command-line inspector.
/// <para>
/// <c>--demo</c> is self-contained (re-derives the demo piece and samples a clean cloud); otherwise it
/// reads a <c>*.macros.json</c> nominal and a <c>*.ply</c> scan from disk.
/// </para>
/// </summary>
internal static class HeadlessInspection
{
    public static int Run(string[] args)
    {
        try
        {
            Args ia = Parse(args);
            var factory = new BeamFactory();

            PieceSpec piece;
            IReadOnlyList<SurfaceSample> scan;

            if (ia.Demo)
            {
                piece = BuildDemoPiece();
                MachinedBeam scanSource = factory.BuildMachined(piece);
                Mesh mesh = BrepTessellator.ToMesh(scanSource.Solid, factory.BrepTolerance);
                scan = new MeshSurfaceSampler(ia.Density, ia.Seed).Sample(mesh);
                Console.WriteLine($"Demo piece: {piece.Macros.Count} macro(s); sampled {scan.Count:N0} clean points " +
                                  $"(density {ia.Density.ToString(CultureInfo.InvariantCulture)} pt/mm², seed {ia.Seed}).");
            }
            else
            {
                piece = PieceSpecSerializer.Read(ia.MacrosPath!);
                scan = new PlyReader().Read(ia.ScanPath!);
                Console.WriteLine($"Loaded {piece.Macros.Count} macro(s) from {Path.GetFileName(ia.MacrosPath)}; " +
                                  $"{scan.Count:N0} scan points from {Path.GetFileName(ia.ScanPath)}.");
            }

            MachinedBeam machined = factory.BuildMachined(piece);
            var options = new InspectionOptions
            {
                Align = ia.Align,
                HoleToleranceMm = ia.ToleranceMm,
                NotchTolerance = new NotchTolerance(ia.ToleranceMm, ia.ToleranceMm, ia.ToleranceMm),
            };

            PieceInspectionReport report = new PieceInspector()
                .Inspect(machined, piece.Macros, scan, factory.BrepTolerance, options);

            Console.WriteLine();
            foreach (string line in InspectionReportFormatter.Format(report))
                Console.WriteLine(line);

            return report.InTolerance ? 0 : 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Inspection failed: {ex.Message}");
            return 2;
        }
    }

    /// <summary>The same demo piece the Generator and the Step-5 tests use (IPE300 + SCAI01 + INTC01).</summary>
    private static PieceSpec BuildDemoPiece() => new()
    {
        Beam = BeamSpec.Ipe300(length: 1000.0),
        Macros = new[]
        {
            new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 80).With("B", 60).With("C", 40).With("D", 60).With("E", 40).With("R", 10),
            new MacroSpec { MacroClassName = "INTC01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 500).With("B", 150).With("C", 40),
        },
    };

    private static Args Parse(string[] args)
    {
        bool demo = false, align = true;
        string? macros = null, scan = null;
        double tol = 0.5, density = 0.5;
        int seed = 11;

        // args[0] is the headless switch (--headless / inspect); options follow.
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--demo": demo = true; break;
                case "--macros": macros = Value(args, ref i, "--macros"); break;
                case "--scan": scan = Value(args, ref i, "--scan"); break;
                case "--no-align": align = false; break;
                case "--tol": tol = Number(Value(args, ref i, "--tol"), "--tol"); break;
                case "--density": density = Number(Value(args, ref i, "--density"), "--density"); break;
                case "--seed": seed = Integer(Value(args, ref i, "--seed"), "--seed"); break;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        // No files given → behave as the self-contained demo.
        if (!demo && (string.IsNullOrWhiteSpace(macros) || string.IsNullOrWhiteSpace(scan)))
        {
            if (string.IsNullOrWhiteSpace(macros) && string.IsNullOrWhiteSpace(scan))
                demo = true;
            else
                throw new ArgumentException("Provide both --macros <file> and --scan <file>, or use --demo.");
        }

        return new Args(demo, macros, scan, align, tol, density, seed);
    }

    private static string Value(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"Option '{name}' requires a value.");
        return args[++i];
    }

    private static double Number(string s, string name) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
            ? v : throw new ArgumentException($"Option '{name}' expects a number, got '{s}'.");

    private static int Integer(string s, string name) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? v : throw new ArgumentException($"Option '{name}' expects an integer, got '{s}'.");

    private readonly record struct Args(
        bool Demo, string? MacrosPath, string? ScanPath, bool Align, double ToleranceMm, double Density, int Seed);
}
