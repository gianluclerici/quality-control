using System.Reflection;
using Ficep.MacroGra;
using Ficep.QualityControl.Core.Model;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;

namespace Ficep.QualityControl.Core.Generation;

/// <summary>
/// Instantiates machining macros by class name from the <c>Ficep.MacroGra</c> assembly.
/// Mirrors how RobServer itself creates macros (reflection over
/// <c>Ficep.MacroGra.&lt;name&gt;</c>), which keeps this generator open to any macro the
/// plant library defines without hard-coding each type.
/// </summary>
internal static class MacroBuilder
{
    // The assembly that contains SCAI01, INTC01, ... (compiled as "Ficep.MacroGra").
    private static readonly Assembly MacroAssembly = typeof(SCAI01).Assembly;

    /// <summary>
    /// Creates and returns an <see cref="EyeMacro"/> for the given spec, bound to the
    /// supplied workpiece and tolerances. The macro is not yet evaluated; call
    /// <see cref="EyeMacro.CreateMacro"/> to generate its cutting features.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The macro class is unknown, or does not expose the expected constructor.
    /// </exception>
    public static EyeMacro Create(MacroSpec spec, IWorkPiece workpiece, EyeParam eyeParam)
    {
        string fullName = "Ficep.MacroGra." + spec.MacroClassName;
        Type? type = MacroAssembly.GetType(fullName);
        if (type is null)
            throw new InvalidOperationException($"Macro class '{fullName}' not found in {MacroAssembly.GetName().Name}.");

        ICopeParams copeParams = spec.ToCopeParams();

        // Constructor signature shared by all cope macros:
        // (IWorkPiece, ICopeParams, string macroClassName, string macroName, EyeParam, uint lineNumber)
        object? instance = Activator.CreateInstance(
            type,
            workpiece,
            copeParams,
            spec.MacroClassName,
            spec.MacroClassName,
            eyeParam,
            (uint)0);

        if (instance is not EyeMacro macro)
            throw new InvalidOperationException($"Macro class '{fullName}' is not an EyeMacro.");

        return macro;
    }
}
