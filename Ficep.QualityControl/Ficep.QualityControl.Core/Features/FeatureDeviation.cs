using Ficep.QualityControl.Core.Measurement;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// The deviation report for the scan points segmented to one feature: the feature's identity plus the
/// usual statistics / conformity verdict computed over just that feature's points.
/// </summary>
/// <param name="Feature">Identity of the feature.</param>
/// <param name="Report">Deviation statistics and verdict over the feature's points.</param>
public readonly record struct FeatureDeviation(FeatureDescriptor Feature, DeviationReport Report);
