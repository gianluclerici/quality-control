using devDept.Eyeshot.Entities;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// A machined feature paired with the tool solid that produced it: the <see cref="Cutter"/> Brep is
/// the volume subtracted from the blank to create the feature, so the feature's surface on the final
/// part lies on this cutter's boundary. That is exactly what makes the cutter a robust segmentation
/// primitive — a scan point sitting on the cutter boundary belongs to this feature
/// (see <see cref="FeatureSegmentation"/>).
/// </summary>
public sealed record FeatureCutter
{
    /// <summary>Identity/provenance of the feature.</summary>
    public required FeatureDescriptor Descriptor { get; init; }

    /// <summary>The tool solid subtracted to produce the feature.</summary>
    public required Brep Cutter { get; init; }
}
