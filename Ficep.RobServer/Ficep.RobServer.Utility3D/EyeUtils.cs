using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.AnyCut.Mathematics;
using Ficep.RobServer.Data;
using Ficep.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using static devDept.Eyeshot.Entities.Brep;
using Vector3D = devDept.Geometry.Vector3D;
using Circle = devDept.Eyeshot.Entities.Circle;
using Line = devDept.Eyeshot.Entities.Line;

namespace Ficep.RobServer.Utility3D
{
    public partial class EyeUtils
    {

        //*******************************************
        //                 LAYERS
        //*******************************************
        public static string differenceSolids = "DifferenceSolids";
        public static string workedPiece = "FinalPart";
        public static string vectors = "Vectors";
        public static string macroLungCurves = "LungCurves";

        //
        //  Calcolo le features e i vettori dell'EyeWorkPiece a partire dal Brep
        //
        public static bool ComputeEyeWorkPieceFeaturesAndVectorsFromBrep(EyeWorkPiece eyeWp, double tolBrep, double tolLinear, double arcSegmentLength, log4net.ILog log, ref Brep brep, out List<devDept.Eyeshot.Entities.Line> lines, out List<devDept.Eyeshot.Entities.Line> oppositeLines)
        {
            eyeWp.Features.Clear();
            lines = null;
            oppositeLines = null
                ;
            try
            {
                brep.Rebuild(tolBrep);
                if (!eyeWp.ComputeFeatures(tolLinear, arcSegmentLength, brep))
                    return false;
            }
            catch (Exception e)
            {
                log.Debug(e.Message);

                return false;
            }

            lines = new List<devDept.Eyeshot.Entities.Line>();
            oppositeLines = new List<devDept.Eyeshot.Entities.Line>();

            foreach (var feature in eyeWp.Features)
            {
                if (!feature.ComputeLines(out List<devDept.Eyeshot.Entities.Line> tempLines))
                    return false;

                lines.AddRange(tempLines);

                foreach (var line in lines)
                {
                    devDept.Eyeshot.Entities.Line oppositeLine = new devDept.Eyeshot.Entities.Line(line.StartPoint, line.StartPoint - line.Direction);
                    oppositeLines.Add(oppositeLine);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Compute the area of a polygon in the 2D space assuming that the edges of the polygon do not intersect
        /// </summary>
        /// <param name="polygon">
        /// List of vertices of the polygon in the 2D space
        /// </param>
        /// <returns>
        /// The calculated area
        /// </returns>
        public static double ComputePolygonArea(List<Point2D> polygon)
        {
            int numPoints = polygon.Count;
            double area = 0;

            for (int i = 0; i < numPoints; i++)
            {
                int j = (i + 1) % numPoints;
                area += (polygon[i].X * polygon[j].Y) - (polygon[j].X * polygon[i].Y);
            }

            return Math.Abs(area / 2);
        }
        // Get the normal plane with respect to the curve tangent starting vector 
        internal static void GetPlaneNormalToCurve(ICurve curve, ref Plane normalPlane)
        {
            if (curve.StartTangent.X < 0 || curve.StartTangent.Y < 0 || curve.StartTangent.Z < 0)
                normalPlane = new Plane(curve.StartPoint, -1 * curve.StartTangent);
            else
                normalPlane = new Plane(curve.StartPoint, curve.StartTangent);
        }

        // Given the angle in degree of the chamfer, the depth of the chamfer, the workpiece and the list of the curves at the right height, returns the solid to be subtracted to the raw part in order to obtain the chamfer
        // the depth of the chamfer is the lenght perpendicular to the edge to be removed  
        /*
             * pos = "CSI" stands for chamfer on the Superior part of the side C at the Initial part
             * pos = "CII" stands for chamfer on the Inferior part of the side C at the Initial part
             * pos = "CSF" stands for chamfer on the Superior part of the side C at the Final part
             * pos = "CIF" stands for chamfer on the Inferior part of the side C at the Final part
             * pos = "AEI" stands for chamfer on the Extern part of the side A at the Initial part
             * pos = "AII" stands for chamfer on the intern part of the side A at the Initial part
             * pos = "AEF" stands for chamfer on the Extern part of the side A at the Final part
             * pos = "AIF" stands for chamfer on the intern part of the side A at the Final part
             * pos = "BEI" stands for chamfer on the Extern part of the side B at the Initial part
             * pos = "BII" stands for chamfer on the intern part of the side B at the Initial part
             * pos = "BEF" stands for chamfer on the Extern part of the side B at the Final part
             * pos = "BIF" stands for chamfer on the intern part of the side B at the Final part
             */
        public static bool Chamfer(in double angleDeg, in double depth, in string pos, in IWorkPiece wp, in List<ICurve> rails, in double brepTolerance, ref Brep brep)
        {
            if (rails.Count == 0)
                throw new ArgumentException("The curve list is empty");

            string posUpper = pos.ToUpper();

            CompositeCurve cc = new CompositeCurve(rails);
            double alfar = (angleDeg) * Math.PI / 180;
            Plane normalToRail = null;
            Region region = null;
            
            
            //*/
            if (posUpper == "CSI" || posUpper == "CII" || posUpper == "CSF" || posUpper == "CIF")
            {
                if (posUpper == "CSI")
                {
                    // Translate the composite curve at the right position
                    cc.Translate(0, 0, wp.Prf.SB / 2 + wp.Prf.TA / 2);
                    // Get the normal plane
                    GetPlaneNormalToCurve(cc, ref normalToRail);
                    // Compute the angular and q coefficients of the line of the triangle with the desired angle inclination 
                    double m = Math.Tan(Math.PI / 2 - alfar);
                    double q = -depth;
                    // Start with the vertex of the triangle outside the line, in order to make more robust the brep.difference method
                    Point2D vert1 = new Point2D(1, -1);
                    // Compute the other two vertex knowing the line equation of the triangle and the first vertex
                    Point2D vert2 = new Point2D(m * -1 + q, -1);
                    Point2D vert3 = new Point2D(1, (1 - q) / m);
                    List<Point2D> vertices = new List<Point2D>();
                    vertices.Add(vert1);
                    vertices.Add(vert2);
                    vertices.Add(vert3);
                    region =  Region.CreatePolygon(normalToRail, vert1, vert2, vert3);;
                }
                else if (posUpper == "CII")
                {
                    // Translate the composite curve at the right position
                    cc.Translate(0, 0, wp.Prf.SB / 2 - wp.Prf.TA / 2);
                    // Get the normal plane
                    GetPlaneNormalToCurve(cc, ref normalToRail);
                    double m = Math.Tan(Math.PI / 2 + alfar);
                    double q = depth;
                    Point2D vert1 = new Point2D(-1, -1);
                    Point2D vert2 = new Point2D(m * -1 + q, -1);
                    Point2D vert3 = new Point2D(-1, (-1 - q) / m);
                    List<Point2D> vertices = new List<Point2D>();
                    vertices.Add(vert1);
                    vertices.Add(vert2);
                    vertices.Add(vert3);
                    region = devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);;
                }
                else if (posUpper == "CSF")
                {
                    // Translate the composite curve at the right position
                    cc.Translate(0, 0, wp.Prf.SB / 2 + wp.Prf.TA / 2);
                    // Get the normal plane
                    GetPlaneNormalToCurve(GetMirroredCompositeCurve(cc, wp), ref normalToRail);
                    double m = -Math.Tan(Math.PI / 2 - alfar);
                    double q = -depth;
                    Point2D vert1 = new Point2D(1, 1);
                    Point2D vert2 = new Point2D(m * 1 + q, 1);
                    Point2D vert3 = new Point2D(1, (1 - q) / m);
                    List<Point2D> vertices = new List<Point2D>();
                    vertices.Add(vert1);
                    vertices.Add(vert2);
                    vertices.Add(vert3);
                    region = devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);;
                }
                else
                {
                    // Translate the composite curve at the right position
                    cc.Translate(0, 0, wp.Prf.SB / 2 - wp.Prf.TA / 2);
                    // Get the normal plane
                    GetPlaneNormalToCurve(GetMirroredCompositeCurve(cc, wp), ref normalToRail);
                    double m = -Math.Tan(Math.PI / 2 + alfar);
                    double q = depth;
                    Point2D vert1 = new Point2D(-1, 1);
                    Point2D vert2 = new Point2D(m * 1 + q, 1);
                    Point2D vert3 = new Point2D(-1, (-1 - q) / m);
                    List<Point2D> vertices = new List<Point2D>();
                    vertices.Add(vert1);
                    vertices.Add(vert2);
                    vertices.Add(vert3);
                    region = devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);;
                }
            }
            else
            {
                // Function used for flange A extern chamfer
                Func<double, double, devDept.Eyeshot.Entities.Region> downLeftTriangle = (alfaRad, triangleBase) =>
                {
                    double m = Math.Tan(Math.PI - alfaRad);
                    double q = -m * triangleBase;
                    Point2D vert1 = new Point2D(-1, (-1 - q) / m);
                    Point2D vert2 = new Point2D(-1 * m + q, -1);
                    Point2D vert3 = new Point2D(-1, -1);
                    List<Point2D> vertices = new List<Point2D>();
                    vertices.Add(vert1);
                    vertices.Add(vert2);
                    vertices.Add(vert3);
                    return devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);;
                };
                // Function used for flange A intern chamfer
                Func<double, double, devDept.Eyeshot.Entities.Region> downRightTriangle = (alfaRad, triangleBase) =>
                {
                    double x0 = -triangleBase;
                    double m = Math.Tan(alfaRad);
                    double q = -m * x0;
                    Point2D vert1 = new Point2D(-1, (-1 - q) / m);
                    Point2D vert2 = new Point2D(1 * m + q, 1);
                    Point2D vert3 = new Point2D(-1, 1);
                    List<Point2D> vertices = new List<Point2D>();
                    vertices.Add(vert1);
                    vertices.Add(vert2);
                    vertices.Add(vert3);
                    return devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);;
                };
                // Function used for flange A final intern chamfer
                Func<double, double, devDept.Eyeshot.Entities.Region> upRightTriangle = (alfaRad, triangleBase) =>
                {
                    double m = -Math.Tan(alfaRad);
                    double q = m * triangleBase;
                    Point2D vert1 = new Point2D(1, (1 - q) / m);
                    Point2D vert2 = new Point2D(1 * m + q, 1);
                    Point2D vert3 = new Point2D(1, 1);
                    List<Point2D> vertices = new List<Point2D>();
                    vertices.Add(vert1);
                    vertices.Add(vert2);
                    vertices.Add(vert3);
                    return devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);
                };
                // Function used for flange A final extern chamfer
                Func<double, double, devDept.Eyeshot.Entities.Region> upLeftTriangle = (alfaRad, triangleBase) =>
                {
                    double m = Math.Tan(alfaRad);
                    double q = -m * triangleBase;
                    Point2D vert1 = new Point2D(1, (1 - q) / m);
                    Point2D vert2 = new Point2D(-1 * m + q, -1);
                    Point2D vert3 = new Point2D(1, -1);
                    return devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);
                };

                if (posUpper == "AEI")
                {
                    GetPlaneNormalToCurve(cc, ref normalToRail);
                    region = downLeftTriangle(alfar, depth);
                }
                else if (posUpper == "AII")
                {
                    cc.Translate(0, wp.Prf.TB);
                    GetPlaneNormalToCurve(cc, ref normalToRail);
                    region = downRightTriangle(alfar, depth);
                }
                else if (posUpper == "AEF")
                {
                    GetPlaneNormalToCurve(GetMirroredCompositeCurve(cc, wp), ref normalToRail);
                    region = upLeftTriangle(alfar, depth);
                }
                else if (posUpper == "AIF")
                {
                    cc.Translate(0, wp.Prf.TB);
                    GetPlaneNormalToCurve(GetMirroredCompositeCurve(cc, wp), ref normalToRail);
                    region = upRightTriangle(alfar, depth);
                }
                else if (posUpper == "BEI")
                {
                    cc.Translate(0, wp.Prf.SA);
                    GetPlaneNormalToCurve(cc, ref normalToRail);
                    region = downRightTriangle(alfar, depth);
                }
                else if (posUpper == "BII")
                {
                    cc.Translate(0, wp.Prf.SA - wp.Prf.TB);
                    GetPlaneNormalToCurve(cc, ref normalToRail);
                    region = downLeftTriangle(alfar, depth);
                }
                else if (posUpper == "BEF")
                {
                    cc.Translate(0, wp.Prf.SA);
                    GetPlaneNormalToCurve(GetMirroredCompositeCurve(cc, wp), ref normalToRail);
                    region = upRightTriangle(alfar, depth);
                }
                else
                {
                    cc.Translate(0, wp.Prf.SA - wp.Prf.TB);
                    GetPlaneNormalToCurve(GetMirroredCompositeCurve(cc, wp), ref normalToRail);
                    region = upLeftTriangle(alfar, depth);
                }
            }
            try
            {
                brep = region.SweepAsBrep(cc, brepTolerance);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public static bool Chamfer(in ICurve rail, in IWorkPiece wp, in string extrusionPlane, in bool external, in bool flip, in double radAngle, in double depth, in double surplus, in double tolBrep, in double tolLinear, out Brep solid, bool normalToRailCurve = true)
        {
            solid = null;
            Point2D vert1 = null, vert2 = null, vert3 = null;
            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                 verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            // Compute the triangle vertices in the standard plane XY with the origin in (0, 0, 0)
            double dy = surplus * Math.Tan(Math.PI / 2 - radAngle),
                   dx = Math.Tan(radAngle) * surplus;

            // Compute the vertices of the triangle 
            vert1 = new Point2D(- surplus, -surplus);
            vert2 = new Point2D(- surplus, depth + dy);
            vert3 = new Point2D(depth * Math.Tan(radAngle) + dx, -surplus);

            // Compute the plane
            Plane sketchPlane = null;
            double x = rail.EndPoint.X - rail.StartPoint.X,
                   y = horizontalPlane ? rail.EndPoint.Y - rail.StartPoint.Y : rail.EndPoint.Z - rail.StartPoint.Z;
            
            double angle = normalToRailCurve ? Math.Atan2(x, y) : 0;

            if (extrusionPlane == "A")
            {
                sketchPlane = Plane.XY;

                if (rail is devDept.Eyeshot.Entities.Line && !angle.IsEqualTo(0, tolLinear))
                    sketchPlane.Rotate(angle, Vector3D.AxisY);

                if (!external)
                {
                    vert1.Y *= -1;
                    vert2.Y *= -1;
                    vert3.Y *= -1;
                }
                if (flip)
                {
                    vert1.X *= -1;
                    vert2.X *= -1;
                    vert3.X *= -1;
                }
            }
            else if (extrusionPlane == "B")
            {
                sketchPlane = Plane.XY;

                if (rail is devDept.Eyeshot.Entities.Line && !angle.IsEqualTo(0, tolLinear))
                    sketchPlane.Rotate(angle, Vector3D.AxisY);

                if (external)
                {
                    vert1.Y *= -1;
                    vert2.Y *= -1;
                    vert3.Y *= -1;
                }
                if (flip)
                {
                    vert1.X *= -1;
                    vert2.X *= -1;
                    vert3.X *= -1;
                }
            }
            else if (extrusionPlane == "C")
            {
                sketchPlane = Plane.XZ;

                if (rail is devDept.Eyeshot.Entities.Line && !angle.IsEqualTo(0, tolLinear))
                    sketchPlane.Rotate(angle, Vector3D.AxisMinusZ);

                if (external)
                {
                    vert1.Y *= -1;
                    vert2.Y *= -1;
                    vert3.Y *= -1;
                }
                if (flip)
                {
                    vert1.X *= -1;
                    vert2.X *= -1;
                    vert3.X *= -1;
                }
            }
            else if (extrusionPlane == "D")
            {
                sketchPlane = Plane.XZ;

                if (rail is devDept.Eyeshot.Entities.Line && !angle.IsEqualTo(0, tolLinear))
                    sketchPlane.Rotate(angle, Vector3D.AxisMinusZ);

                if (!external)
                {
                    vert1.Y *= -1;
                    vert2.Y *= -1;
                    vert3.Y *= -1;
                }
                if (flip)
                {
                    vert1.X *= -1;
                    vert2.X *= -1;
                    vert3.X *= -1;
                }
            }
            else 
                return false;


            sketchPlane.Translate(rail.StartPoint.AsVector);

            // Compute the sides of the triangle
            devDept.Eyeshot.Entities.Line l1 = new devDept.Eyeshot.Entities.Line(sketchPlane, vert1, vert2),
                 l2 = new devDept.Eyeshot.Entities.Line(sketchPlane, vert2, vert3),
                 l3 = new devDept.Eyeshot.Entities.Line(sketchPlane, vert3, vert1);

            CompositeCurve cc = new CompositeCurve(l1, l2, l3);
            Region triangle = new Region(cc);

            solid = triangle.SweepAsBrep(rail, tolBrep);

            return solid != null;
        }
        // Given a composite curve and the workpiece, it return the corresponding composite curve at the final side of the bar 
        private static CompositeCurve GetMirroredCompositeCurve(in CompositeCurve cc, in IWorkPiece wp)
        {
            CompositeCurve copyCC = (CompositeCurve)cc.Clone();
            List<ICurve> curves = copyCC.CurveList;
            List<ICurve> mirroredCurves = new List<ICurve>();

            foreach (var curve in curves)
            {
                if (curve is devDept.Eyeshot.Entities.Line)
                {
                    Point3D start = new Point3D(wp.Lp - curve.StartPoint.X, curve.StartPoint.Y, curve.StartPoint.Z);
                    Point3D end = new Point3D(wp.Lp - curve.EndPoint.X, curve.EndPoint.Y, curve.EndPoint.Z);
                    mirroredCurves.Add(new devDept.Eyeshot.Entities.Line(start, end));
                }
                else if (curve is Arc)
                {
                    Arc arc = (Arc)curve;
                    Point3D start = new Point3D(wp.Lp - curve.StartPoint.X, curve.StartPoint.Y, curve.StartPoint.Z);
                    Point3D end = new Point3D(wp.Lp - curve.EndPoint.X, curve.EndPoint.Y, curve.EndPoint.Z);
                    Point3D center = new Point3D(wp.Lp - arc.Center.X, arc.Center.Y, arc.Center.Z);
                    mirroredCurves.Add(new Arc(center, start, end));
                }
                else if (curve is Circle)
                {
                    Circle circle = (Circle)curve;
                    Point3D center = new Point3D(wp.Lp - circle.Center.X, circle.Center.Y, circle.Center.Z);
                    mirroredCurves.Add(new Circle(center, circle.Radius));
                }
            }
            return new CompositeCurve(mirroredCurves);
        }

        // Project curves on the specified plane
        public static List<ICurve> ProjectCurvesOnPlane(List<ICurve> curveList, Plane plane, bool arcSharingSegment = false)
        {
            List<ICurve> curvesOnPlane = new List<ICurve>();
            for (int i = 0; i < curveList.Count; i++)
            {
                if (curveList[i] is devDept.Eyeshot.Entities.Line)
                    curvesOnPlane.Add(new devDept.Eyeshot.Entities.Line(plane, curveList[i].StartPoint, curveList[i].EndPoint));
                else if (curveList[i] is Arc)
                {

                    Point3D start = new Point3D(((Arc)curveList[i]).StartPoint.X, 0, ((Arc)curveList[i]).StartPoint.Y);
                    Point3D mid = new Point3D(((Arc)curveList[i]).MidPoint.X, 0, ((Arc)curveList[i]).MidPoint.Y);
                    Point3D end = new Point3D(((Arc)curveList[i]).EndPoint.X, 0, ((Arc)curveList[i]).EndPoint.Y);
                    Arc arc1 = new Arc(start, mid, end, false);
                    curvesOnPlane.Add(arc1);
                }
                else if (curveList[i] is Circle)
                {
                    Point3D centre = new Point3D(((Circle)curveList[i]).Center.X, 0, ((Circle)curveList[i]).Center.Y);
                    curvesOnPlane.Add(new Circle(plane, centre, ((Circle)curveList[i]).Radius));
                }
            }
            return curvesOnPlane;
        }
        // Mirror, translate and subtract the brep feature passed in and returns the feature subtracted
        public static bool MirrorBrep(in IWorkPiece wp, in string side, ref Brep feature, in bool mirrorYZ = false, in bool _mirrorXZ = false, in bool mirrorXY = false)
        {
            BlockKeyedCollection blocks = new BlockKeyedCollection();
            LayerKeyedCollection layers = new LayerKeyedCollection();
            Point3D[] bboxPoints = feature.EstimateBoundingBox(blocks, layers);
            double xMin = bboxPoints.Min(p => p.X), xMax = bboxPoints.Max(p => p.X),
                yMin = bboxPoints.Min(p => p.Y), yMax = bboxPoints.Max(p => p.Y),
                zMin = bboxPoints.Min(p => p.Z), zMax = bboxPoints.Max(p => p.Z);
            double xLength = xMax - xMin;
            double yLength = yMax - yMin;
            double zLength = zMax - zMin;

            bool mirrorXZ = _mirrorXZ;

            bool horizontalPlane = side == "C" || side == "D" || side == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = side == "A" || side == "B" && wp.Prf.CodePrf != "L";

            if (mirrorXZ && side == "B")
            {
                if (wp.Prf.CodePrf == "I" || wp.Prf.CodePrf == "U" || wp.Prf.CodePrf == "Q")
                    mirrorXZ = false;
                else if (wp.Prf.CodePrf == "L")
                    mirrorXZ = false;
                    //return true;
            }

            double zReference = 0;

            if (wp.Prf.CodePrf == "I")
                zReference = wp.Prf.SB / 2;

            //
            //  Mirror X -> LP -X
            //
            if (mirrorYZ)
            {
                double offsXMirrorPlane = 1;
                double xMirrorPlane = xMin - offsXMirrorPlane;

                Plane cutPlane = Plane.YZ;
                cutPlane.Translate (xMirrorPlane, 0);
                // Mirror the solid created before wrt the YZ plane
                Brep[] mirror = Brep.Mirror(cutPlane, feature);

                if (mirror.Count() > 1)
                {
                    //  Translate the solid on the final side  
                    mirror[1].Translate(wp.Lp - 2 * xMirrorPlane, 0);
                    feature = mirror[1];
                }
                else
                {
                    //
                    //  QUI NON DOVREBBE MAI ENTRARE
                    // Cut the generated solid wrt the YZ plane
                    mirror[0].CutBy(cutPlane, false);
                    // Translate the solid on the final side  
                    mirror[0].Translate(wp.Lp - 2 * xMin, 0);
                    feature = mirror[0];
                }
            }

            //
            //  Mirror Y -> SA - Y
            //
            if (mirrorXZ || mirrorXY)
            {
                //  Offset Z del piano di mirroring per creare 2 entità distinte
                double offsZMirrorPlane = 1;
                double zMirrorPlane = zMin - offsZMirrorPlane;
                double offsYMirrorPlane = 1;
                double yMirrorPlane = yMin - offsYMirrorPlane;

                Plane mirrorPlane = side == "C" ? Plane.XZ : Plane.XY;
                if (side == "C")
                    mirrorPlane.Translate(0, yMirrorPlane);
                else
                    mirrorPlane.Translate(0, 0, zMirrorPlane);

                // Mirror the solid created before wrt the cutPlane
                Brep[] mirror = Brep.Mirror(mirrorPlane, feature);

                if (mirror.Count() > 1)
                {
                    // Translate the solid on the final side  
                    if (side == "C")
                        mirror[1].Translate(0, wp.Prf.SA - 2 * yMirrorPlane);
                    else
                    {
                        double z = -2 * zMirrorPlane;

                        if (wp.Prf.CodePrf == "U" || wp.Prf.CodePrf == "Q")
                            z += wp.Prf.SB;

                        mirror[1].Translate(0, 0, z);
                    }
                        
                    feature = mirror[1];
                }
                else
                {
                    //
                    //  QUI NON DOVREBBE MAI ENTRARE
                    // Cut the generated solid wrt the XZ plane
                    var a = mirror[0].CutBy(mirrorPlane, side == "C");
                    //var a = mirror[0].CutBy(cutPlane, true);
                    // Translate the solid on the final side  
                    if (side == "C")
                        mirror[0].Translate(0, wp.Prf.SA - 2 * yMin);
                    else
                        mirror[0].Translate(0, 0, - 2 * zMirrorPlane);
                    feature = mirror[0];
                }
            }

            if (side != "C" && wp.Prf.CodePrf == "I")
                feature.Translate(0, 0, zReference);

            return true;
        }

        // Given a list of curves, a workpiece, a plane (A, B or C), a boolean variable true if the solid is on the initial side. It returns the solid with the subtracted feature on the side indicated and the subtracted feature.
        // the list of curves are extruded based on the thickness of the side indicated
        public static void SolidSubtract(in List<ICurve> curveList, in IWorkPiece wp, in string extrusionPlane, in double brepTolerance, ref Brep feature, in bool mirrorYZ = false, in bool mirrorXZ = false, in bool mirrorXY = false, in bool arcSharingSegment = false)
        {
            if (curveList.Count == 0 || wp == null || extrusionPlane == null)
                throw new ArgumentException("One or more arguments are invalid");

            CompositeCurve cc = null;
            devDept.Eyeshot.Entities.Region region = null;

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            if (horizontalPlane)
            {
                cc = new CompositeCurve(curveList);
                region = new Region(cc);
                double height = wp.Prf.TA + wp.Prf.Radius * 2;
                Vector3D amount = new Vector3D(0, 0, height);
                feature = region.ExtrudeAsBrep(amount, 0, brepTolerance);
                feature.Translate(0, 0, wp.Prf.SB / 2 - height / 2);
                MirrorBrep(wp, extrusionPlane, ref feature, mirrorYZ, mirrorXZ, mirrorXY);
                return;
            }
            else if (verticalPlane)
            {
                List<ICurve> curvesOnPlaneXZ = ProjectCurvesOnPlane(curveList, Plane.XZ, arcSharingSegment);
                cc = new CompositeCurve(curvesOnPlaneXZ);
                region = new Region(cc, Plane.XZ);
                double thickness = wp.Prf.TB + wp.Prf.Radius + 1;
                Vector3D amount = new Vector3D(0, thickness, 0);
                feature = region.ExtrudeAsBrep(amount, 0, brepTolerance);
                feature.Translate(0, -(wp.Prf.Radius + 1) / 2);
                MirrorBrep(wp, extrusionPlane, ref feature, mirrorYZ, mirrorXZ, mirrorXY);
                return;
            }
        }

        // Given a list of curves, a workpiece, a plane (A, B or C), a boolean variable true if the solid is on the initial side, a vector 3d indicating the amount of extrusion and the direction of extrusion. It returns the solid with the subtracted feature on the side indicated and the subtracted feature.
        // the centre of the subtracted solid is on the centre of the indicated plane i.e. plane thickness / 2.
        // N.B. when using this function is concerning of the user extruding more than the plane thickness
        public static void SolidSubtract(in List<ICurve> curveList, in IWorkPiece wp, in string extrusionPlane, in Vector3D amount, 
            in double offsetX, in double offsetY, in double offsetZ, in double brepTolerance, ref Brep feature, in bool mirrorYZ = false, in bool mirrorXZ = false, in bool mirrorXY = false, in bool arcSharingSegment = false)
        {
            if (curveList.Count == 0 || wp == null || extrusionPlane == null || amount == null)
                throw new ArgumentException("One or more arguments are invalid");

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            CompositeCurve cc = null;
            devDept.Eyeshot.Entities.Region region = null;
            if (horizontalPlane)
            {
                cc = new CompositeCurve(curveList);
                region = new Region(cc);
                feature = region.ExtrudeAsBrep(amount, 0, brepTolerance);
                feature.Translate(offsetX, offsetY, offsetZ);
                MirrorBrep(wp, extrusionPlane, ref feature, mirrorYZ, mirrorXZ, mirrorXY);
                return;
            }
            else if (verticalPlane)
            {
                List<ICurve> curvesOnPlaneXZ = ProjectCurvesOnPlane(curveList, Plane.XZ, arcSharingSegment);
                cc = new CompositeCurve(curvesOnPlaneXZ);
                region = new Region(cc, Plane.XZ);
                feature = region.ExtrudeAsBrep(amount, 0, brepTolerance);
                feature.Translate(offsetX, offsetY, offsetZ);
                MirrorBrep(wp, extrusionPlane, ref feature, mirrorYZ, mirrorXZ, mirrorXY);
                return;
            }
        }

        public static void CreateRadialSlotAperture(in double totalLenght, in double positionAngle, in double arcRadius, in double cylinderInnerRadius, in double cylinderOuterRadius, in double brepTolerance, in bool mirrorXY, out List<Brep> features, in double xTraslation = 0)
        {
            double angler = positionAngle * Math.PI / 180;
            features = new List<Brep>();
            // Used to create orthogonal cuting on the straight sides of the slot
            //CreateRadialAperture(radius * 2, totalLenght - 2 * radius, angle, cylinderInnerRadius, cylinderOuterRadius, mirrorXY, out features, xTraslation, 0, false, false, true);
            Region r = Region.CreateSlot(totalLenght - 2 * arcRadius, arcRadius - 0.001);
            Brep slot = r.ExtrudeAsBrep(cylinderOuterRadius * 3, 0, brepTolerance);
            slot.Rotate(angler, Vector3D.AxisX);
            slot.Translate(xTraslation, cylinderOuterRadius, cylinderOuterRadius);
            features.Add(slot);
        }

        //Create the radial aperture starting from the center, the angle is illustred in G1F137_R.BMP. N.B. the lenght is the total lenght of the feature in x direction
        public static void CreateRadialAperture(in double width, in double totalLenght, double positionAngle, in double cylinderInnerRadius, in double cylinderOuterRadius, in double brepTolerance, in bool mirrorXY, out List<Brep> features, in double filletRadius = 0, in double xTraslation = 0, in double surplus = 0, in bool filletInitialEdgesOnly = false, in bool filletFinalEdgesOnly = false, bool withoutFillet = false)
        {
            if (filletRadius * 2 == width)
            { 
                CreateRadialSlotAperture(totalLenght, positionAngle, filletRadius, cylinderInnerRadius, cylinderOuterRadius, brepTolerance, mirrorXY, out features, xTraslation);
                return;
            }

            Plane mirrorPlane = null;
            Brep mirroredBrep = null, feature;
            features = new List<Brep>();
            double angler = positionAngle * Math.PI / 180;
            // Vertices of the triangle

            // Angle of the half-right triangle 
            double beta = FmathExt.Acos(width / (2 * cylinderInnerRadius));
            // Height of the horizontal line composing the triangle 
            double h = cylinderInnerRadius * Math.Sin(beta);

            Point3D centre = new Point3D(-surplus, 0, 0);
            Point3D rightVer = new Point3D(-surplus, -width / 2, h);
            Point3D leftVer = new Point3D(-surplus, width / 2, h);
            // twice the lenght of the sides touching the center to go out of the cylinder edges
            rightVer *= 2;
            leftVer *= 2;

            // Lines composing the triangle
            devDept.Eyeshot.Entities.Line lRight = new devDept.Eyeshot.Entities.Line(centre, rightVer);
            devDept.Eyeshot.Entities.Line lHorizontal = new devDept.Eyeshot.Entities.Line(rightVer, leftVer);
            devDept.Eyeshot.Entities.Line lLeft = new devDept.Eyeshot.Entities.Line(leftVer, centre);
            // Brep creation 
            CompositeCurve cc = new CompositeCurve(lRight, lLeft, lHorizontal);
            cc.Reverse();
            Region region = new Region(cc);
            // Parametro tolleranza messo a 0.01
            feature = region.ExtrudeAsBrep(totalLenght + surplus, 0, brepTolerance);
            // Plane used to cut the tip of the solid, needed to use fillet function
            Plane plane = Plane.XY;
            plane.Translate(0, 0, h - surplus * 2);
            feature.CutBy(plane, true);
            // Fillet the edges required
            if (!withoutFillet)
            {
                List<Brep.Edge> edges = GetNonHorizontalOrVerticalEdges(feature);
                if (filletInitialEdgesOnly)
                    edges = edges.Where(e => e.Curve.StartPoint.X < 0.5).ToList();
                else if (filletFinalEdgesOnly)
                    edges = edges.Where(e => e.Curve.StartPoint.X > 0.5).ToList();
                foreach (Brep.Edge edge in edges)
                    feature.Fillet(edge, filletRadius);
            }
            // Rotate the feature by the required angle
            feature.Rotate(angler, Vector3D.AxisX);

            // Mirror the feature wrt xy plane to obtain the feature at the bottom of the part
            if (mirrorXY)
            {
                mirrorPlane = Plane.XY;
                mirrorPlane.Rotate(angler, Vector3D.AxisX);
                mirroredBrep = Brep.Mirror(mirrorPlane, feature)[1];
                mirroredBrep.Translate(xTraslation, cylinderOuterRadius, cylinderOuterRadius);
                features.Add(mirroredBrep);
            }

            // Translate the feature at the center of the cylinder
            feature.Translate(xTraslation, cylinderOuterRadius, cylinderOuterRadius);
            features.Add(feature);
        }

        /// <summary>
        /// Cuts flange scrap solids to separate the portion affecting the flange from the web fillet area.
        /// </summary>
        public static IEnumerable<(Brep solid, Brep? flangeWeb)> ProcessFlangeScraps(List<Brep> scraps, string plane, bool onlyFilletSolid, double lp, double _yU, double _yO, double _zUp, double _zDown, double flange_surplus, double flange_web_clearance, double flange_x_translation, double web_x_translation_flange, double tolerance)
        {
            // Guardo se c'è da convertire la stringa del piano 
            if (plane.ToUpper() == "A")
                plane = "u";
            else if (plane.ToUpper() == "B")
                plane = "o";

            var processedScraps = new List<(Brep solid, Brep? flangeWeb)>();
            // serve aumentare perchè il solido che va sottratto all'ala deve eccedere l'ala
            double verticalCutY = plane == "u"
                ? _yU + flange_surplus
                : _yO - flange_surplus;

            double zUp = _zUp + flange_web_clearance,
                   zDown = _zDown - flange_web_clearance;

            var verticalCuttingPlane = new Plane(new Point3D(0, verticalCutY, 0), Vector3D.AxisY);
            var upperHorizontalPlane = new Plane(new Point3D(0, 0, zUp), Vector3D.AxisZ);
            var lowerHorizontalPlane = new Plane(new Point3D(0, 0, zDown), Vector3D.AxisZ);

            foreach (var scrap in scraps)
            {
                var flangeSolid = (Brep)scrap.Clone();
                var webFilletUpper = (Brep)scrap.Clone();

                // 1. Isolate the main flange cutting solid.
                flangeSolid.CutBy(verticalCuttingPlane, plane == "o");

                // Assicurarsi che esista il flange web, altrimenti non lo creo
                // Esiste se il solido della flangia ha una parte che si estende sopra e sotto il web
                Brep flangeWeb = null;
                if (flangeSolid.BoxMin == null) flangeSolid.Regen(0.1);
                if (flangeSolid.BoxMin.Z < _zDown && flangeSolid.BoxMax.Z > _zUp)
                {
                    flangeWeb = (Brep)flangeSolid.Clone();
                    flangeWeb.CutBy(upperHorizontalPlane, false); // Cut the upper part of the flange solid
                    flangeWeb.CutBy(lowerHorizontalPlane, true); // Cut the lower part of the flange solid
                }

                // Translate pieces at the beam ends to ensure clean boolean operations.
                TranslateEndPiece(flangeSolid, flange_x_translation, lp);

               if (!onlyFilletSolid)
                    processedScraps.Add((flangeSolid, flangeWeb));

                // N.B. Anche se il profilo non è I va sottratto comunque perchè migliora la funzione e fa so che riescano più sottrazioni
                var webFilletLower = (Brep)scrap.Clone();
                // Assicurarsi che esista il webFilletLower, altrimenti non lo creo
                if (webFilletLower.BoxMin == null) webFilletLower.Regen(0.1);
                if (webFilletLower.BoxMin.Z.IsLessThan(_zDown, tolerance))
                {
                    if (webFilletLower.BoxMax.Z.IsGreaterThan(_zDown, tolerance))
                        ;//webFilletLower.CutBy(verticalCuttingPlane, plane != "o"); // Cut the lower part of the web fillet solid
                    else
                    {
                        //webFilletLower.CutBy(verticalCuttingPlane, plane != "o"); // Cut the lower part of the web fillet solid
                        webFilletLower.Translate(0, 0, -flange_web_clearance);
                        flangeSolid.Translate(0, 0, -flange_web_clearance);
                    }

                    // 3. Isolate the part that cuts below the web.
                    webFilletLower.CutBy(lowerHorizontalPlane, false);

                    // Translate the lower web fillet piece to ensure clean boolean operations.
                    TranslateEndPiece(webFilletLower, web_x_translation_flange, lp);

                    processedScraps.Add((webFilletLower, null));
                }

                // Assicurarsi che esista il webFilletUpper, altrimenti non lo creo
                if (webFilletUpper.BoxMin == null) webFilletUpper.Regen(0.1);
                if (webFilletUpper.BoxMax.Z.IsGreaterThan(_zUp, tolerance))
                {
                    //webFilletUpper.CutBy(verticalCuttingPlane, plane == "u"); // Cut the upper part of the web fillet solid
                    if (!webFilletUpper.BoxMin.Z.IsLessThan(_zUp, tolerance))
                    {
                        webFilletUpper.Translate(0, 0, flange_web_clearance);
                        flangeSolid.Translate(0, 0, -flange_web_clearance);
                    }
                    else
                        // 2. Isolate the part that cuts above the web.
                        webFilletUpper.CutBy(upperHorizontalPlane, true);

                    // Translate pieces at the beam ends to ensure clean boolean operations.
                    TranslateEndPiece(webFilletUpper, web_x_translation_flange, lp);

                    // Add the processed upper web fillet solid.
                    processedScraps.Add((webFilletUpper, null));
                }
            }

            return processedScraps;
        }


        public static IEnumerable<Brep> ProcessWebScraps(List<Brep> scraps, double _zUp, double _zDown, double prfRadius, double tb, double lp, double web_x_translation)
        {
            var processedScraps = new List<Brep>();
            foreach (var scrap in scraps)
            {
                    var webFilletUpper = (Brep)scrap.Clone();
                    var webFilletLower = (Brep)scrap.Clone();

                    var upperHorizontalPlane = new Plane(new Point3D(0, 0, _zUp-0.011), Vector3D.AxisZ);
                    var lowerHorizontalPlane = new Plane(new Point3D(0, 0, _zDown + 0.011), Vector3D.AxisZ);

                    var verticalPlane = Plane.XZ;
                    verticalPlane.Translate(0, prfRadius + tb);
                    // 2. Isolate the part that cuts above the web.
                    webFilletUpper.CutBy(upperHorizontalPlane, true);
                    //webFilletUpper.CutBy(verticalPlane, true); // Ensure the fillet does not extend into the flange area
                                                               // 3. Isolate the part that cuts below the web.
                    webFilletLower.CutBy(lowerHorizontalPlane, false);

                    EyeUtils.TranslateEndPiece(webFilletUpper, web_x_translation, lp);
                    EyeUtils.TranslateEndPiece(webFilletLower, web_x_translation, lp);

                    processedScraps.Add(webFilletUpper);
                    processedScraps.Add(webFilletLower);
            }

           return processedScraps;
        }

        /// <summary>
        /// Helper to translate a Brep if it's located at the start or end of the workpiece.
        /// </summary>
        public static void TranslateEndPiece(Brep piece, double translationAmount, double lp)
        {
            if (piece.BoxMin == null) piece.Regen(0.1);
            if (piece.BoxMin.X <= 0)
            {
                piece.Translate(-translationAmount, 0);
            }
            else if (piece.BoxMax.X >= lp)
            {
                piece.Translate(translationAmount, 0);
            }
        }

        public static List<Brep.Edge> GetNonHorizontalOrVerticalEdges(Brep brep)
        {
            List<Brep.Edge> nonHorizontalOrVerticalEdges = new List<Brep.Edge>();

            foreach (Brep.Edge edge in brep.Edges)
            {
                // Get the start and end vertices of the edge
                Point3D startPoint = edge.Curve.StartPoint;
                Point3D endPoint = edge.Curve.EndPoint;

                // Check if the edge is neither horizontal nor vertical
                if (!IsHorizontalEdge(startPoint, endPoint, 1.5) && !IsVerticalEdge(startPoint, endPoint, 1.5))
                {
                    nonHorizontalOrVerticalEdges.Add(edge);
                }
            }

            return nonHorizontalOrVerticalEdges;
        }

        public static bool IsHorizontalEdge(Point3D startPoint, Point3D endPoint, double tolerance)
        {
            // Compare the Y-coordinates of the start and end points
            // to determine if the edge is horizontal within a tolerance
            if (
                startPoint.X.IsEqualTo(endPoint.X, tolerance) &&
                startPoint.Z.IsEqualTo(endPoint.Z, tolerance)
               )
                return true;
            else if (
                     startPoint.Y.IsEqualTo(endPoint.Y, tolerance) &&
                     startPoint.Z.IsEqualTo(endPoint.Z, tolerance)
                    )
                return true;

            return false;
        }

        public static bool IsVerticalEdge(Point3D startPoint, Point3D endPoint, double tolerance)
        {
            // Compare the X-coordinates of the start and end points
            // to determine if the edge is vertical within a tolerance
            if (
                startPoint.X.IsEqualTo(endPoint.X, tolerance) &&
                startPoint.Y.IsEqualTo(endPoint.Y, tolerance)
               )
                return true;
           
            return false;
        }

        // Given two composite curves reorder them such that they'll share the starting point
        public static bool ReorderCompositeCurves(CompositeCurve c1, CompositeCurve c2)
        {
            int curveIndex = -1;
            List<double> distanceFromStartPoint = new List<double>();
            List<double> distanceFromStartPoint2 = new List<double>();
            
            for (int i = 0; i < c2.CurveList.Count; i++)
            {
                ICurve currentCurve = c2.CurveList[i];

                distanceFromStartPoint.Add(currentCurve.StartPoint.DistanceTo(c1.StartPoint));
                //distanceFromStartPoint2.Add(currentCurve.EndPoint.DistanceTo(c1.StartPoint));
            }

            curveIndex = distanceFromStartPoint.IndexOf(distanceFromStartPoint.Min());
            //curveIndex2 = distanceFromStartPoint2.IndexOf(distanceFromStartPoint2.Min());
            if (c1.CurveList[0].StartTangent == c2.CurveList[curveIndex].StartTangent)
            {
                for (int i = 0; i < curveIndex; i++)
                {
                    Utility.RotateLeft(c2.CurveList);
                }
            }
            else
            {
                for (int i = 0; i < curveIndex; i++)
                {
                    Utility.RotateRight(c2.CurveList);
                }
            }

            return true;
        }

        public static bool ComputeVectors(in Brep workedPiece, in IWorkPiece wp, in double distanceTol, double arcSegmentLength, bool skipUselessFaces, ref List<List<EyeCuttingEdge>> faceCuttingEdgesList)
        {
            for (int i = 0; i < workedPiece.Faces.Count(); i++)
            {
                var face = workedPiece.Faces[i];

                if (skipUselessFaces)
                {
                    // If the point at the face center is not inside the workpiece profile region skip that face
                    if (!IsFacePointInsideProfileRegion(i, wp, workedPiece, distanceTol))
                        continue;
                }

                // If the face is a chamfer compute its type internal or external and assign it to each edge 
                IsChamferFace(workedPiece, wp, face, out EyeCuttingEdge.ChamferType chamferType);

                // The elements in the list have the same shared face
                List<EyeCuttingEdge> faceCuttingEdges = new List<EyeCuttingEdge>();
                foreach (var loop in face.Loops)
                {
                    // Extract the oriented edges defining the face
                    var orientedEdges = loop.Segments.Select(x => x).Reverse().ToList();

                    orientedEdges = FilterOrientedEdges(orientedEdges, workedPiece, wp);
                    EyeCuttingEdge eyeCuttingEdge = null;

                    foreach (var edge in orientedEdges)
                    {
                        // Extract the edge from the brep solid 
                        Brep.Edge e = workedPiece.Edges[edge.CurveIndex];

                        // Extract the edge's face parents
                        
                        // If the edge is not shared between two faces skip it
                         if (e.Parents.Length != 2)
                            continue;
                         // Skip the small edges
                         if (e.Curve.Length() < 1)
                            continue;

                        Brep.Face f1 = workedPiece.Faces[e.Parents[0]];
                        Brep.Face f2 = workedPiece.Faces[e.Parents[1]];

                        // Check which is the face adjacent to the face shared between the intersection brep and final part
                        Brep.Face adjacent = null;
                        int faceIndex = 0, adjacentIndex =0;

                        if (!AreFacesEqual(f1, face))
                        {
                            faceIndex = e.Parents[1];
                            adjacentIndex = e.Parents[0];

                            adjacent = f1;
                        }
                        else if (!AreFacesEqual(f2, face))
                        {
                            faceIndex = e.Parents[0];
                            adjacentIndex = e.Parents[1];

                            adjacent = f2;
                        }
                        else
                            continue;

                        (List<devDept.Eyeshot.Entities.Line> normalLinesSharedFace, List<devDept.Eyeshot.Entities.Line> normalLinesAdjacentFace) lines;

                        double step;
                        if (e.Curve.IsLinear(0.01, out Segment3D line))
                        {
                            step = e.Curve.Length();
                        }
                        else
                            // Compute a cutting line each arcSegmentLength mm
                             step = arcSegmentLength > 0 ? arcSegmentLength : 2;

                        // 
                        // Se l'edge è su un bordo del profilo o dentro la zona raggio del profilo allora calcolo la normale alla faccia
                        //
                        bool isEdgeOnProfileBoundary = !IsEdgeInsideRegion(edge.CurveIndex, wp, workedPiece, distanceTol),
                             isEdgeInsideProfileRadiusRegion = IsEdgeInsideProfileRadius(edge.CurveIndex, wp, workedPiece, distanceTol);

                        if (isEdgeOnProfileBoundary || isEdgeInsideProfileRadiusRegion)
                        {
                            lines = NormalToFaceEdge(face, adjacent, e.Curve, step);
                        }
                        else
                        {
                            // It could be an edge belonging to a chamfer 
                            var faceNormal = face.Normal(e.Curve.EndPoint);
                            var adjacentNormal = adjacent.Normal(e.Curve.EndPoint);
                            bool isOnWeb = e.Curve.StartPoint.Y < wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius && e.Curve.EndPoint.Y > wp.Prf.TB + wp.Prf.Radius ||
                                           e.Curve.EndPoint.Y < wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius && e.Curve.StartPoint.Y > wp.Prf.TB + wp.Prf.Radius,
                                isHorizontal = e.Curve.StartPoint.Z.IsEqualTo(e.Curve.EndPoint.Z, 0.01) && e.Curve.StartPoint.X.IsEqualTo(e.Curve.EndPoint.X, 0.01),
                                isVertical = e.Curve.StartPoint.Y.IsEqualTo(e.Curve.EndPoint.Y, 0.01) && e.Curve.StartPoint.X.IsEqualTo(e.Curve.EndPoint.X, 0.01),
                                // Check if at aleat a shared face is inclined
                                inclinedFace = (isOnWeb && (!faceNormal.AngleFromXY.IsEqualTo(Math.PI / 2, 0.017) && !faceNormal.AngleFromXY.IsEqualTo(0, 0.017) || !adjacentNormal.AngleFromXY.IsEqualTo(Math.PI / 2, 0.017) && !adjacentNormal.AngleFromXY.IsEqualTo(0, 0.017)))||
                                !isOnWeb && (!adjacentNormal.AngleInXY.IsEqualTo(Math.PI / 2, 0.017) && !adjacentNormal.AngleInXY.IsEqualTo(0, 0.017));



                            if (isOnWeb && isHorizontal && inclinedFace || !isOnWeb && isVertical && inclinedFace)
                            {

                                lines = NormalToFaceEdge(face, adjacent, e.Curve, step);
                            }
                            else
                                lines = (null, null);
                        }
                            eyeCuttingEdge = new EyeCuttingEdge(edge.CurveIndex, lines.normalLinesSharedFace, lines.normalLinesAdjacentFace, faceIndex, adjacentIndex, chamferType);

                            if (!(eyeCuttingEdge is null))
                                faceCuttingEdges.Add(eyeCuttingEdge);
                    }
                }
                faceCuttingEdgesList.Add(faceCuttingEdges);
            }

            foreach (var CuttingEdges in faceCuttingEdgesList)
            {
                List<devDept.Eyeshot.Entities.Line> cuttingEdgeVectors = new List<devDept.Eyeshot.Entities.Line>();
                foreach (var cuttingEdge in CuttingEdges)
                {
                    if (cuttingEdge.NormalLinesAdjacentFace is null)
                        continue;

                    for (int i = 0; i < cuttingEdge.NormalLinesWorkedFace.Count; i++)
                    {
                        devDept.Eyeshot.Entities.Line l = ComputeCuttingLine(cuttingEdge.NormalLinesWorkedFace[i], cuttingEdge.NormalLinesAdjacentFace[i], workedPiece);
                        if (l != null)
                            cuttingEdgeVectors.Add(l);
                    }

                }
            }

            return true;
        }

        // Compute the face type of chamfer
        public static void IsChamferFace(Brep brep, IWorkPiece wp, Brep.Face face, out EyeCuttingEdge.ChamferType chamferType)
        {
            chamferType = EyeCuttingEdge.ChamferType.None;
            var param = face.Parametric[0];
            var curves = face.Loops[0].Segments.Select(x => brep.Edges[x.CurveIndex].Curve).ToList();

            if (param == null)
                return;

            bool allSideA = true,
                 allSideB = true,
                 allSideC = true;

            foreach (var curve in curves)
            {
                //bool isSideA = (curve.StartPoint.Y.IsEqualTo(wp.Prf.TB + wp.Prf.Radius, 0.01) || curve.StartPoint.Y.IsLessThan(wp.Prf.TB + wp.Prf.Radius, 0.01)) &&
                //               (curve.EndPoint.Y.IsEqualTo(wp.Prf.TB + wp.Prf.Radius, 0.01) || curve.EndPoint.Y.IsLessThan(wp.Prf.TB + wp.Prf.Radius, 0.01));
                //bool isSideB = (curve.StartPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius, 0.01) || curve.StartPoint.Y.IsGreaterThan(wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius, 0.01)) &&
                //               (curve.EndPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius, 0.01) || curve.EndPoint.Y.IsGreaterThan(wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius, 0.01));
                bool isSideA = (curve.StartPoint.Y.IsEqualTo(wp.Prf.TB, 0.01) || curve.StartPoint.Y.IsLessThan(wp.Prf.TB, 0.01)) &&
                               (curve.EndPoint.Y.IsEqualTo(wp.Prf.TB, 0.01) || curve.EndPoint.Y.IsLessThan(wp.Prf.TB, 0.01));
                bool isSideB = (curve.StartPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB, 0.01) || curve.StartPoint.Y.IsGreaterThan(wp.Prf.SA - wp.Prf.TB, 0.01)) &&
                               (curve.EndPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB, 0.01) || curve.EndPoint.Y.IsGreaterThan(wp.Prf.SA - wp.Prf.TB, 0.01));
                bool isSideC = wp.Prf.CodePrf == "I" ? (curve.EndPoint.Z.IsEqualTo(wp.Prf.SB / 2 - wp.Prf.TA, 0.01) || curve.EndPoint.Z.IsGreaterThan(wp.Prf.SB / 2 - wp.Prf.TA, 0.01)) || (curve.StartPoint.Z.IsEqualTo(wp.Prf.SB / 2 - wp.Prf.TA, 0.01) || curve.StartPoint.Z.IsGreaterThan(wp.Prf.SB / 2 - wp.Prf.TA, 0.01)) &&
                                                       (curve.EndPoint.Z.IsEqualTo(wp.Prf.SB / 2 + wp.Prf.TA, 0.01) || curve.EndPoint.Z.IsLessThan(wp.Prf.SB / 2 + wp.Prf.TA, 0.01)) || (curve.StartPoint.Z.IsEqualTo(wp.Prf.SB / 2 + wp.Prf.TA, 0.01) || curve.StartPoint.Z.IsLessThan(wp.Prf.SB / 2 + wp.Prf.TA, 0.01)) :
                               wp.Prf.CodePrf == "Q" ? (curve.EndPoint.Z.IsEqualTo(wp.Prf.SB, 0.01) || curve.EndPoint.Z.IsGreaterThan(wp.Prf.SB, 0.01)) || (curve.StartPoint.Z.IsEqualTo(wp.Prf.SB, 0.01) || curve.StartPoint.Z.IsGreaterThan(wp.Prf.SB, 0.01)) :
                                                       (curve.EndPoint.Z.IsEqualTo(wp.Prf.TA, 0.01) || curve.EndPoint.Z.IsLessThan(wp.Prf.TA, 0.01)) || (curve.StartPoint.Z.IsEqualTo(wp.Prf.TA, 0.01) || curve.StartPoint.Z.IsLessThan(wp.Prf.TA, 0.01));

                if (!isSideA)
                    allSideA = false;
                if (!isSideB)
                    allSideB = false;
                if (!isSideC)
                    allSideC = false;
            }

            if (allSideA || allSideB)
            {
                Vector3D n = face.Parametric[0].NormalAt(0.5, 0.5);

                // Face normal is on Y axis or in the XZ plane
                if (Math.Abs(n.Y).IsEqualTo(1, 0.01) || Math.Abs(n.Y).IsEqualTo(0, 0.01))
                    return;
                else if (allSideA && n.Y.IsGreaterThan(0, 0.1))
                    chamferType = EyeCuttingEdge.ChamferType.Internal;
                else if (allSideA && n.Y.IsLessThan(0, 0.1))
                    chamferType = EyeCuttingEdge.ChamferType.External;
                else if (allSideB && n.Y.IsGreaterThan(0, 0.1))
                    chamferType = EyeCuttingEdge.ChamferType.External;
                else if (allSideB && n.Y.IsLessThan(0, 0.1))
                    chamferType = EyeCuttingEdge.ChamferType.Internal;
                else
                    chamferType = EyeCuttingEdge.ChamferType.None;
            }
            else if (allSideC)
            {
                Vector3D n = face.Parametric[0].NormalAt(0.5, 0.5);
                // Face normal is on Z axis or in the XY plane
                if (Math.Abs(n.Z).IsEqualTo(1, 0.01) || Math.Abs(n.Z).IsEqualTo(0, 0.01))
                    return;
                else if (n.Z.IsGreaterThan(0, 0.1))
                    chamferType = EyeCuttingEdge.ChamferType.External;
                else if (n.Z.IsLessThan(0, 0.1))
                    chamferType = EyeCuttingEdge.ChamferType.Internal;
                else
                    chamferType = EyeCuttingEdge.ChamferType.None;
            }
        }


        // Skip the parallel edges with very small distance on Y = 0, Y = SA, Z = webZdown and Z = webUp
        // Funzione che filtra gli oriented edges in modo da non avere più edges paralleli con distanza molto piccola, nata per 
        // i cianfrini
        private static List<Brep.OrientedEdge> FilterOrientedEdges(List<Brep.OrientedEdge> orientedEdges, Brep brep, IWorkPiece wp)
        {
            var oldMethod = false;

            if (oldMethod)
            {
                List<Brep.OrientedEdge> filteredEdges = new List<Brep.OrientedEdge>();

                if (wp.Prf.CodePrf == "I" || wp.Prf.CodePrf == "U" || wp.Prf.CodePrf == "L" || wp.Prf.CodePrf == "F")
                {
                    for (int i = 1; i <= orientedEdges.Count; i++)
                    {
                        var edge1 = brep.Edges[orientedEdges[i - 1].CurveIndex].Curve;

                        if (!(edge1 is Line line1))
                            filteredEdges.Add(orientedEdges[i - 1]);
                        else
                        {
                            var direction1 = line1.Direction;
                            direction1.Normalize();
                            bool skipEdge = false;

                            // Comparo l'edge precedente con tutti gli altri per capire se devo skipparlo o no
                            for (int j = 0; j < orientedEdges.Count; j++)
                            {
                                if (i - 1 == j)
                                    continue;

                                var edge2 = brep.Edges[orientedEdges[j].CurveIndex].Curve;
                                if (!(edge2 is Line line2))
                                    continue;

                                var direction2 = line2.Direction;
                                direction2.Normalize();
                                bool areParallel = direction1.X.IsEqualTo(direction2.X, 0.01) && direction1.Y.IsEqualTo(direction2.Y, 0.01) && direction1.Z.IsEqualTo(direction2.Z, 0.01);
                                bool areNear = line1.StartPoint.Y.IsEqualTo(line2.StartPoint.Y, 0.1);
                                bool areEqual = line1.StartPoint.Y.IsEqualTo(line2.StartPoint.Y, 0.001);

                                if (areParallel && areNear && !areEqual)
                                {
                                    if (wp.Prf.CodePrf == "I")
                                    {
                                        double tol = 0.1;
                                        //Y: 0, ta, ha-tb, ha
                                        //Z: ha/2 -tc/2, ha/2+tc/2
                                        if (line1.StartPoint.Y.IsEqualTo(0, tol) && line1.EndPoint.Y.IsEqualTo(0, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Y.IsEqualTo(wp.Prf.TB, tol) && line1.EndPoint.Y.IsEqualTo(wp.Prf.TB, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Y.IsEqualTo(wp.Prf.SA, tol) && line1.EndPoint.Y.IsEqualTo(wp.Prf.SA, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB, tol) && line1.EndPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Z.IsEqualTo(wp.Prf.SB / 2 - wp.Prf.TA / 2, tol) && line1.EndPoint.Z.IsEqualTo(wp.Prf.SB / 2 - wp.Prf.TA / 2, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Z.IsEqualTo(wp.Prf.SB / 2 + wp.Prf.TA / 2, tol) && line1.EndPoint.Z.IsEqualTo(wp.Prf.SB / 2 + wp.Prf.TA / 2, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                    }
                                    else if (wp.Prf.CodePrf == "U" || wp.Prf.CodePrf == "L" || wp.Prf.CodePrf == "F")
                                    {
                                        double tol = 0.1;
                                        //Y: 0, ta, ha-tb, ha
                                        //Z: ha/2 -tc/2, ha/2+tc/2
                                        if (line1.StartPoint.Y.IsEqualTo(0, tol) && line1.EndPoint.Y.IsEqualTo(0, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Y.IsEqualTo(wp.Prf.TB, tol) && line1.EndPoint.Y.IsEqualTo(wp.Prf.TB, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Y.IsEqualTo(wp.Prf.SA, tol) && line1.EndPoint.Y.IsEqualTo(wp.Prf.SA, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB, tol) && line1.EndPoint.Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Z.IsEqualTo(0, tol) && line1.EndPoint.Z.IsEqualTo(0, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                        else if (line1.StartPoint.Z.IsEqualTo(wp.Prf.TA / 2, tol) && line1.EndPoint.Z.IsEqualTo(wp.Prf.TA / 2, tol))
                                        {
                                            // l'edge appartiene a una faccia che non deve essere considerata
                                            skipEdge = true;
                                        }
                                    }
                                }
                            }

                            if (!skipEdge)
                                filteredEdges.Add(orientedEdges[i - 1]);
                        }

                    }
                }
                else
                    filteredEdges = orientedEdges.ToList();

                return filteredEdges;
            }
            else
            {
                List<Brep.OrientedEdge> filteredEdges = new List<Brep.OrientedEdge>();
                double parallelTol = 0.01;
                double offsetTol = 0.1;
                double overlapTol = 0.1;

                bool IsParallel(Vector3D d1, Vector3D d2) =>
                    d1.X.IsEqualTo(d2.X, parallelTol) &&
                    d1.Y.IsEqualTo(d2.Y, parallelTol) &&
                    d1.Z.IsEqualTo(d2.Z, parallelTol);

                double PerpendicularDistance(Line l1, Line l2)
                {
                    // Calcola la distanza tra le due rette (parallele) usando il vettore normale
                    Vector3D dir = l1.Direction;
                    dir.Normalize();
                    Vector3D diff = (l2.StartPoint - l1.StartPoint).AsVector;
                    Vector3D perp = diff - Vector3D.Dot(diff, dir) * dir;
                    return perp.Length;
                }

                double GetProjectionMin(Line l, Vector3D dir)
                {
                    double p1 = Vector3D.Dot(l.StartPoint.AsVector, dir);
                    double p2 = Vector3D.Dot(l.EndPoint.AsVector, dir);
                    return Math.Min(p1, p2);
                }
                double GetProjectionMax(Line l, Vector3D dir)
                {
                    double p1 = Vector3D.Dot(l.StartPoint.AsVector, dir);
                    double p2 = Vector3D.Dot(l.EndPoint.AsVector, dir);
                    return Math.Max(p1, p2);
                }

                bool ProjectionsOverlap(Line l1, Line l2, Vector3D dir)
                {
                    double min1 = GetProjectionMin(l1, dir);
                    double max1 = GetProjectionMax(l1, dir);
                    double min2 = GetProjectionMin(l2, dir);
                    double max2 = GetProjectionMax(l2, dir);
                    // Sovrapposizione o contenimento
                    return !(max1 < min2 + overlapTol || max2 < min1 + overlapTol);
                }

                for (int i = 0; i < orientedEdges.Count; i++)
                {
                    var edge1 = brep.Edges[orientedEdges[i].CurveIndex].Curve;
                    if (!(edge1 is Line line1))
                    {
                        filteredEdges.Add(orientedEdges[i]);
                        continue;
                    }
                    var dir1 = line1.Direction;
                    dir1.Normalize();
                    bool skipEdge = false;

                    for (int j = 0; j < orientedEdges.Count; j++)
                    {
                        if (i == j) continue;
                        var edge2 = brep.Edges[orientedEdges[j].CurveIndex].Curve;
                        if (!(edge2 is Line line2)) continue;

                        var dir2 = line2.Direction;
                        dir2.Normalize();

                        if (IsParallel(dir1, dir2))
                        {
                            // Non giacciono sulla stessa retta (offset piccolo ma non zero)
                            double dist = PerpendicularDistance(line1, line2);
                            if (dist > 0 && dist < offsetTol)
                            {
                                // Proiezioni sovrapposte o contenute
                                if (ProjectionsOverlap(line1, line2, dir1))
                                {
                                    // Filtra uno dei due (tieni solo quello con indice minore)
                                    if (i > j)
                                    {
                                        skipEdge = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (!skipEdge)
                        filteredEdges.Add(orientedEdges[i]);
                }
                return filteredEdges;
            }
        }

        /// <summary>
        /// COMPUTE VECTORS FOR CreateFinalPart old
        /// Compute the cutting vectors for each face shared between the intersection and the final part
        /// </summary>
        /// <param name="intersection">
        /// Brep entity obtained from the intersection between the feature brep and the workpiece brep
        /// </param>
        /// <param name="finalPart">
        /// Brep entity of the workpiece
        /// </param>
        /// <param name="faceCuttingEdgesList">
        /// Each face will be a list and each face will have a list of cuttingedge containing the vectors for each edge
        /// </param>
        /// <returns>
        /// true if succesful, false otherwise
        /// </returns>
        public static bool ComputeVectorsOld(in Brep intersection, in Brep finalPart, ref List<List<EyeCuttingEdge>> faceCuttingEdgesList,
            double arcSegmentLength)
        {
            List<Brep.Face> sharedFaces = GetEqualFaces(finalPart, intersection);

            if (sharedFaces == null) return false;

            // Compute the normal vectors to the shared face and the adjacent faces for each edge of the shared face 
            foreach (var sharedface in sharedFaces)
            {
                // Extract the oriented edges defining the face
                var loop = sharedface.Loops[0];
                var orientedEdges = loop.Segments;

                // The elements in the list have the same shared face
                List<EyeCuttingEdge> faceCuttingEdges = new List<EyeCuttingEdge>();
                foreach (var edge in orientedEdges)
                {
                    // Extract the edge from the brep solid 
                    Brep.Edge e = finalPart.Edges[edge.CurveIndex];

                    // Extract the edge's face parents
                    Brep.Face f1 = finalPart.Faces[e.Parents[0]];
                    Brep.Face f2 = finalPart.Faces[e.Parents[1]];

                    // Check which is the face adjacent to the face shared between the intersection brep and final part
                    Brep.Face adjacent = null;
                    if (!AreFacesEqualOld(f1, sharedface))
                        adjacent = f1;
                    else if (!AreFacesEqualOld(f2, sharedface))
                        adjacent = f2;
                    else
                        continue;

                    (List<devDept.Eyeshot.Entities.Line> normalLinesSharedFace, List<devDept.Eyeshot.Entities.Line> normalLinesAdjacentFace) lines;

                    if (e.Curve.IsLinear(0.01, out Segment3D line))
                    {
                        double step = e.Curve.Length();
                        lines = NormalToFaceEdge(sharedface, adjacent, e.Curve, step);
                    }
                    else
                    {
                        // Compute a cutting line each arcSegmentLength mm
                        double step = arcSegmentLength > 0 ? arcSegmentLength : 2;
                        lines = NormalToFaceEdge(sharedface, adjacent, e.Curve, step);
                    }
                    faceCuttingEdges.Add(new EyeCuttingEdge(edge.CurveIndex, lines.normalLinesSharedFace, lines.normalLinesAdjacentFace, (int)sharedface.Surface.TranslationID.Index, (int)adjacent.Surface.TranslationID.Index));
                }
                faceCuttingEdgesList.Add(faceCuttingEdges);
            }

            foreach (var CuttingEdges in faceCuttingEdgesList)
            {
                List<devDept.Eyeshot.Entities.Line> cuttingEdgeVectors = new List<devDept.Eyeshot.Entities.Line>();
                foreach (var cuttingEdge in CuttingEdges)
                {
                    for (int i = 0; i < cuttingEdge.NormalLinesWorkedFace.Count; i++)
                    {
                        devDept.Eyeshot.Entities.Line l = ComputeCuttingLine(cuttingEdge.NormalLinesWorkedFace[i], cuttingEdge.NormalLinesAdjacentFace[i], finalPart);
                        if (l != null)
                            cuttingEdgeVectors.Add(l);
                    }

                }
            }
            return true;
        }

        /// <summary>
        /// Compute the cutting vectors for each face of the brep passed in
        /// </summary>
        /// <param name="workedPiece">
        /// Brep entity on which will be computed the cutting vectors
        /// </param>
        /// <param name="faces">
        /// Each face will be a list and each face will have a list of cuttingedge containing the vectors for each edge
        /// </param>
        /// <returns></returns>
        public static bool ComputeVectorsOld(in Brep workedPiece, ref List<(List<EyeCuttingEdge> faceCuttingEdgesList, int faceNumber)> faces)
        {

            for (int i = 0; i < workedPiece.Faces.Length; i++)
            {
                var face = workedPiece.Faces[i];

                // Extract the oriented edges defining the face
                var loop = face.Loops[0];
                var orientedEdges = loop.Segments;
                orientedEdges = orientedEdges.Reverse().ToArray();
                // The elements in the list have the same shared face
                List<EyeCuttingEdge> faceCuttingEdges = new List<EyeCuttingEdge>();
                foreach (var edge in orientedEdges)
                {
                    // Extract the edge from the brep solid 
                    Brep.Edge e = workedPiece.Edges[edge.CurveIndex];

                    // Extract the edge's face parents
                    Brep.Face f1 = workedPiece.Faces[e.Parents[0]];
                    Brep.Face f2 = workedPiece.Faces[e.Parents[1]];

                    // Check which is the face adjacent to the face shared between the intersection brep and final part
                    Brep.Face adjacent = null;
                    if (!AreFacesEqualOld(f1, face))
                        adjacent = f1;
                    else if (!AreFacesEqualOld(f2, face))
                        adjacent = f2;
                    else
                        continue;

                    (List<devDept.Eyeshot.Entities.Line> normalLinesSharedFace, List<devDept.Eyeshot.Entities.Line> normalLinesAdjacentFace) lines;

                    if (e.Curve.IsLinear(0.01, out Segment3D line))
                    {
                        double step = e.Curve.Length();
                        lines = NormalToFaceEdge(face, adjacent, e.Curve, step);
                    }
                    else
                    {
                        // Compute a cutting line each 2 mm
                        double step = 2;
                        lines = NormalToFaceEdge(face, adjacent, e.Curve, step);
                    }
                    faceCuttingEdges.Add(new EyeCuttingEdge(edge.CurveIndex, lines.normalLinesSharedFace, lines.normalLinesAdjacentFace, (int)face.Surface.TranslationID.Index, (int)adjacent.Surface.TranslationID.Index));
                }
                faces.Add((faceCuttingEdges, i));
            }

            foreach (var face in faces)
            {
                var CuttingEdges = face.faceCuttingEdgesList;
                List<devDept.Eyeshot.Entities.Line> cuttingEdgeVectors = new List<devDept.Eyeshot.Entities.Line>();
                foreach (var cuttingEdge in CuttingEdges)
                {
                    for (int i = 0; i < cuttingEdge.NormalLinesWorkedFace.Count; i++)
                    {
                        devDept.Eyeshot.Entities.Line l = ComputeCuttingLine(cuttingEdge.NormalLinesWorkedFace[i], cuttingEdge.NormalLinesAdjacentFace[i], workedPiece);
                        if (l != null)
                            cuttingEdgeVectors.Add(l);
                    }

                }
            }
            return true;
        }

        // Return a list of faces that are present in brep1 and brep2
        public static List<Brep.Face> GetEqualFaces(Brep brep1, Brep brep2)
        {
            if (brep1 == null || brep2 == null) return null;

            List<Brep.Face> equalFaces = new List<Brep.Face>();
            foreach (Brep.Face face1 in brep1.Faces)
            {
                foreach (Brep.Face face2 in brep2.Faces)
                {
                    if (AreFacesEqualOld(face1, face2))
                    {
                        equalFaces.Add(face1);
                        break;
                    }
                }
            }

            return equalFaces;
        }
        // Check if two faces are equal
        public static bool AreFacesEqualOld(Brep.Face face1, Brep.Face face2)
        {
            // Check if the number of edges in the faces are equal
            ICurve[] edges1 = face1.Parametric.FirstOrDefault().ExtractEdges();
            ICurve[] edges2 = face2.Parametric.FirstOrDefault().ExtractEdges();
            if (edges1.Length != edges2.Length)
                return false;

            // Create a list to store the unmatched curves from array1
            List<ICurve> unmatchedCurves = edges1.ToList();

            // Iterate over the curves in array2 and try to match them with the curves in unmatchedCurves
            foreach (ICurve curve2 in edges2)
            {
                bool foundMatch = false;

                // Iterate over the unmatchedCurves list
                for (int i = 0; i < unmatchedCurves.Count; i++)
                {
                    ICurve curve1 = unmatchedCurves[i];

                    double minPointDistance = 1;
                    if (curve1.StartPoint.DistanceTo(curve2.StartPoint) < minPointDistance &&
                        curve1.EndPoint.DistanceTo(curve2.EndPoint) < minPointDistance ||
                        curve1.StartPoint.DistanceTo(curve2.EndPoint) < minPointDistance &&
                        curve1.EndPoint.DistanceTo(curve2.StartPoint) < minPointDistance)
                    {
                        foundMatch = true;
                        unmatchedCurves.RemoveAt(i);
                        break;
                    }
                }

                // If no match is found for curve2, the arrays are not equal
                if (!foundMatch)
                    return false;
            }

            // If all curves in array2 have been matched, check if any unmatched curves remain in array1
            return unmatchedCurves.Count == 0;
        }
        
        // Check if two faces are equal
        public static bool AreFacesEqual(Brep.Face face1, Brep.Face face2)
        {
            if (face1.Equals(face2)) 
                return true;

            if (face1.Loops.Length != face2.Loops.Length)
                return false;

            bool areLoopsEqual = true;
            for(int i  = 0; i < face1.Loops.Length; i++)
            {
                Brep.OrientedEdge[] loop1 = face1.Loops[0].Segments,
                                    loop2 = face2.Loops[0].Segments;

                areLoopsEqual = loop1.All(edge => loop2.Select(l2 => l2.CurveIndex).Contains(edge.CurveIndex));

                if (!areLoopsEqual)
                    return false;
            }

            return true;
        }

        // Check if two curves are equal
        public static bool AreCurvesEqual(ICurve curve1, ICurve curve2)
        {
            double minPointDistance = 1;
            if (curve1.StartPoint.DistanceTo(curve2.StartPoint) < minPointDistance &&
                curve1.EndPoint.DistanceTo(curve2.EndPoint) < minPointDistance ||
                curve1.StartPoint.DistanceTo(curve2.EndPoint) < minPointDistance &&
                curve1.EndPoint.DistanceTo(curve2.StartPoint) < minPointDistance)
            {
                // Check if the curve type of the edges are equal
                if (curve1.GetType() != curve2.GetType())
                    return false;
                else
                    return true;
            }
            
            return false;
            
        }

        // Given a curve containing two set of curves on the inner radius of the cylinder and the outer radius of the cylinder,
        // returns the two separated curves 
        private void SeparateCurve(ICurve curve, out CompositeCurve cc1, out CompositeCurve cc2, EyeWorkPiece wp)
        {
            List<ICurve> curves = curve.GetIndividualCurves().ToList(),
                         innerCurves = new List<ICurve>(),
                         outerCurves = new List<ICurve>();

            double outerRadius = wp.Prf.SA / 2,
                   innerRadius = (wp.Prf.SA - wp.Prf.TA) / 2,
                   tol = 0.1;

            foreach (var c in curves)
            {
                Point3D centerStart = new Point3D(c.StartPoint.X, outerRadius, outerRadius);
                Point3D centerEnd = new Point3D(c.EndPoint.X, outerRadius, outerRadius);
                if (Math.Abs(c.StartPoint.DistanceTo(centerStart) - outerRadius) < tol && Math.Abs(c.EndPoint.DistanceTo(centerEnd) - outerRadius) < tol)
                    outerCurves.Add(c);
                else if (Math.Abs(c.StartPoint.DistanceTo(centerStart) - innerRadius) < tol && Math.Abs(c.EndPoint.DistanceTo(centerEnd) - innerRadius) < tol)
                    innerCurves.Add(c);
            }

            cc1 = new CompositeCurve(innerCurves);
            cc2 = new CompositeCurve(outerCurves);
        }

        // Compute the cutting line needed to obtain shared face
        public static devDept.Eyeshot.Entities.Line ComputeCuttingLine(devDept.Eyeshot.Entities.Line normalLineShared, devDept.Eyeshot.Entities.Line adjacentNormal, Brep finalPart)
        {
            Point3D start = (Point3D)normalLineShared.StartPoint.Clone();
            //if (finalPart.IsPointInside(start) == pointStatusType.Outside)
            //    return null;
            Point3D end = (Point3D)adjacentNormal.EndPoint.Clone();

            Vector3D normal = normalLineShared.Direction;

            Plane plane = new Plane(start, normal);

            // Project the end point onto the plane 
            Point2D projectedEnd = plane.Project(end);
            devDept.Eyeshot.Entities.Line l = new devDept.Eyeshot.Entities.Line(plane, 0, 0, projectedEnd.X, projectedEnd.Y);
            l.ColorMethod = colorMethodType.byEntity;
            l.Color = System.Drawing.Color.OrangeRed;

            return l;
        }

        // Given an edge and the two faces sharing it, compute the normals along the edge 
        private static (List<devDept.Eyeshot.Entities.Line> normalFace1, List<devDept.Eyeshot.Entities.Line> normalFace2) NormalToFaceEdge(Brep.Face face1, Brep.Face face2, ICurve edge, double step)
        {
            ICurve temp = (ICurve)edge.Clone();
            var parametric1 = face1.Parametric[0];
            var parametric2 = face2.Parametric[0];

            // lista dei vettori per questo edge
            List<devDept.Eyeshot.Entities.Line> normalFace1 = new List<devDept.Eyeshot.Entities.Line>();
            List<devDept.Eyeshot.Entities.Line> normalFace2 = new List<devDept.Eyeshot.Entities.Line>();

            // distanza in 3D
            double lenght = edge.Length();
            var points3D = temp.GetPointsByLength(step);

            foreach (var p in points3D)
            {
                bool b1 = parametric1.Project(p, 1e-6, false, out double u1, out double v1);
                bool b2 = parametric2.Project(p, 1e-6, false, out double u2, out double v2);

                devDept.Eyeshot.Entities.Line l1 = new devDept.Eyeshot.Entities.Line(p, p + parametric1.NormalAt(u1, v1) * 7);
                l1.ColorMethod = colorMethodType.byEntity;
                l1.Color = System.Drawing.Color.OrangeRed;
                devDept.Eyeshot.Entities.Line l2 = new devDept.Eyeshot.Entities.Line(p, p + parametric2.NormalAt(u2, v2) * 7);
                l2.ColorMethod = colorMethodType.byEntity;
                l2.Color = System.Drawing.Color.OrangeRed;
                normalFace1.Add(l1);
                normalFace2.Add(l2);

            }
            return (normalFace1, normalFace2);
        }

        public static void ChamferRoundTube(ICurve curve, double landing, double angleDeg, IWorkPiece wp, out Brep chamfer, double surplus = 1, double brepTol = 0.01)
        {
            double angler = angleDeg * Math.PI / 180;
            double depth = wp.Prf.TA - landing;
            double distanceFromPlaneOrigin = landing + depth + surplus;


            Plane normalToRail;
            if (curve.StartTangent.X < 0 || curve.StartTangent.Y < 0 || curve.StartTangent.Z < 0)
                normalToRail = new Plane(curve.StartPoint, -1 * curve.StartTangent);
            else
                normalToRail = new Plane(curve.StartPoint, curve.StartTangent);

            double m = Math.Tan(angler);
            double q = -m * landing;
            Point2D vert1 = new Point2D(-1, (-1 - q) / m);
            Point2D vert2 = new Point2D(vert1.X, distanceFromPlaneOrigin);
            Point2D vert3 = new Point2D(distanceFromPlaneOrigin * m + q, vert2.Y);
            devDept.Eyeshot.Entities.Region region = null;

            region = devDept.Eyeshot.Entities.Region.CreatePolygon(normalToRail, vert1, vert2, vert3);
            chamfer = region.SweepAsBrep(curve, brepTol);
        }

        public static bool IsEdgeInsideCylindricalRegion(int edgeIndex, in IWorkPiece wp, in Brep finalPart, double distanceTol)
        {
            ICurve edgeCurve = finalPart.Edges[edgeIndex].Curve;
            Point3D startPoint = edgeCurve.StartPoint,
                    endPoint = edgeCurve.EndPoint,
                    midPoint = edgeCurve.PointAt(edgeCurve.Domain.Mid);

            Point3D cylinderCenter = new Point3D(midPoint.X, wp.Prf.SA / 2, wp.Prf.SA / 2);

            double distanceToCenterMP = midPoint.DistanceTo(cylinderCenter);

            if (distanceToCenterMP - wp.Prf.SA / 2 <= -distanceTol &&
                distanceToCenterMP - (wp.Prf.SA / 2 - wp.Prf.TA) >= distanceTol)
                return true;

            return false;
        }

        public static bool IsEdgeInsideRegion(int edgeIndex, in IWorkPiece wp, in Brep finalPart, double distanceTol)
        {
            ICurve edgeCurve = finalPart.Edges[edgeIndex].Curve;
            Point3D midPoint = edgeCurve.PointAt(edgeCurve.Domain.Mid);

            Point2D mid = new Point2D(midPoint.Y, midPoint.Z);

            if (wp.Prf is EyeProfile eyeWp)
            {
                return !(eyeWp.Region.IsPointInside(mid) || eyeWp.Region.IsPointOnContour(new Point3D(0, mid.X, mid.Y), distanceTol));
            }

            return false;
        }

        public static bool IsEdgeOnRegionContour(int edgeIndex, in IWorkPiece wp, in Brep finalPart, double distanceTol)
        {
            ICurve edgeCurve = finalPart.Edges[edgeIndex].Curve;
            Point3D midPoint = edgeCurve.PointAt(edgeCurve.Domain.Mid);

            Point2D mid = new Point2D(midPoint.Y, midPoint.Z);

            if (wp.Prf is EyeProfile eyeWp)
            {
                return eyeWp.Region.IsPointOnContour(new Point3D(0, mid.X, mid.Y), distanceTol);
            }

            return false;
        }

        //
        // Controlla se un edge è all'interno del raggio del profilo ad altezza anima alta/bassa
        //
        public static bool IsEdgeInsideProfileRadius(int edgeIndex, in IWorkPiece wp, in Brep finalPart, double distanceTol)
        {
            ICurve edgeCurve = finalPart.Edges[edgeIndex].Curve;

            double webZDown = wp.Prf.CodePrf == "I" ? wp.Prf.SB / 2 - wp.Prf.TA / 2 : 0, // Z coordinate of the web lower planar face
                   webZUp = wp.Prf.CodePrf == "I" ? wp.Prf.SB / 2 + wp.Prf.TA / 2 : wp.Prf.TA,
                   yARadius = wp.Prf.TB + wp.Prf.Radius,
                   yBRadius = wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius;

            Point3D startPoint = edgeCurve.StartPoint,
                    endPoint = edgeCurve.EndPoint;
            if ((startPoint.Z.IsEqualTo(webZDown, distanceTol) && endPoint.Z.IsEqualTo(webZDown, distanceTol) || startPoint.Z.IsEqualTo(webZUp, distanceTol) && endPoint.Z.IsEqualTo(webZUp, distanceTol))
                 &&
                 (startPoint.Y.IsLessThan(yARadius, distanceTol) && startPoint.Y.IsGreaterThan(wp.Prf.TB, distanceTol) && endPoint.Y.IsLessThan(yARadius, distanceTol) && endPoint.Y.IsGreaterThan(wp.Prf.TB, distanceTol)
                 ||
                 startPoint.Y.IsGreaterThan(yBRadius, distanceTol) && startPoint.Y.IsLessThan(wp.Prf.SA - wp.Prf.TB, distanceTol) && endPoint.Y.IsGreaterThan(yBRadius, distanceTol) && endPoint.Y.IsLessThan(wp.Prf.SA - wp.Prf.TB, distanceTol)))
                return true;

            return false;

        }

        /// <summary>
        /// Check if a point inside the face surface is inside the workpiece profile region 
        /// </summary>
        /// <param name="faceIndex">
        /// Index of the face in the face list of the workpiece brep
        /// </param>
        /// <param name="wp">
        /// Workpiece
        /// </param>
        /// <param name="finalPart">
        /// Brep solid containing the face
        /// </param>
        /// <param name="distanceTol">
        /// Check tolerance
        /// </param>
        /// <returns></returns>
        public static bool IsFacePointInsideProfileRegion(int faceIndex, in IWorkPiece wp, in Brep finalPart, in double distanceTol)
        {
            var face = finalPart.Faces[faceIndex];

            var surface = face.Parametric.First();
            double midU = surface.DomainU.Mid,
                   midV = surface.DomainV.Mid;
            Point3D midPoint = surface.Evaluate(midU, midV); // Point at the face center

            if (wp is EyeWorkPiece eyeWp)
            {
                if (((EyeProfile)(eyeWp.Prf)).Region != null)
                    return !(eyeWp.Prf as EyeProfile).Region.IsPointOnContour(new Point3D(0, midPoint.Y, midPoint.Z), distanceTol);
                else
                    return false;
            }
            //return !(eyeWp.Prf as EyeProfile).Region.IsPointOnContour(new Point3D(0, midPoint.Y, midPoint.Z), distanceTol);
            else 
                return false;
        }

        public static bool IsFaceOnRegionContour(int faceIndex, in IWorkPiece wp, in Brep finalPart, double distanceTol)
        {
            var face = finalPart.Faces[faceIndex];
            var edgesIndices = face.Loops.SelectMany(x => x.Segments.Select(s => s.CurveIndex));
            
            foreach (int edgeIndex in edgesIndices)
            {
                if(!IsEdgeOnRegionContour(edgeIndex, wp, finalPart, distanceTol))
                    return false;
            }

            return true;
        }

        public static bool IsOnCircle(in Point2D center, in double radius, in Point2D point, in double distanceTol)
        {
            double distanceToCenter = center.DistanceTo(point);
            if (Math.Abs(distanceToCenter).IsEqualTo(radius, distanceTol))
                return true;
            
            return false;
        }

        public static bool IsInsideCircle(in Point2D center, in double radius, in Point2D point, in double distanceTol)
        {
            double distanceToCenter = center.DistanceTo(point);
            if (Math.Abs(distanceToCenter).IsLessThan(radius, distanceTol))
                return true;

            return false;
        }

        /// <summary>
        /// Check if two points are equal within the specified tolerance
        /// </summary>
        /// <param name="p1">
        /// First 3D point
        /// </param>
        /// <param name="p2">
        /// Second 3D point
        /// </param>
        /// <param name="tol"></param>
        /// <returns>
        /// true if they are equal, false otherwise
        /// </returns>
        public static bool ArePointsEqual(in Point3D p1, in Point3D p2, double tol)
        {
            if (Math.Abs(p1.X - p2.X) < tol &&
            Math.Abs(p1.Y - p2.Y) < tol &&
            Math.Abs(p1.Z - p2.Z) < tol)
                return true;

            return false;
        }

        /// <summary>
        /// Check if there is an edge in common between the edgeIndex passed in and the list of edges of the faces
        /// passed in inside the cylindrical regione between the outer radius and the inner radius
        /// </summary>
        /// <param name="edgeIndex"></param>
        /// <param name="distanceTol"></param>
        /// <param name="wp"></param>
        /// <param name="finalPart"></param>
        /// <param name="faceList"></param>
        /// <param name="faceIndex"></param>
        /// <param name="commonEdgeIndex"></param>
        /// <returns></returns>
        public static bool HasCommonEdge(in int edgeIndex, in double distanceTol, in IWorkPiece wp, in Brep finalPart, in List<int> ints, in List<List<EyeCuttingEdge>> faceList, out int faceIndex)
        {
            faceIndex = -1;

            if (!IsEdgeInsideRegion(edgeIndex, wp, finalPart, distanceTol * 5))
                return false;

            for (int i = 0; i < faceList.Count; i++)
            {
                var faceEdges = faceList[i];

                foreach (var edge in faceEdges)
                {
                    // definita dal cilindro esterno e cilindro interno 
                    if (edge.EdgeIndex == edgeIndex && !ints.Contains(edge.WorkedFaceIndex))
                    {
                        faceIndex = edge.WorkedFaceIndex;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
