using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Milling;
using devDept.Geometry;
using Ficep.Utils;
using System;
using System.Linq;

namespace Ficep.RobServer.Utility3D
{
    /// <summary>
    /// LineEdge descrive un edge di tipo linea.
    /// E' il caso dell'edge di una mesh (tipo STL) in cui una superficie è descritta da triangoli e il lato di ciascun triangolo
    /// potenzialmente può essere un edge (in realtà gli edge vengono estratti come il sottoinsieme di lati che appartengono a
    /// 2 triangoli non complanari)
    /// Dette s1 e s2 le superfici cui appartengono i 2 triangoli "adiacenti" della mesh che hanno in comuni l'edge, un LineEdge è caratterizzato da:
    /// 
    /// -   2 punti (estremi) StartPoint e EndPoint
    /// -   2 normali Normal1 e Normal2 alle superfici s1 e s2
    /// -   2 vettori V1Start e V2Start associati a StartPoint, che rappresentano le direzioni di 2 vettori ortogonali
    ///     al segmento [StartPoint, EndPoint] e  appartenenti ai piani delle 2 superfici (sono i vettori che potenzialmente rappresentano
    ///     le direzioni di taglio)
    /// -   2 vettori V1End e V2End, con lo stesso significato di V1Start e V1End ma associati a EndPoint
    /// -   V1Start e V1End sono i vettori identificati come candidati per essere utilizzati come direzioni di taglio
    ///     (V2Start e V2End probabilmente non verranno mai utilizzati)
    ///     
    /// </summary>
    public class EyeLineEdge
    {
        public enum LineEdgeType { INT, EST, UNDEFINED };
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public Vector3D Normal1 {  get; set; }
        public Vector3D Normal2 { get; set; }
        // Default vector
        public Vector3D V1Start { get; set; }
        public Vector3D V2Start { get; set; }
        // Default vector
        public Vector3D V1End { get; set; }
        public Vector3D V2End { get; set; }
        public LineEdgeType Type { get; set; }
        public EyeCuttingEdge.ChamferType ChamferType { get; set; }

        public EyeLineEdge(in Point3D startPoint, in Point3D endPoint, in Vector3D normal1, in Vector3D normal2, in Vector3D v1Start, in Vector3D v2Start, in Vector3D v1End, in Vector3D v2End, in double distanceTol, in Brep finalPart, EyeCuttingEdge.ChamferType  chamferType = EyeCuttingEdge.ChamferType.None) 
        { 
            StartPoint = (Point3D)startPoint.Clone();
            EndPoint = (Point3D)endPoint.Clone();
            Normal1 = (Vector3D)normal1.Clone();
            Normal2 = (Vector3D)normal2.Clone();
            V1Start = (Vector3D)v1Start.Clone();
            V2Start = (Vector3D)v2Start.Clone();
            V1End = (Vector3D)v1End.Clone();
            V2End = (Vector3D)v2End.Clone();
            SetType(distanceTol, finalPart);
            ChamferType = chamferType;
        }

        private void SetType(double distanceTol, in Brep finalPart)
        {
            double minX = finalPart.EstimateBoundingBox(null, null).Min(x => x.X),
                   maxX = finalPart.EstimateBoundingBox(null, null).Max(x => x.X);

            if (StartPoint.X.IsEqualTo(minX, distanceTol) || StartPoint.X.IsEqualTo(maxX, distanceTol))
                Type = LineEdgeType.EST;
            else
                Type = LineEdgeType.INT;
        }
       
    }
}
