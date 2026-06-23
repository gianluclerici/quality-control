using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static Ficep.AnyCut.Common.Constants;

namespace Ficep.RobServer.Utility3D
{
    public class EyeProfile : IProfile
    {

        public string CodePrf { get; set; }

        public double SA { get; set; }
        public double TA { get; set; }
        public double SB { get; set; }
        public double TB { get; set; }
        public double Radius { get; set; }
        

        public Region Region { get; private set; }

        public EyeProfile(string CodePrf, double sA, double tA, double sB, double tB, double r)
        {
            this.CodePrf = CodePrf;
            SA = sA;
            TA = tA;
            SB = sB;
            TB = tB;
            Radius = r;

            // Setting the region 
            SetRegion();
        }
        public EyeProfile() 
        {
            this.CodePrf = "I";
            SA = 0;
            TA = 0;
            SB = 0;
            TB = 0;
            Radius = 0;
            Region = null;
        }

        public bool SetRegion()
        {
            if (CodePrf == "I")
            {
                double y1 = 0, z1 = 0, y2 = 0, z2 = SB;
                Line line1 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = TB;
                z2 = z1;
                Line line2 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1;
                z2 = SB / 2 + TA / 2 + Radius;
                Arc arc1 = null, arc2 = null, arc3 = null, arc4 = null;
                Line line3 = new Line(Plane.YZ, y1, z1, y2, z2);
                if (Radius != 0)
                    arc1 = new Arc(Plane.YZ, new Point3D(y2 + Radius, z2), new Point3D(y2, z2), new Point3D(TB + Radius, SB / 2 + TA / 2));
                y1 = TB + Radius;
                z1 = SB / 2 + TA / 2;
                y2 = SA - TB - Radius;
                z2 = z1;
                Line line4 = new Line(Plane.YZ, y1, z1, y2, z2);
                if (Radius != 0)
                    arc2 = new Arc(Plane.YZ, new Point3D(y2, z2 + Radius), new Point3D(y2, z2), new Point3D(y2 + Radius, z2 + Radius));
                y1 = y2 + Radius;
                z1 = z2 + Radius;
                y2 = y1;
                z2 = SB;
                Line line5 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1 + TB;
                z2 = z1;
                Line line6 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1;
                z2 = 0;
                Line line7 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1 - TB;
                z2 = z1;
                Line line8 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1;
                z2 = SB / 2 - TA / 2 - Radius;
                Line line9 = new Line(Plane.YZ, y1, z1, y2, z2);
                if (Radius != 0)
                    arc3 = new Arc(Plane.YZ, new Point3D(y2 - Radius, z2), new Point3D(y2, z2), new Point3D(y2 - Radius, z2 + Radius));
                y1 = y2 - Radius;
                z1 = SB / 2 - TA / 2;
                y2 = TB + Radius;
                z2 = z1;
                Line line10 = new Line(Plane.YZ, y1, z1, y2, z2);
                if (Radius != 0)
                    arc4 = new Arc(Plane.YZ, new Point3D(y2, z2 - Radius), new Point3D(y2, z2), new Point3D(TB, z2 - Radius));
                y1 = TB;
                z1 = z2 - Radius;
                y2 = y1;
                z2 = 0;
                Line line11 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y1 = 0;
                z2 = z1;
                Line line12 = new Line(Plane.YZ, y1, z1, y2, z2);
                CompositeCurve profile = null;
                if (Radius != 0)
                    profile = new CompositeCurve(line1, line2, line3, arc1, line4, arc2, line5, line6, line7, line8, line9, arc3, line10, arc4, line11, line12);
                else
                    profile = new CompositeCurve(line1, line2, line3, line4, line5, line6, line7, line8, line9, line10, line11, line12);
                Region = new Region(profile);
            }
            else if (CodePrf == "Q")
            {
                bool flip = true;
                double R = Radius, r = Math.Max(Radius - TA, 0.1);
                Arc arc1 = null, arc2 = null, arc3 = null, arc4 = null;
                Line line1 = new Line(Plane.YZ, 0, R, 0, SB - R);
                if (R != 0)
                    arc1 = new Arc(Plane.YZ, new Point3D(0, R, SB - R), R, new Point3D(0, 0, SB - R), new Point3D(0, R, SB), flip);
                Line line2 = new Line(Plane.YZ, R, SB, SA - R, SB);
                if (R != 0)
                    arc2 = new Arc(Plane.YZ, new Point3D(0, SA - R, SB - R), R, new Point3D(0, SA - R, SB), new Point3D(0, SA, SB - R), flip);
                Line line3 = new Line(Plane.YZ, SA, SB - R, SA, R);
                if (R != 0)
                    arc3 = new Arc(Plane.YZ, new Point3D(0, SA - R, R), R, new Point3D(0, SA, R), new Point3D(0, SA - R, 0), flip);
                Line line4 = new Line(Plane.YZ, SA - R, 0, R, 0);
                if (R != 0)
                    arc4 = new Arc(Plane.YZ, new Point3D(0, R, R), R, new Point3D(0, R, 0), new Point3D(0, 0, R), flip);

                double innerEdge = Math.Max(R, TA);
                Arc arc5 = null, arc6 = null, arc7 = null, arc8 = null;
                Line line5 = new Line(Plane.YZ, TA, innerEdge, TA, SB - innerEdge);
                if (R != 0)
                    arc5 = new Arc(Plane.YZ, new Point3D(0, R, SB - R), r, new Point3D(0, TA, SB - R), new Point3D(0, R, SB - TA), flip);
                Line line6 = new Line(Plane.YZ, innerEdge, SB - TA, SA - innerEdge, SB - TA);
                if (R != 0)
                    arc6 = new Arc(Plane.YZ, new Point3D(0, SA - R, SB - R), r, new Point3D(0, SA - R, SB - TA), new Point3D(0, SA - TA, SB - R), flip);
                Line line7 = new Line(Plane.YZ, SA - TA, SB - innerEdge, SA - TA, innerEdge);
                if (R != 0)
                    arc7 = new Arc(Plane.YZ, new Point3D(0, SA - R, R), r, new Point3D(0, SA - TA, R), new Point3D(0, SA - R, TA), flip);
                Line line8 = new Line(Plane.YZ, SA - innerEdge, TA, innerEdge, TA);
                if (R != 0)
                    arc8 = new Arc(Plane.YZ, new Point3D(0, R, R), r, new Point3D(0, R, TA), new Point3D(0, TA, R), flip);

                CompositeCurve square1 = null;
                if (R != 0)
                    square1 = new CompositeCurve(line1, arc1, line2, arc2, line3, arc3, line4, arc4);
                else
                    square1 = new CompositeCurve(line1, line2, line3, line4);

                CompositeCurve square2 = null;
                if (R != 0)
                    square2 = new CompositeCurve(line5, arc5, line6, arc6, line7, arc7, line8, arc8);
                else
                    square2 = new CompositeCurve(line5, line6, line7, line8);

                //
                //  Questi 2 Reverse sono necessari per far diventare antiorarie le 2 CompositeCurve (create orarie)
                //
                square1.Reverse();
                square2.Reverse();

                Region = new Region(square1, square2);
            }
            else if (CodePrf == "U")
            {
                double y1 = 0, z1 = 0, y2 = 0, z2 = SB;
                Line line1 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = TB;
                z2 = z1;
                Line line2 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1;
                z2 = TA + Radius;
                Line line3 = new Line(Plane.YZ, y1, z1, y2, z2);
                Arc arc1 = null, arc2 = null;
                if (Radius != 0)
                    arc1 = new Arc(Plane.YZ, new Point3D(y2 + Radius, z2), new Point3D(y2, z2), new Point3D(TB + Radius, z2 - Radius));
                y1 = TB + Radius;
                z1 = TA;
                y2 = SA - TB - Radius;
                z2 = z1;
                Line line4 = new Line(Plane.YZ, y1, z1, y2, z2);
                if (Radius != 0)
                    arc2 = new Arc(Plane.YZ, new Point3D(y2, z2 + Radius), new Point3D(y2, z2), new Point3D(y2 + Radius, z2 + Radius));
                y1 = y2 + Radius;
                z1 = z2 + Radius;
                y2 = y1;
                z2 = SB;
                Line line5 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1 + TB;
                z2 = z1;
                Line line6 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1;
                z2 = 0;
                Line line7 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = 0;
                z2 = 0;
                Line line8 = new Line(Plane.YZ, y1, z1, y2, z2);
                CompositeCurve curves = null;
                if (Radius != 0)
                    curves = new CompositeCurve(line1, line2, line3, arc1, line4, arc2, line5, line6, line7, line8);
                else
                    curves = new CompositeCurve(line1, line2, line3, line4, line5, line6, line7, line8);
                Region = new Region(curves);
            }
            else if (CodePrf == "L")
            {
                double y1 = 0, z1 = 0, y2 = 0, z2 = SA;
                Line line1 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = TA;
                z2 = z1;
                Line line2 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = y1;
                z2 = TB + Radius;
                Line line3 = new Line(Plane.YZ, y1, z1, y2, z2);
                Arc arc1 = null;
                if (Radius != 0)
                    arc1 = new Arc(Plane.YZ, new Point3D(y2 + Radius, z2), new Point3D(y2, z2), new Point3D(TA + Radius, z2 - Radius));
                y1 = TA + Radius;
                z1 = TB;
                y2 = SB;
                z2 = z1;
                Line line4 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = SB;
                z2 = 0;
                Line line5 = new Line(Plane.YZ, y1, z1, y2, z2);
                y1 = y2;
                z1 = z2;
                y2 = 0;
                z2 = 0;
                Line line6 = new Line(Plane.YZ, y1, z1, y2, z2);

                CompositeCurve curves = null;
                if (Radius != 0)
                    curves = new CompositeCurve(line1, line2, line3, arc1, line4, line5, line6);
                else
                    curves = new CompositeCurve(line1, line2, line3, line4, line5, line6);
                Region = new Region(curves);
            }
            else if (CodePrf == "F")
            {
                Line line1 = new Line(Plane.YZ, 0, 0, SA, 0);
                Line line2 = new Line(Plane.YZ, SA, 0, SA, TA);
                Line line3 = new Line(Plane.YZ, SA, TA, 0, TA);
                Line line4 = new Line(Plane.YZ, 0, TA, 0, 0);

                CompositeCurve curves = null;
                curves = new CompositeCurve(line1, line2, line3, line4);
                Region = new Region(curves);
            }
            else if (CodePrf == "R")
            {
                Circle outerCircle = new Circle(Plane.YZ, SA / 2);
                Circle innerCircle = new Circle(Plane.YZ, SA / 2 - TA);
                Region = new Region(innerCircle, outerCircle);
                Region.Translate(0, SA / 2, SA / 2);
            }
            else
                return false;

            return true;
        }
    }

