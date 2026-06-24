using System.Runtime.InteropServices;

namespace Ficep.QualityControl.App;

internal static class Program
{
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    /// <summary>
    /// Single entry point for both demos. With no arguments it launches the WinForms viewport (the GUI
    /// demo); with <c>--headless</c> / <c>inspect</c> it runs the headless inspection demo on the console
    /// and exits. The project is a WinExe, so to print in headless mode we attach to the launching
    /// terminal's console (<see cref="AttachConsole"/>), falling back to a fresh one.
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && IsHeadless(args[0]))
            return RunHeadless(args);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    private static bool IsHeadless(string arg) =>
        arg is "--headless" or "-h" or "inspect" || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase);

    private static int RunHeadless(string[] args)
    {
        bool attached = AttachConsole(AttachParentProcess);
        bool allocated = false;
        if (!attached)
            allocated = AllocConsole();

        // After (re)attaching, point Console.Out/Error at the real console handles.
        var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        var stderr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
        Console.SetOut(stdout);
        Console.SetError(stderr);

        try
        {
            if (args[0] is "--help" or "-h" && (args.Length == 1))
            {
                PrintUsage();
                return 0;
            }
            return HeadlessInspection.Run(args);
        }
        finally
        {
            stdout.Flush();
            stderr.Flush();
            if (allocated)
                FreeConsole();
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Ficep Quality Control");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  (no arguments)                       launch the GUI viewport (demo)");
        Console.WriteLine("  --headless [--demo]                  inspect the demo piece, print the verdict");
        Console.WriteLine("  --headless --macros <f> --scan <f>   inspect a real macros.json + scan PLY");
        Console.WriteLine();
        Console.WriteLine("Headless options: --no-align, --tol <mm>, --density <N>, --seed <K>");
    }
}
