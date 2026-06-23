using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using Ficep.RobServer.Data;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;

namespace Ficep.RobServer.Utility3D
{
    /// <summary>
    /// Class describing the machining feature, with the vectors associated to the edges to obtain it
    /// </summary>
    public class EyeFeature
    {
        /// <summary>
        /// Internal, Extremity, undefined
        /// </summary>
        public enum FeatureType { INT, EST, UNDEFINED }
        /// <summary>
        /// Solid at which the feature is associated
        /// </summary>
        public Brep Solid { get; private set; }
        /// <summary>
        /// For each worked face of the final part due to this feature the cutting edges of that face are computed
        /// Each list of cutting edge correspond to a face
        /// </summary>
        public List<List<EyeCuttingEdge>> FaceList { get; set; }
        /// <summary>
        /// List of linear edges to obtain the feature
        /// </summary>
        public List<EyeLineEdge> EdgeList { get; private set; }
        /// <summary>
        /// Feature type
        /// </summary>
        public FeatureType Type { get; private set; }

        public EyeFeature(in Brep solid)
        {
            Solid = (Brep)solid.Clone();
            FaceList = new List<List<EyeCuttingEdge>>();
            EdgeList = new List<EyeLineEdge>();
        }

        /// <summary>
        /// Compute the edge list, marking them as int or est based on the tolerance passed in
        /// </summary>
        /// <param name="distanceTol">
        /// Tolerance
        /// </param>
        /// <param name="wp">
        /// Workpiece
        /// </param>
        /// <param name="lines">
        /// List of lines used to add the vectors to the design
        /// </param>
        public bool ComputeEdgeListOld(in double distanceTol, in EyeWorkPiece wp, out List<Line> lines)
        {
            lines = null;

            foreach (var edgeList in FaceList)
            {
                foreach (var edge in edgeList)
                {
                    if (edge.NormalLinesAdjacentFace is null)
                        continue;

                    if (!edge.ConvertToLineEdgeOld(Solid, distanceTol, wp, out List<EyeLineEdge> ficepEdges))
                        return false;

                    EdgeList.AddRange(ficepEdges);

                    if (!ComputeLines(out lines))
                        return false;
                }
            }

            bool isEst = EdgeList.FirstOrDefault(x => x.Type == EyeLineEdge.LineEdgeType.EST) != null, 
                 isInt = EdgeList.FirstOrDefault(x => x.Type == EyeLineEdge.LineEdgeType.INT) != null;

            if (isEst)
                Type = FeatureType.EST;
            else if (isInt)
                Type = FeatureType.INT;
            else
                Type = FeatureType.UNDEFINED;

            return true;
        }

        public bool ComputeEdgeList(in double distanceTol, in EyeWorkPiece wp, in List<EyeCuttingEdge> cuttingEdges, out List<Line> lines)
        {
            lines = null;

            if (cuttingEdges is null || cuttingEdges.Count == 0)
                return false;

            foreach (var ce in cuttingEdges)
            {
                // Find the EyeCuttingEdge corresponding to the curve 
                var edge = FaceList.SelectMany(x => x).FirstOrDefault(e => e.EdgeIndex == ce.EdgeIndex && e.WorkedFaceIndex == ce.WorkedFaceIndex);

                // Check if the edge curve in the brep solid will need to be reverted or if it is already with the
                // right sense
                bool reverseCurve = false;

                if (Solid.Edges[ce.EdgeIndex].Curve.StartPoint.DistanceTo(Solid.Edges[edge.EdgeIndex].Curve.EndPoint) < distanceTol)
                {
                    reverseCurve = true;
                }

                // Convert the EyeCuttingEdge to the EyeLineEdge
                if (!edge.ConvertToLineEdge(Solid, distanceTol, wp, reverseCurve, out List<EyeLineEdge> ficepEdges))
                    return false;

                EdgeList.AddRange(ficepEdges);

                if (!ComputeLines(out lines))
                    return false;
            }

            bool isEst = EdgeList.FirstOrDefault(x => x.Type == EyeLineEdge.LineEdgeType.EST) != null,
                 isInt = EdgeList.FirstOrDefault(x => x.Type == EyeLineEdge.LineEdgeType.INT) != null;

            if (isEst)
                Type = FeatureType.EST;
            else if (isInt)
                Type = FeatureType.INT;
            else
                Type = FeatureType.UNDEFINED;

            return true;
        }