    /// <summary>
    /// Class containing the parameters describing the workpiece and the brep solid
    /// </summary>
    public class EyeWorkPiece : IWorkPiece
    {
        /// <summary>
        /// Class containing the parameters of the profile 
        /// </summary>
        public IProfile Prf { get; set; }
        /// <summary>
        /// Length of the workpiece
        /// </summary>
        public double Lp { get; set; }
        /// <summary>
        /// Brep solid representing the workpiece
        /// </summary>
        public Brep Solid { get; private set; }
        /// <summary>
        ///  List of machining features
        /// </summary>
        public List<EyeFeature> Features { get; set; }

        public EyeWorkPiece(IWorkPiece wp)
        {
            Lp = wp.Lp;
            Prf = new EyeProfile(wp.Prf.CodePrf, wp.Prf.SA, wp.Prf.TA, wp.Prf.SB, wp.Prf.TB, wp.Prf.Radius);
            Features = new List<EyeFeature>();
        }

        /// <summary>
        /// Initializes the parameters of the profile
        /// </summary>
        /// <param name="CodePrf">
        /// Code profile
        /// </param>
        /// <param name="sA">
        /// Web lenght 
        /// </param>
        /// <param name="tA">
        /// Thickness of the web
        /// </param>
        /// <param name="sB">
        /// Flange lenght 
        /// </param>
        /// <param name="tB">
        /// Thickness of the web
        /// </param>
        /// <param name="r">
        /// Radius
        /// </param>
        public EyeWorkPiece(string CodePrf, double sA, double tA, double sB, double tB, double r, double lp = 0)
        {
            Lp = lp;
            Prf = new EyeProfile(CodePrf, sA, tA, sB, tB, r);
            Features = new List<EyeFeature>();
        }

