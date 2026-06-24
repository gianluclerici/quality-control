using System.Globalization;
using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Features;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Sampling;

// Ficep.QualityControl synthetic-scan generator + headless inspector.
//
//   generate --out <dir> [--density N] [--sigma S] [--seed K]
//   inspect  --demo [--density N] [--seed K] [--no-align] [--tol M]
//   inspect  --macros <file.macros.json> --scan <file.ply> [--no-align] [--tol M] [--on-surface-tol M]
//
// generate produces, for a demo IPE300 beam:
//   grezzo.ply  / grezzo.step           raw blank: scan cloud + nominal solid
//   lavorato.ply / lavorato.step        machined part (SCAI01 + INTC01): scan cloud + nominal solid
//   lavorato.macros.json                nominal macro list + parameters (QC step input)
// inspect runs the whole quality-control pipeline (align → segment → per-feature dimensional verdict)
// and prints the result; --demo is fully self-contained (no files needed).

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

            return args[0].ToLowerInvariant() switch
            {
                "generate" => RunGenerate(args),
                "inspect" => RunInspect(args),
                _ => UnknownCommand(args[0]),
            };
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            PrintUsage();
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Command failed: {ex.Message}");
            return 2;
        }
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        PrintUsage();
        return 1;
    }

    // --- generate -------------------------------------------------------------

    private static int RunGenerate(string[] args)
    {
        GenerateOptions opt = ParseGenerateOptions(args);
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

    // --- inspect --------------------------------------------------------------

    private static int RunInspect(string[] args)
    {
        InspectArgs ia = ParseInspectArgs(args);
        var factory = new BeamFactory();

        PieceSpec piece;
        IReadOnlyList<SurfaceSample> scan;

        if (ia.Demo)
        {
            // Fully self-contained demo: re-derive the nominal piece and sample a clean cloud from it.
            piece = BuildDemoPiece(BeamSpec.Ipe300(length: 1000.0));
            MachinedBeam machinedForScan = factory.BuildMachined(piece);
            Mesh mesh = BrepTessellator.ToMesh(machinedForScan.Solid, factory.BrepTolerance);
            scan = new MeshSurfaceSampler(ia.Density, ia.Seed).Sample(mesh);
            Console.WriteLine($"Demo piece: {piece.Macros.Count} macro(s); sampled {scan.Count:N0} clean points " +
                              $"(density {ia.Density} pt/mm², seed {ia.Seed}).");
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
            OnSurfaceToleranceMm = ia.OnSurfaceToleranceMm,
        };

        PieceInspectionReport report = new PieceInspector()
            .Inspect(machined, piece.Macros, scan, factory.BrepTolerance, options);

        Console.WriteLine();
        foreach (string line in InspectionReportFormatter.Format(report))
            Console.WriteLine(line);

        return report.InTolerance ? 0 : 3; // non-zero exit signals a non-conforming part
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

    // --- option parsing -------------------------------------------------------

    private static GenerateOptions ParseGenerateOptions(string[] args)
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

        return new GenerateOptions(outDir, density, sigma, seed);
    }

    private static InspectArgs ParseInspectArgs(string[] args)
    {
        bool demo = false;
        string? macros = null, scan = null;
        bool align = true;
        double tol = 0.5;
        double onSurfaceTol = FeatureSegmentation.DefaultOnSurfaceToleranceMm / 2.0; // 0.5 mm (clean-cloud band)
        double density = 0.5;
        int seed = 11;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--demo":
                    demo = true;
                    break;
                case "--macros":
                    macros = RequireValue(args, ref i, "--macros");
                    break;
                case "--scan":
                    scan = RequireValue(args, ref i, "--scan");
                    break;
                case "--no-align":
                    align = false;
                    break;
                case "--tol":
                    tol = ParseDouble(RequireValue(args, ref i, "--tol"), "--tol");
                    break;
                case "--on-surface-tol":
                    onSurfaceTol = ParseDouble(RequireValue(args, ref i, "--on-surface-tol"), "--on-surface-tol");
                    break;
                case "--density":
                    density = ParseDouble(RequireValue(args, ref i, "--density"), "--density");
                    break;
                case "--seed":
                    seed = ParseInt(RequireValue(args, ref i, "--seed"), "--seed");
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        if (!demo && (string.IsNullOrWhiteSpace(macros) || string.IsNullOrWhiteSpace(scan)))
            throw new ArgumentException("inspect needs either --demo, or both --macros <file> and --scan <file>.");

        return new InspectArgs(demo, macros, scan, align, tol, onSurfaceTol, density, seed);
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
        Console.WriteLine("  inspect  --demo [--density N] [--seed K] [--no-align] [--tol M]");
        Console.WriteLine("  inspect  --macros <file.macros.json> --scan <file.ply> [--no-align] [--tol M] [--on-surface-tol M]");
        Console.WriteLine();
        Console.WriteLine("generate options:");
        Console.WriteLine("  --out      output directory (required)");
        Console.WriteLine($"  --density  sampling density in points/mm² (default {GenerationOptions.DefaultDensityPerMm2})");
        Console.WriteLine($"  --sigma    range-noise std-dev in mm (default {GenerationOptions.Default.SigmaMm})");
        Console.WriteLine("  --seed     RNG seed for reproducible output (default: random)");
        Console.WriteLine();
        Console.WriteLine("inspect options:");
        Console.WriteLine("  --demo            inspect a freshly sampled clean cloud of the demo piece (no files)");
        Console.WriteLine("  --macros <file>   nominal macro list (*.macros.json)");
        Console.WriteLine("  --scan <file>     scanned point cloud (*.ply)");
        Console.WriteLine("  --no-align        skip ICP registration (use the scan as-is)");
        Console.WriteLine("  --tol M           per-parameter tolerance half-width in mm (default 0.5)");
        Console.WriteLine("  --on-surface-tol  segmentation on-surface band in mm (default 0.5)");
        Console.WriteLine("  --density / --seed   clean-cloud sampling for --demo (default 0.5 / 11)");
    }

    private readonly record struct GenerateOptions(string OutDir, double Density, double Sigma, int? Seed);

    private readonly record struct InspectArgs(
        bool Demo, string? MacrosPath, string? ScanPath, bool Align,
        double ToleranceMm, double OnSurfaceToleranceMm, double Density, int Seed);
}