        public bool ComputeEdgeList(in double distanceTol, in EyeWorkPiece wp, in List<ICurve> curves, out List<Line> lines)
        {
            lines = null;
            
            if (curves is null || curves.Count == 0)
                return false;
           
            foreach (var curve in curves)
            {
                // Find the EyeCuttingEdge corresponding to the curve 
                var edge = FaceList.SelectMany(x => x).FirstOrDefault(ce => ce.EdgeIndex == curve.EdgeIndex);

                // Check if the edge curve in the brep solid will need to be reverted or if it is already with the
                // right sense
                bool reverseCurve = false;

                if (curve.StartPoint.DistanceTo(Solid.Edges[edge.EdgeIndex].Curve.EndPoint) < distanceTol)
                {
                    reverseCurve = true;
                }

                // Convert the EyeCuttingEdge to the EyeLineEdge
                if (!edge.ConvertToLineEdge(Solid, distanceTol, wp, reverseCurve, out List<EyeLineEdge> ficepEdges))
                    return false;

                EdgeList.AddRange(ficepEdges);

                if (!ComputeLines(out lines))
                    return false;
            }

            bool isEst = EdgeList.FirstOrDefault(x => x.Type == EyeLineEdge.LineEdgeType.EST) != null,
                 isInt = EdgeList.FirstOrDefault(x => x.Type == EyeLineEdge.LineEdgeType.INT) != null;

            if (isEst)
                Type = FeatureType.EST;
            else if (isInt)
                Type = FeatureType.INT;
            else
                Type = FeatureType.UNDEFINED;

            return true;
        }

        /// <summary>
        /// Compute the list of lines
        /// </summary>
        /// <param name="lines">
        /// List of lines used to add the vectors to the design
        /// </param>
        /// <returns></returns>
        public bool ComputeLines(out List<Line> lines)
        {
            lines = new List<Line>();

            if (EdgeList.Count == 0)
                return false;

            foreach (var ficepEdge in EdgeList)
            {
                Line l1 = new Line(ficepEdge.StartPoint, ficepEdge.StartPoint + ficepEdge.V1Start * 2);
                Line l2 = new Line(ficepEdge.StartPoint, ficepEdge.StartPoint + ficepEdge.V2Start);
                Line l3 = new Line(ficepEdge.EndPoint, ficepEdge.EndPoint + ficepEdge.V1End * 2);
                Line l4 = new Line(ficepEdge.EndPoint, ficepEdge.EndPoint + ficepEdge.V2End);
                l1.Color = Color.Red;
                l3.Color = Color.Red;
                l2.Color = Color.Yellow;
                l4.Color = Color.Yellow;
                l1.ColorMethod = colorMethodType.byEntity;
                l2.ColorMethod = colorMethodType.byEntity;
                l3.ColorMethod = colorMethodType.byEntity;
                l4.ColorMethod = colorMethodType.byEntity;
                lines.Add(l1);
                lines.Add(l2);
                lines.Add(l3);
                lines.Add(l4);
            }

            return true;
        }

