using System.Text.Json;
using Ficep.QualityControl.Core.Model;

namespace Ficep.QualityControl.Core.Io;

/// <summary>
/// Reads and writes the <c>*.macros.json</c> sidecar that accompanies a generated point
/// cloud. It captures the full nominal <see cref="PieceSpec"/> (beam geometry + ordered
/// macro list with parameters) — i.e. exactly the input the quality-control measurement
/// step receives alongside the scan: nominal Brep + macro list + nominal parameters.
/// </summary>
public static class PieceSpecSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // Keep the key order/casing of the model so the file reads like the domain types.
        PropertyNamingPolicy = null,
    };

    /// <summary>Serializes <paramref name="piece"/> to an indented JSON string.</summary>
    public static string Serialize(PieceSpec piece)
    {
        ArgumentNullException.ThrowIfNull(piece);
        return JsonSerializer.Serialize(piece, Options);
    }

    /// <summary>Writes <paramref name="piece"/> as JSON to <paramref name="path"/>, overwriting it.</summary>
    public static void Write(string path, PieceSpec piece)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(piece);
        File.WriteAllText(path, Serialize(piece));
    }

    /// <summary>Parses a <see cref="PieceSpec"/> from a JSON string (inverse of <see cref="Serialize"/>).</summary>
    /// <exception cref="JsonException">The JSON is malformed or missing required members.</exception>
    public static PieceSpec Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<PieceSpec>(json, Options)
               ?? throw new JsonException("Deserialized PieceSpec was null.");
    }

    /// <summary>Reads a <see cref="PieceSpec"/> from a JSON file.</summary>
    public static PieceSpec Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Deserialize(File.ReadAllText(path));
    }
}
