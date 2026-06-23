namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Broad class of a machining feature, used to drive per-feature measurement and reporting. Derived
/// from the macro class name (e.g. INTC* → <see cref="Hole"/>, SCAI* → <see cref="Notch"/>). Scan
/// points that do not belong to any machined feature are <see cref="Base"/> (the raw beam body).
/// </summary>
public enum FeatureKind
{
    /// <summary>The unmachined beam body (no feature).</summary>
    Base,

    /// <summary>A drilled/contoured hole (INTC* macros): a cylindrical cut through web or flange.</summary>
    Hole,

    /// <summary>A notch / cope (SCAI* macros): a contoured cut, typically at a beam end.</summary>
    Notch,

    /// <summary>A recognised machined feature whose macro family is not specifically modelled.</summary>
    Other,
}

/// <summary>Maps a macro class name to its <see cref="FeatureKind"/>.</summary>
public static class FeatureKinds
{
    /// <summary>
    /// Classifies a macro by the prefix of its class name. Unknown families map to
    /// <see cref="FeatureKind.Other"/>; null/empty maps to <see cref="FeatureKind.Other"/> too.
    /// </summary>
    public static FeatureKind FromMacroClassName(string? macroClassName)
    {
        if (string.IsNullOrEmpty(macroClassName))
            return FeatureKind.Other;
        if (macroClassName.StartsWith("INTC", StringComparison.OrdinalIgnoreCase))
            return FeatureKind.Hole;
        if (macroClassName.StartsWith("SCAI", StringComparison.OrdinalIgnoreCase))
            return FeatureKind.Notch;
        return FeatureKind.Other;
    }
}