        /// <summary>
        /// Compute the faces belonging to this feature 
        /// </summary>
        /// <param name="features">
        /// List of features already computed
        /// </param>
        /// <param name="facesList">
        /// List of faces with the cuttingEdge computed. A face is a list of cutting edges
        /// </param>
        /// <param name="wp">
        /// Eyeworkpiece
        /// </param>
        /// <param name="distanceTol">
        /// </param>
        public void ComputeFeature(in List<List<EyeCuttingEdge>> facesList, in EyeWorkPiece wp, in double distanceTol, ref List<EyeFeature> features)
        {
            List<int> facesAlreadyUsed = null;
            // If the features are not empty compute the faces already in the features
            if (features.Count != 0)
                facesAlreadyUsed = features.SelectMany(x => x.FaceList.Select(y => y == null ? -1 : y.FirstOrDefault().WorkedFaceIndex))
                                   .Where(i => i != -1).ToList();

            // If the feature's face list is empty take the first face list not in the facesAlreadyUsed
            if (FaceList.Count == 0)
            {
                // Take the first face not contained in other features 
                List<EyeCuttingEdge> faceEdges;

                if (facesAlreadyUsed != null)
                { 
                    var firstFace = facesList.FirstOrDefault(x => x != null && x.Count > 0 && !facesAlreadyUsed.Contains(x.FirstOrDefault().WorkedFaceIndex));
                    faceEdges = firstFace?.Select(ce => (EyeCuttingEdge)ce.Clone())?.ToList();
                }
                else
                {
                    faceEdges = facesList.First().Select(ce => (EyeCuttingEdge)ce.Clone()).ToList();
                }

                if (faceEdges != null)
                    FaceList.Add(faceEdges);

                features.Add(this);

                // Update the list of faces already used 
                facesAlreadyUsed = features.SelectMany(x => x.FaceList.Select(y => y == null || y.Count == 0 ? -1 : y.FirstOrDefault().WorkedFaceIndex))
                                   .Where(i => i != -1).ToList();
            }

            // Take the last face's edges of the feature and search all the edges shared by other faces 
            // not in the facesAlreadyUsed list
            var commonEdges = CommonEdges(FaceList.LastOrDefault(), distanceTol, wp, Solid, facesList, facesAlreadyUsed);

            // Has a common edge not inside the faces already in the features list
            bool hasCommonEdge = !(commonEdges is null) && (commonEdges.Count != 0);

            if (!hasCommonEdge)
                return;
            else
            {
                foreach (EyeCuttingEdge commonEdge in commonEdges)
                {
                    // If the feature is closed the last face is added two times, so this is a check to avoid happening this
                    bool alreadyInsideFeature = FaceList.SelectMany(x => x).Select(ce => ce?.WorkedFaceIndex).Contains(commonEdge.WorkedFaceIndex);

                    if (alreadyInsideFeature)
                        continue;

                    // Add the list of EyeCuttingEdges having the worked face of the common edge considered
                    var facesWithCommonEdge = facesList.FirstOrDefault(f => f.FirstOrDefault()?.WorkedFaceIndex == commonEdge.WorkedFaceIndex);
                    if (facesWithCommonEdge is null || facesWithCommonEdge.Count == 0)
                        continue;

                    FaceList.Add(facesWithCommonEdge);
                    ComputeFeature(facesList, wp, distanceTol, ref features);
                }
            }

        }

        private List<EyeCuttingEdge> CommonEdges(List<EyeCuttingEdge> featureFaceEdgesList, double distanceTol, EyeWorkPiece eyeWp, Brep finalPart, List<List<EyeCuttingEdge>> facesList, List<int> facesAlreadyUsed)
        {
            if (featureFaceEdgesList is null)
                return null;

            List<int> temp = facesAlreadyUsed.Select(x => x).ToList();
            List<EyeCuttingEdge> commonEdges = new List<EyeCuttingEdge>();

            foreach (EyeCuttingEdge edge in featureFaceEdgesList)
            {
                if (
                    EyeUtils.HasCommonEdge(edge.EdgeIndex, distanceTol, eyeWp, finalPart, temp, facesList, out int faceIndex)
                    )
                {
                    commonEdges.Add(facesList.SelectMany(x => x).First(ce => ce.EdgeIndex == edge.EdgeIndex && ce.WorkedFaceIndex == faceIndex));
                    temp.Add(faceIndex);
                }
            }
            return commonEdges;
        }
    }
}