        public EyeWorkPiece()
        {
            Prf = new EyeProfile();
            Features = new List<EyeFeature>();
        }
        /// <summary>
        /// Create the brep solid of the profile
        /// </summary>
        /// <param name="brepTol">
        /// tolerance with which the brep is created
        /// </param>
        public void CreateSolidRawPart(double brepTol = 0.01)
        {
            if (Prf is EyeProfile eyePrf)
            {
                if (eyePrf.SetRegion())
                {
                    Solid = eyePrf.Region.ExtrudeAsBrep(Lp, 0, brepTol);
                    Solid.Rebuild(brepTol);
                }
            }
        }

        public void CreateSolidRawPart(double totalLp, double brepTol = 0.01)
        {
            if (Prf is EyeProfile eyePrf)
            {
                eyePrf.SetRegion();
                Solid = eyePrf.Region.ExtrudeAsBrep(totalLp, 0, brepTol);
                Solid.Rebuild(brepTol);
            }
        }

        public bool ComputeFeatures(in double distanceTol, in double arcSegmentLength, in Brep finalPart)
        {
            List<List<EyeCuttingEdge>> facesList = new List<List<EyeCuttingEdge>>();
            List<List<EyeCuttingEdge>> test = new List<List<EyeCuttingEdge>>();

            // Compute the cutting vectors
            if (!EyeUtils.ComputeVectors(finalPart, this, distanceTol, arcSegmentLength, true, ref facesList))
                return false;

            if (facesList.Count == 0)
                return false;

            EyeFeature feature1;
            var temp = Features;
            // Compute features until the feature's facelist is empty 
            do
            {
                feature1 = new EyeFeature(finalPart);
                feature1.ComputeFeature(facesList, this, distanceTol, ref temp);
            } while (feature1.FaceList.Count != 0);

            // Remove the last feature that is empty 
            Features.RemoveAt(Features.Count - 1);

            foreach (var feature in Features)
            {
                // Compute the edge indices for just the edges with the cuttingvectors computed, i.e. the desired vectors
                var edges = feature.FaceList.SelectMany(x => x.Select(y => y.NormalLinesAdjacentFace == null ? null : y)).Where(ce => ce != null).ToList();

                var tempFinalPart = finalPart;
                var tempTol = distanceTol;

                if (Prf.CodePrf == "R")
                {

                    // Get the curves on the outer and inner cylinders
                    var edgesCurvesOuter = edges.Where(e => EyeUtils.IsOnCircle(new Point2D(Prf.SA / 2, Prf.SA / 2), Prf.SA / 2, new Point2D(tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Y, tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Z), tempTol)).ToList();
                    var edgesCurvesInner = edges.Where(e => EyeUtils.IsOnCircle(new Point2D(Prf.SA / 2, Prf.SA / 2), Prf.SA / 2 - Prf.TA, new Point2D(tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Y, tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Z), tempTol)).ToList();
                    var remainingEdges = edges.Where(e => !EyeUtils.IsOnCircle(new Point2D(Prf.SA / 2, Prf.SA / 2), Prf.SA / 2 - Prf.TA, new Point2D(tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Y, tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Z), tempTol)
                                                                &&
                                                                !EyeUtils.IsOnCircle(new Point2D(Prf.SA / 2, Prf.SA / 2), Prf.SA / 2, new Point2D(tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Y, tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.Z), tempTol)
                                                                && (
                                                                    (tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.X.IsEqualTo(0, tempTol) && tempFinalPart.Edges[e.EdgeIndex].Curve.EndPoint.X.IsEqualTo(0, tempTol)) ||
                                                                    (tempFinalPart.Edges[e.EdgeIndex].Curve.StartPoint.X.IsEqualTo(Lp, tempTol) && tempFinalPart.Edges[e.EdgeIndex].Curve.EndPoint.X.IsEqualTo(Lp, tempTol))
                                                                   )
                                                    ).ToList();

                    if (edgesCurvesOuter.Count() != 0)
                    {
                        CompositeCurve ccOuter = new CompositeCurve(edgesCurvesOuter.Select(e => tempFinalPart.Edges[e.EdgeIndex].Curve));
                        ccOuter.SortAndOrient();

                        if (!feature.ComputeEdgeList(distanceTol, this, ccOuter.CurveList, out List<Line> lines1))
                            return false;
                    }

                    if (edgesCurvesInner.Count() != 0)
                    {
                        CompositeCurve ccInner = new CompositeCurve(edgesCurvesInner.Select(e => tempFinalPart.Edges[e.EdgeIndex].Curve));
                        ccInner.SortAndOrient();

                        if (!feature.ComputeEdgeList(distanceTol, this, ccInner.CurveList, out List<Line> lines2))
                            return false;
                    }

                    // Compute the vectors on the edges having the worked face in the YZ plane at x = 0 or x = lp (Landing face)
                    // and being the edge's curve inside the region defined by the outer and inner circle

                    if (remainingEdges.Count != 0)
                    {
                        List<EyeCuttingEdge> landingEdges = new List<EyeCuttingEdge>();

                        foreach (EyeCuttingEdge edge in remainingEdges)
                        {
                            if (
                                finalPart.Faces[edge.WorkedFaceIndex].Parametric[0].IsPlanar(distanceTol, out Plane plane) &&
                                (plane.Origin.X.IsEqualTo(0, distanceTol) || plane.Origin.X.IsEqualTo(Lp, distanceTol))
                                && plane.Equation.Y == 0 && plane.Equation.Z == 0
                                )
                                landingEdges.Add(edge);
                        }

                        if (!feature.ComputeEdgeList(distanceTol, this, landingEdges, out List<Line> lines2))
                            return false;
                    }
                }
                else
                {
                    if (!feature.ComputeEdgeList(distanceTol, this, edges, out List<Line> lines2))
                        return false;
                }
            }

            return true;
        }
    }
}
