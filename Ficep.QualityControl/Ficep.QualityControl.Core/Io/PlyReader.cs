using System.Globalization;
using devDept.Geometry;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Io;

/// <summary>
/// Reads an <b>ASCII PLY</b> vertex cloud back into <see cref="SurfaceSample"/>s, the inverse of
/// <see cref="PlyWriter"/>. Parses the header to locate the <c>x y z</c> (required) and
/// <c>nx ny nz</c> (optional) properties by name, so the order they appear in does not matter and
/// extra per-vertex properties are tolerated and skipped. Numbers are parsed with invariant
/// culture, matching the writer.
/// <para>
/// Only the ASCII format is supported (<c>format ascii 1.0</c>); a binary PLY is rejected with a
/// clear message rather than silently mis-parsed.
/// </para>
/// </summary>
public sealed class PlyReader : IPointCloudReader
{
    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="FormatException">The file is not a valid ASCII PLY, or a body line is malformed.</exception>
    public IReadOnlyList<SurfaceSample> Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        using var reader = new StreamReader(path);
        return ReadFrom(reader);
    }

    /// <summary>Reads the PLY document from an arbitrary text reader (used by <see cref="Read"/> and tests).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
    /// <exception cref="FormatException">The stream is not a valid ASCII PLY, or a body line is malformed.</exception>
    public IReadOnlyList<SurfaceSample> ReadFrom(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var ci = CultureInfo.InvariantCulture;

        if (reader.ReadLine()?.Trim() != "ply")
            throw new FormatException("Not a PLY file: missing 'ply' magic on the first line.");

        // Parse the header: format, vertex count, and the ordered list of vertex property names.
        int vertexCount = -1;
        bool inVertexElement = false;
        var propertyNames = new List<string>();

        bool headerDone = false;
        string? line;
        while (!headerDone && (line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("comment", StringComparison.Ordinal))
                continue;

            string[] tok = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (tok[0])
            {
                case "format":
                    if (tok.Length < 2 || tok[1] != "ascii")
                        throw new FormatException($"Unsupported PLY format '{(tok.Length > 1 ? tok[1] : "")}': only 'ascii' is supported.");
                    break;
                case "element":
                    // element <name> <count> — we only consume the 'vertex' element.
                    inVertexElement = tok.Length >= 3 && tok[1] == "vertex";
                    if (inVertexElement)
                        vertexCount = int.Parse(tok[2], ci);
                    break;
                case "property":
                    // property <type> <name>  (or 'property list ...' for faces, which we ignore).
                    if (inVertexElement && tok.Length >= 3 && tok[1] != "list")
                        propertyNames.Add(tok[^1]);
                    break;
                case "end_header":
                    headerDone = true;
                    break;
            }
        }

        if (vertexCount < 0)
            throw new FormatException("PLY header has no 'element vertex' declaration.");

        int xi = propertyNames.IndexOf("x"), yi = propertyNames.IndexOf("y"), zi = propertyNames.IndexOf("z");
        if (xi < 0 || yi < 0 || zi < 0)
            throw new FormatException("PLY vertex element must declare x, y and z properties.");
        int nxi = propertyNames.IndexOf("nx"), nyi = propertyNames.IndexOf("ny"), nzi = propertyNames.IndexOf("nz");
        bool hasNormals = nxi >= 0 && nyi >= 0 && nzi >= 0;

        var samples = new List<SurfaceSample>(vertexCount);
        for (int i = 0; i < vertexCount; i++)
        {
            line = reader.ReadLine();
            if (line is null)
                throw new FormatException($"PLY declared {vertexCount} vertices but the body ended after {i}.");

            string[] f = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (f.Length < propertyNames.Count)
                throw new FormatException($"PLY vertex line {i} has {f.Length} values, expected {propertyNames.Count}.");

            var pos = new Point3D(
                double.Parse(f[xi], ci), double.Parse(f[yi], ci), double.Parse(f[zi], ci));
            var nrm = hasNormals
                ? new Vector3D(double.Parse(f[nxi], ci), double.Parse(f[nyi], ci), double.Parse(f[nzi], ci))
                : Vector3D.AxisZ;
            samples.Add(new SurfaceSample(pos, nrm));
        }
        return samples;
    }
}
