using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Ficep.RobServer.Utility3D
{
    ///<summary>
    /// Subclass of Line
    ///</summary>
    public class EyeContourLine : Line, IEyeCurve
    {
        //  side è il piano profilo di apprtenenza
        public string side { get; set; } = "C";
        //  disconnected = true se l'EyeContourLine va creato come primo punto di un Contour
        public bool disconnected { get; set; } = false;
        //  overlapped = true se l'EyeContourLine definisce un overlap
        public bool overlapped { get; set; } = false;

        public EyeCuttingEdge.ChamferType ChamferType { get; set; } = EyeCuttingEdge.ChamferType.None;

        public EyeContourLine(Point3D start, Point3D end, string side,
            bool disconnected = false, bool overlapped = false, EyeCuttingEdge.ChamferType chamferType = EyeCuttingEdge.ChamferType.None)
            : base(start, end)
        {
            this.side = side;
            this.disconnected = disconnected;
            this.overlapped = overlapped;
            this.ChamferType = chamferType;
        }

        public EyeContourLine(Line another, string side,
            bool disconnected = false, bool overlapped = false, EyeCuttingEdge.ChamferType chamferType = EyeCuttingEdge.ChamferType.None)
            : base(another)
        {
            this.side = side;
            this.disconnected = disconnected;
            this.overlapped = overlapped;
            this.ChamferType = chamferType;
        }
    }

    public class EyeContourArc : Arc, IEyeCurve
    {
        //  side è il piano profilo di apprtenenza
        public string side { get; set; } = "C";
        public EyeContourArc(Point3D center, Point3D start, Point3D end, string side)
           : base(center, start, end)
        {
            this.side = side;
        }

        public EyeContourArc(Arc arc, string side)
            : base(arc)
        {
            this.side = side;
        }
    }

    public class EyeContourCompositeCurve : CompositeCurve, IEyeCurve
    {
        public string side { get; set; }
        private List<IEyeCurve> eyeCurveList;

        public new List<IEyeCurve> CurveList
        {
            get { return eyeCurveList; }
            private set { }
        }

        public EyeContourCompositeCurve(CompositeCurve cc, string side) : base(cc)
        {
            eyeCurveList = new List<IEyeCurve>();
            this.side = side;
            ComputeEyeCurveList();
        }

        public EyeContourCompositeCurve(IEnumerable<ICurve> curveList, string side) : base(curveList)
        {
            eyeCurveList = new List<IEyeCurve>();
            this.side = side;
            ComputeEyeCurveList();
        }
        public EyeContourCompositeCurve(string side, params ICurve[] curveList) : base(curveList)
        {
            eyeCurveList = new List<IEyeCurve>();
            this.side = side;
            ComputeEyeCurveList();
        }

        public void ComputeEyeCurveList()
        {
            foreach (ICurve curve in base.CurveList)
            {
                if (curve is Line line)
                    eyeCurveList.Add(line.ConvertToEyeCurve(side));
                else if (curve is Arc arc)
                    eyeCurveList.Add(arc.ConvertToEyeCurve(side));
                else if (curve is CompositeCurve cc)
                    eyeCurveList.Add(cc.ConvertToEyeCurve(side));
            }
        }
    }

    public interface IEyeCurve : ICurve
    {
        string side { get; set; }
    }

    public static class MyCurveMethods
    {
        public static IEyeCurve ConvertToEyeCurve(this ICurve curve, string plane)
        {
            if (curve == null)
                return null;
            else if (curve is Line line)
                return new EyeContourLine(line, plane);
            else if (curve is Arc arc)
                return new EyeContourArc(arc, plane);
            else if (curve is CompositeCurve cc)
                return new EyeContourCompositeCurve(cc, plane);
            else
                return null;
        }

        public static void Translate(this ICurve curve, double x, double y, double z = 0)
        {
            if (curve == null)
                return;
            else if (curve is Line line)
                line.Translate(x, y, z);
            else if (curve is Arc arc)
                arc.Translate(x, y, z);
            else if (curve is CompositeCurve cc)
                cc.Translate(x, y, z);
            else if (curve is Curve c)
                c.Translate(x, y, z);
            else if (curve is Circle circ)
                circ.Translate(x, y, z);
            else
                throw new NotImplementedException("Not recognized type of ICurve");
                
        }
    }
}