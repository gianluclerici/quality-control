using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ficep.RobServer.Utility3D
{
    /// <summary>
    /// EyeCuttingEdge is the edge of the face for which the cutting vectors are computed.
    /// </summary>
    public class EyeCuttingEdge: ICloneable
    {
        public enum ChamferType
        {
            None,
            Internal,
            External
        }

        /// <summary>
        /// Index of the edge in the list of edges of the workpiece
        /// </summary>
        public int EdgeIndex { get; set; }
        /// <summary>
        /// List of vectors normal to the worked face
        /// </summary>
        public List<Line> NormalLinesWorkedFace { get; set; }
        /// <summary>
        /// List of vectors normal to the face adjacent at the worked face
        /// </summary>
        public List<Line> NormalLinesAdjacentFace { get; set; }
        /// <summary>
        /// Index of the shared face in the list of faces of the workpiece
        /// </summary>
        public int WorkedFaceIndex { get; set; }
        /// <summary>
        /// Index of the face adjacent at the worked face in the list of faces of the workpiece
        /// </summary>
        public int AdjacentFaceIndex { get; set; }

        public ChamferType Type { get; set; }

        public EyeCuttingEdge(int edgeIndex, List<Line> normalLinesWorkedFace, List<Line> normalLinesAdjacentFace, int workedFaceIndex, int adjacentFaceIndex, ChamferType type = ChamferType.None)
        {
            EdgeIndex = edgeIndex;
            NormalLinesWorkedFace = normalLinesWorkedFace;
            NormalLinesAdjacentFace = normalLinesAdjacentFace;
            WorkedFaceIndex = workedFaceIndex;
            AdjacentFaceIndex = adjacentFaceIndex;
            Type = type;
        }

        public EyeCuttingEdge(EyeCuttingEdge ce)
        {
            EdgeIndex = ce.EdgeIndex;
            NormalLinesAdjacentFace = ce.NormalLinesAdjacentFace?.Select(x => (Line)x.Clone())?.ToList();
            NormalLinesWorkedFace = ce.NormalLinesWorkedFace?.Select(x => (Line)x.Clone())?.ToList();
            WorkedFaceIndex = ce.WorkedFaceIndex;
            AdjacentFaceIndex= ce.AdjacentFaceIndex;
        }

        /// <summary>
        /// Convert the CuttingEdge in LineEdge
        /// </summary>
        /// <param name="finalPart">Brep solid</param>
        /// <param name="distanceTol">
        /// Tolerance used to compare if the point 3d is inside the solid or at the extremity
        /// </param>
        /// <param name="wp">
        /// Workpiece paramenters
        /// </param>
        /// <param name="ficepEdges">
        /// the computed line edges
        /// </param>
        public bool ConvertToLineEdgeOld(in Brep finalPart, in double distanceTol, in EyeWorkPiece wp, out List<EyeLineEdge> ficepEdges)
        {
            Brep.Edge edge = finalPart.Edges[EdgeIndex];
            
            
            if (
                edge is null ||
                NormalLinesAdjacentFace is null ||
                NormalLinesAdjacentFace.Count != NormalLinesWorkedFace.Count
               )
            {
                ficepEdges = null;
                return false;
            }

            ficepEdges = new List<EyeLineEdge>();

            for(int i = 0; i < NormalLinesAdjacentFace.Count - 1; i++)
            {
                Point3D start = (Point3D)NormalLinesWorkedFace[i].StartPoint.Clone();
                Point3D end = (Point3D)NormalLinesWorkedFace[i + 1].StartPoint.Clone();
                Vector3D normal1 = (Vector3D)NormalLinesWorkedFace[i].Direction.Clone();
                Vector3D normal2 = (Vector3D)NormalLinesAdjacentFace[i].Direction.Clone();
                normal1.Normalize();
                normal2.Normalize();
                Vector3D v1Start = EyeUtils.ComputeCuttingLine(NormalLinesWorkedFace[i], NormalLinesAdjacentFace[i], finalPart).Direction;
                Vector3D v2Start = EyeUtils.ComputeCuttingLine(NormalLinesAdjacentFace[i], NormalLinesWorkedFace[i], finalPart).Direction;
                Vector3D v1End = EyeUtils.ComputeCuttingLine(NormalLinesWorkedFace[i + 1], NormalLinesAdjacentFace[i + 1], finalPart).Direction;
                Vector3D v2End = EyeUtils.ComputeCuttingLine(NormalLinesAdjacentFace[i + 1], NormalLinesWorkedFace[i + 1], finalPart).Direction;
                v1Start.Normalize();
                v2Start.Normalize();
                v1End.Normalize();
                v2End.Normalize();

                ficepEdges.Add(new EyeLineEdge(start, end, normal1, normal2, v1Start, v2Start, v1End, v2End, distanceTol, finalPart));
            }
            return true;
        }

        public bool ConvertToLineEdge(in Brep finalPart, in double distanceTol, in EyeWorkPiece wp, in bool reverseSense, out List<EyeLineEdge> ficepEdges)
        {
            Brep.Edge edge = finalPart.Edges[EdgeIndex];
            
            if(reverseSense)
            {
                NormalLinesAdjacentFace.Reverse();
                NormalLinesWorkedFace.Reverse();
            }

            if (
                edge is null ||
                NormalLinesAdjacentFace is null ||
                NormalLinesAdjacentFace.Count != NormalLinesWorkedFace.Count
               )
            {
                ficepEdges = null;
                return false;
            }

            ficepEdges = new List<EyeLineEdge>();

            for (int i = 0; i < NormalLinesAdjacentFace.Count - 1; i++)
            {
                Point3D start = (Point3D)NormalLinesWorkedFace[i].StartPoint.Clone();
                Point3D end = (Point3D)NormalLinesWorkedFace[i + 1].StartPoint.Clone();
                Vector3D normal1 = (Vector3D)NormalLinesWorkedFace[i].Direction.Clone();
                Vector3D normal2 = (Vector3D)NormalLinesAdjacentFace[i].Direction.Clone();
                normal1.Normalize();
                normal2.Normalize();
                Vector3D v1Start = EyeUtils.ComputeCuttingLine(NormalLinesWorkedFace[i], NormalLinesAdjacentFace[i], finalPart).Direction;
                Vector3D v2Start = EyeUtils.ComputeCuttingLine(NormalLinesAdjacentFace[i], NormalLinesWorkedFace[i], finalPart).Direction;
                Vector3D v1End = EyeUtils.ComputeCuttingLine(NormalLinesWorkedFace[i + 1], NormalLinesAdjacentFace[i + 1], finalPart).Direction;
                Vector3D v2End = EyeUtils.ComputeCuttingLine(NormalLinesAdjacentFace[i + 1], NormalLinesWorkedFace[i + 1], finalPart).Direction;
                v1Start.Normalize();
                v2Start.Normalize();
                v1End.Normalize();
                v2End.Normalize();

                ficepEdges.Add(new EyeLineEdge(start, end, normal1, normal2, v1Start, v2Start, v1End, v2End, distanceTol, finalPart, Type));
            }
            return true;
        }
        public object Clone()
        {
            return new EyeCuttingEdge(this);
        }
    }
}
