using System.Globalization;
using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Model;

// Ficep.QualityControl synthetic-scan generator.
//
//   generate --out <dir> [--density N] [--sigma S] [--seed K]
//
// Produces, for a demo IPE300 beam:
//   grezzo.ply  / grezzo.step           raw blank: scan cloud + nominal solid
//   lavorato.ply / lavorato.step        machined part (SCAI01 + INTC01): scan cloud + nominal solid
//   lavorato.macros.json                nominal macro list + parameters (QC step input)

return CliRunner.Run(args);

internal static class CliRunner
{
    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                PrintUsage();
                return args.Length == 0 ? 1 : 0;
            }

            if (!string.Equals(args[0], "generate", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Unknown command '{args[0]}'.");
                PrintUsage();
                return 1;
            }

            Options opt = ParseOptions(args);
            Directory.CreateDirectory(opt.OutDir);

            var factory = new BeamFactory();
            var generator = new ScanGenerator(factory.BrepTolerance);
            var genOptions = new GenerationOptions(opt.Density, opt.Sigma, opt.Seed);

            // --- Raw blank ("grezzo") -------------------------------------------------
            BeamSpec beam = BeamSpec.Ipe300(length: 1000.0);
            Brep raw = factory.BuildRaw(beam);
            ScanResult rawResult = generator.Generate(
                raw, genOptions, seedOffset: 0,
                plyPath: Path.Combine(opt.OutDir, "grezzo.ply"),
                stepPath: Path.Combine(opt.OutDir, "grezzo.step"));
            Report("grezzo", rawResult);

            // --- Machined part ("lavorato") ------------------------------------------
            PieceSpec piece = BuildDemoPiece(beam);
            MachinedBeam machined = factory.BuildMachined(piece);
            foreach (string line in machined.Trace)
                Console.WriteLine($"  macro> {line}");

            ScanResult machinedResult = generator.Generate(
                machined.Solid, genOptions, seedOffset: 1000,
                plyPath: Path.Combine(opt.OutDir, "lavorato.ply"),
                stepPath: Path.Combine(opt.OutDir, "lavorato.step"));
            Report("lavorato", machinedResult);

            PieceSpecSerializer.Write(Path.Combine(opt.OutDir, "lavorato.macros.json"), piece);
            Console.WriteLine($"  wrote lavorato.macros.json ({piece.Macros.Count} macro(s))");

            Console.WriteLine($"Done. Output in: {Path.GetFullPath(opt.OutDir)}");
            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            PrintUsage();
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Generation failed: {ex.Message}");
            return 2;
        }
    }

    /// <summary>The demo machined piece: an IPE300 with an end web-notch (SCAI01) and a web hole (INTC01).</summary>
    private static PieceSpec BuildDemoPiece(BeamSpec beam) => new()
    {
        Beam = beam,
        Macros = new[]
        {
            new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 80).With("B", 60).With("C", 40).With("D", 60).With("E", 40).With("R", 10),
            new MacroSpec { MacroClassName = "INTC01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 500).With("B", 150).With("C", 40),
        },
    };

    private static void Report(string label, ScanResult r) =>
        Console.WriteLine($"  {label}: {r.PointCount} points from {r.TriangleCount} triangles → {label}.ply / {label}.step");

    private static Options ParseOptions(string[] args)
    {
        string? outDir = null;
        double density = GenerationOptions.DefaultDensityPerMm2;
        double sigma = GenerationOptions.Default.SigmaMm;
        int? seed = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out":
                    outDir = RequireValue(args, ref i, "--out");
                    break;
                case "--density":
                    density = ParseDouble(RequireValue(args, ref i, "--density"), "--density");
                    break;
                case "--sigma":
                    sigma = ParseDouble(RequireValue(args, ref i, "--sigma"), "--sigma");
                    break;
                case "--seed":
                    seed = ParseInt(RequireValue(args, ref i, "--seed"), "--seed");
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(outDir))
            throw new ArgumentException("--out <dir> is required.");

        return new Options(outDir, density, sigma, seed);
    }

    private static string RequireValue(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"Option '{name}' requires a value.");
        return args[++i];
    }

    private static double ParseDouble(string s, string name) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
            ? v
            : throw new ArgumentException($"Option '{name}' expects a number, got '{s}'.");

    private static int ParseInt(string s, string name) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? v
            : throw new ArgumentException($"Option '{name}' expects an integer, got '{s}'.");

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  generate --out <dir> [--density N] [--sigma S] [--seed K]");
        Console.WriteLine();
        Console.WriteLine("  --out      output directory (required)");
        Console.WriteLine($"  --density  sampling density in points/mm² (default {GenerationOptions.DefaultDensityPerMm2})");
        Console.WriteLine($"  --sigma    range-noise std-dev in mm (default {GenerationOptions.Default.SigmaMm})");
        Console.WriteLine("  --seed     RNG seed for reproducible output (default: random)");
    }

    private readonly record struct Options(string OutDir, double Density, double Sigma, int? Seed);
}
