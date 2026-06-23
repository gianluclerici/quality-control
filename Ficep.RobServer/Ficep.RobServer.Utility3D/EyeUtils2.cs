using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Milling;
using devDept.Geometry;
using devDept.Serialization;
using Ficep.RobServer.Data;
using Ficep.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ficep.AnyCut.Mathematics;
using Line = devDept.Eyeshot.Entities.Line;
using Point = Ficep.AnyCut.Mathematics.Point;
using System.Numerics;
using Vector3D = Ficep.AnyCut.Mathematics.Vector3D;

namespace Ficep.RobServer.Utility3D
{
    public partial class EyeUtils
    {

        public static bool ComputeContour(in List<ProgramPoint> ProgramPoint,
            in IWorkPiece wp, out CompositeCurve contour)
        {
            contour = null;
            List<ICurve> curves = new List<ICurve>();

            ProgramPoint m1 = null, m2 = null;
            Line lPrev = null, lFirst = null;
            bool lastPoint = false;

            for (int idx = 0; idx < ProgramPoint.Count; idx++)
            {
                m1 = ProgramPoint[idx];

                if (idx < ProgramPoint.Count - 1)
                {
                    lastPoint = false;
                    m2 = ProgramPoint[idx + 1];
                }
                else
                {
                    lastPoint = true;
                    m2 = ProgramPoint[0];
                }
                if (
                    m1.X.IsEqualTo(m2.X, 0.01) &&  // Check if the coordinates of the program points 
                    m1.Y.IsEqualTo(m2.Y, 0.01) &&  // are equals
                    m1.Z.IsEqualTo(m2.Z, 0.01)
                    )
                {
                    continue;
                }

                Point3D a = new Point3D(m1.X, m1.Y, m1.Z), b = new Point3D(m2.X, m2.Y, m2.Z);

                Line l = new Line(a, b);
                curves.Add(l);

                if (m1.Radius > 0)
                {
                    if (!EyeGeometryUtils.AddArc(ref curves, ref lPrev, ref l, m1.Radius))
                        return false;
                }

                if (lastPoint && ProgramPoint[0].Radius > 0)
                {
                    if (!EyeGeometryUtils.AddArc(ref curves, ref l, ref lFirst, ProgramPoint[0].Radius))
                        return false;
                }

                if (idx == 0)
                    lFirst = l;

                lPrev = l;
            }

            contour = new CompositeCurve(curves);

            return true;
        }

        public static bool CreateFaceCurves(in List<ProgramPoint> ProgramPoints, double tol, in IWorkPiece wp, out List<ICurve> faceCurvesList, string plane)
        {
            faceCurvesList = new List<ICurve>();

            if (ProgramPoints == null || ProgramPoints.Count == 0 || !ProgramPoints[0].AreCoordinatesEqual(ProgramPoints[ProgramPoints.Count - 1]))
                return false;

            double xc, yc;

            if (plane == "v")
            {
                // Plane center coordinate
                xc = wp.Lp / 2;
                yc = wp.Prf.SA / 2;
            }
            else
            {
                // Plane center coordinate
                xc = wp.Lp / 2;
                yc = wp.Prf.SB / 2;
            }
            ProgramPoint m1 = null, m2 = null;
            bool lastPoint;
            Line l = null;

            for (int idx = 0; idx < ProgramPoints.Count; idx++)
            {
                // Set the current and the next points
                m1 = ProgramPoints[idx];

                if (idx < ProgramPoints.Count - 1)
                {
                    lastPoint = false;
                    m2 = ProgramPoints[idx + 1];
                }
                //else
                //{
                //    lastPoint = true;
                //    m2 = ProgramPoints[0];
                //}

                if (m1 == m2)
                {
                    continue;
                }

                Point3D start = new Point3D(m1.X, m1.Y, m1.Z),
                        end = new Point3D(m2.X, m2.Y, m2.Z);
                
                if (!m1.Radius.IsEqualTo(0, tol))
                {
                    GetArc(start, end, tol, plane, m1.Radius, out Arc arc);
                    faceCurvesList.Add(arc);
                }
                else
                {
                    l = new Line(m1.X, m1.Y, m1.Z, m2.X, m2.Y, m2.Z);

                    faceCurvesList.Add(l);
                }

            }

            return true;
        }

        private static void GetArc(Point3D start, Point3D end, double tol, string plane, double radius, out Arc arc)
        {
            arc = null;

            double r = Math.Abs(radius);

            double x1, x2, y1, y2;
            devDept.Geometry.Plane projectionPlane;
            if (plane == "v")
            {
                x1 = start.X;
                y1 = start.Y;
                x2 = end.X;
                y2 = end.Y;
                projectionPlane = devDept.Geometry.Plane.XY;
            }
            else
            {
                x1 = start.X;
                y1 = start.Z;
                x2 = end.X;
                y2 = end.Z;
                projectionPlane = devDept.Geometry.Plane.XZ;
            }
            double q = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));

            double y3 = (y1 + y2) / 2;

            double x3 = (x1 + x2) / 2;

            double basex = Math.Sqrt(Math.Pow(r, 2) - Math.Pow((q / 2), 2)) * (y1 - y2) / q; //calculate once
            double basey = Math.Sqrt(Math.Pow(r, 2) - Math.Pow((q / 2), 2)) * (x2 - x1) / q; //calculate once

            double centerX, centerY, centerZ;

            Point s = new Point(x1, y1, start.Z),
                  e = new Point(x2, y2, end.Z);

            Vector3D? cs = null, ce = null;

            if (!FMathExt2.ComputeCentre(s, e, radius, out Point? c, out cs, out ce))
                return;
            
            if (plane == "v")
            {
                if (radius.IsGreaterThan(0, tol))
                {
                    centerX = x3 + basex; //center x of circle 1
                    centerY = y3 + basey; //center y of circle 1
                }
                else
                {
                    centerX = x3 - basex; //center x of circle 2
                    centerY = y3 - basey; //center y of circle 2
                }
                centerZ = start.Z;
            }
            else
            {
                
                if (radius.IsLessThan(0, tol) == (plane == "o"))
                {
                    centerX = x3 + basex; //center x of circle 1
                    centerZ = y3 + basey; //center y of circle 1
                }
                else
                {
                    centerX = x3 - basex; //center x of circle 2
                    centerZ = y3 - basey; //center y of circle 2
                }

                centerY = start.Y;
            }
            
            if (!start.DistanceTo(end).IsEqualTo(2*r, tol))
                arc = new Arc(new Point3D(centerX, centerY, centerZ), start, end);
            else
            {
                double dx1, dz1, dx2, dz2;

                dx1 = start.X - centerX;
                dz1 = start.Z - centerZ;
                dx2 = end.X - centerX;
                dz2 = end.Z - centerZ;

                double startAngle = Math.Atan2(dz1, dx1);
                double endAngle = Math.Atan2(dz2, dx2);

                arc = new Arc(projectionPlane, new Point3D(centerX, centerY, centerZ), r, startAngle, endAngle);
            }
        }

        /// <summary>
        /// Replace in the ProgramPoints list the points on the flanges with the points on the planar web boundary,
        /// compute the list of the points the cylindrical web and the list of the points on the flange
        /// </summary>
        /// <param name="programPoints">
        /// List of the points laying in the planar web region
        /// </param>
        /// <param name="wp">
        /// Workpiece parameters
        /// </param>
        /// <param name="webCylPoints">
        /// Points at the edge of the cylindrical web and the flange, the corresponding point at the web is in the ProgramPoints list
        /// ate the index contained in webCylPoints 
        /// </param>
        /// <param name="flangePoints">
        /// Points at the flange edge
        /// </param>
        /// <returns>
        /// true if the operation is successful 
        /// </returns>
        public static bool TrimWebBoundaries(ref List<ProgramPoint> programPoints, in IWorkPiece wp, double tol)
        {
            bool isOutSideWebPlanarEdges = false;

            // Points defining the boundary lines of the web with the flange,
            // left is intended as the point near the origin
            double z = wp.Prf.SB / 2 - wp.Prf.TA / 2;
            // Plane center coordinate
            double xc = wp.Lp / 2,
                   yc = wp.Prf.SA / 2;

            Point3D downLeftCornerCyl = new Point3D(0, wp.Prf.TB, z),
                    downRightCornerCyl = new Point3D(wp.Lp, wp.Prf.TB, z),
                    upLeftCornerCyl = new Point3D(0, wp.Prf.SA - wp.Prf.TB, z),
                    upRightCornerCyl = new Point3D(wp.Lp, wp.Prf.SA - wp.Prf.TB, z);

            while (!isOutSideWebPlanarEdges)
            {
                // Get the index of the points with lowest and greatest y value
                if (!GetBoundaryIndexPoints(programPoints, out int lowestYIndex, out int greatestYIndex))
                    return false;
                // Get the quadrant where the point with lowest y value lays 
                if (!Utils.Utils.GetQuadrant(programPoints[lowestYIndex].X, programPoints[lowestYIndex].Y, tol, out int q, xc, yc))
                    return false;

                ProgramPoint p1, p2;

                int lowestYPrec = lowestYIndex == 0 ? programPoints.Count - 2 : lowestYIndex - 1,
                    lowestYSucc = lowestYIndex == programPoints.Count - 1 ? 1 : lowestYIndex + 1,
                    greatestYPrec = greatestYIndex == 0 ? programPoints.Count - 2 : greatestYIndex - 1,
                    greatestYSucc = greatestYIndex == programPoints.Count - 1 ? 1 : greatestYIndex + 1;

                if (q == 3)
                {
                    if (programPoints[lowestYPrec].Y.IsLessThan(wp.Prf.TB, tol))
                    {
                        lowestYIndex--;
                        lowestYPrec--;
                    }

                    // Get the point at the boundary between the web and the flange 
                    if (!GetProgramPointAt(programPoints[lowestYPrec], programPoints[lowestYIndex], downLeftCornerCyl, downRightCornerCyl, upLeftCornerCyl, upRightCornerCyl, tol, wp, out p1, out p2))
                        return false;
                }
                else
                {
                    if (programPoints[lowestYSucc].Y.IsLessThan(wp.Prf.TB, tol))
                    {
                        lowestYIndex++;
                        lowestYSucc++;
                    }
                    // Get the point at the boundary between the cylindrical web and the flange 
                    if (!GetProgramPointAt(programPoints[lowestYIndex], programPoints[lowestYSucc], downLeftCornerCyl, downRightCornerCyl, upLeftCornerCyl, upRightCornerCyl, tol, wp, out p1, out p2))
                        return false;
                }

                // Add the point at the edge of the web and flange at the right position
                if (p2 is null)
                { 
                    programPoints.RemoveAt(lowestYIndex);
                    programPoints.Insert(lowestYIndex, p1);
                }
                else
                {
                    Point3D a = new Point3D(p1.X, p1.Y, p1.Z),
                            b = new Point3D(programPoints[lowestYIndex].X, programPoints[lowestYIndex].Y, programPoints[lowestYIndex].Z),
                            c = new Point3D(p2.X, p2.Y, p2.Z);

                    // If the distance of a is less than the distance of c from b then p1 is programPoints[lowestYIndex]
                    double ab = a.DistanceTo(b),
                           cb = c.DistanceTo(b);

                    if (ab.IsLessThan(cb, tol) && q == 3)
                    { 
                        programPoints.RemoveAt(lowestYIndex);
                        programPoints.Insert(lowestYIndex, p1);

                        programPoints.RemoveAt(lowestYPrec);
                        programPoints.Insert(lowestYPrec, p2);
                    }
                    else if (ab.IsLessThan(cb, tol) && q == 4)
                    {
                        programPoints.RemoveAt(lowestYIndex);
                        programPoints.Insert(lowestYIndex, p1);

                        programPoints.RemoveAt(lowestYSucc);
                        programPoints.Insert(lowestYSucc, p2);
                    }
                    else if (ab.IsGreaterThan(cb, tol) && q == 4)
                    {
                        programPoints.RemoveAt(lowestYIndex);
                        programPoints.Insert(lowestYIndex, p2);

                        programPoints.RemoveAt(lowestYSucc);
                        programPoints.Insert(lowestYSucc, p1);
                    }
                    else
                    {
                        programPoints.RemoveAt(lowestYIndex);
                        programPoints.Insert(lowestYIndex, p2);

                        programPoints.RemoveAt(lowestYPrec);
                        programPoints.Insert(lowestYPrec, p1);
                    }

                }

                if (!Utils.Utils.GetQuadrant(programPoints[greatestYIndex].X, programPoints[greatestYIndex].Y, tol, out q, xc, yc))
                    return false;

                if (q == 1)
                {
                    if (programPoints[greatestYPrec].Y.IsGreaterThan(wp.Prf.SA - wp.Prf.TB, tol))
                    {
                        greatestYIndex--;
                        greatestYPrec--;
                    }
                    // Get the point at the boundary between the cylindrical web and the flange 
                    if (!GetProgramPointAt(programPoints[greatestYPrec], programPoints[greatestYIndex], downLeftCornerCyl, downRightCornerCyl, upLeftCornerCyl, upRightCornerCyl, tol, wp, out p1, out p2))
                        return false;
                }
                else
                {
                    if (programPoints[greatestYSucc].Y.IsGreaterThan(wp.Prf.SA - wp.Prf.TB, tol))
                    {
                        greatestYIndex++;
                        greatestYSucc++;
                    }
                    // Get the point at the boundary between the cylindrical web and the flange 
                    if (!GetProgramPointAt(programPoints[greatestYIndex], programPoints[greatestYSucc], downLeftCornerCyl, downRightCornerCyl, upLeftCornerCyl, upRightCornerCyl, tol, wp, out p1, out p2))
                        return false;
                }

                // Add the point at the edge of the planar and cylindrical web at the right position
                // Add the point at the edge of the web and flange at the right position
                if (p2 is null)
                {
                    programPoints.RemoveAt(greatestYIndex);
                    programPoints.Insert(greatestYIndex, p1);
                }
                else
                {
                    Point3D a = new Point3D(p1.X, p1.Y, p1.Z),
                            b = new Point3D(programPoints[greatestYIndex].X, programPoints[lowestYIndex].Y, programPoints[greatestYIndex].Z),
                            c = new Point3D(p2.X, p2.Y, p2.Z);
                    // If the distance of a is less than the distance of c from b then p1 is programPoints[lowestYIndex]
                    double ab = a.DistanceTo(b),
                           cb = c.DistanceTo(b);

                    if (ab.IsLessThan(cb, tol) && q == 1)
                    {
                        programPoints.RemoveAt(greatestYIndex);
                        programPoints.Insert(greatestYIndex, p1);

                        programPoints.RemoveAt(greatestYPrec);
                        programPoints.Insert(greatestYPrec, p2);
                    }
                    else if (ab.IsLessThan(cb, tol) && q == 2)
                    {
                        programPoints.RemoveAt(greatestYIndex);
                        programPoints.Insert(greatestYIndex, p1);

                        programPoints.RemoveAt(greatestYSucc);
                        programPoints.Insert(greatestYSucc, p2);
                    }
                    else if (ab.IsGreaterThan(cb, tol) && q == 1)
                    {
                        programPoints.RemoveAt(greatestYIndex);
                        programPoints.Insert(greatestYIndex, p2);

                        programPoints.RemoveAt(greatestYSucc);
                        programPoints.Insert(greatestYSucc, p1);
                    }
                    else
                    {
                        programPoints.RemoveAt(greatestYIndex);
                        programPoints.Insert(greatestYIndex, p2);

                        programPoints.RemoveAt(greatestYPrec);
                        programPoints.Insert(greatestYPrec, p1);
                    }

                }

                // Get the index of the points with lowest and greatest y value
                if (!GetBoundaryIndexPoints(programPoints, out lowestYIndex, out greatestYIndex))
                    return false;

                isOutSideWebPlanarEdges = programPoints[greatestYIndex].Y.IsEqualTo(wp.Prf.SA - wp.Prf.TB, 0.1)&&
                                          programPoints[lowestYIndex].Y.IsEqualTo(wp.Prf.TB, 0.1);
            }

            CheckProgamPointsCorrectness(ref programPoints, tol);

            return true;
        }

        // Check if in the list are present equal program points and in case remove the point 
        private static void CheckProgamPointsCorrectness(ref List<ProgramPoint> programPoints, double tol)
        {
            ProgramPoint prec, curr, succ;
            List<int> toBeRemoved = new List<int>();
            
            for (int i = 1; i < programPoints.Count() - 1; i++)
            {
                curr = programPoints[i];
                prec = programPoints[i - 1];
                succ = programPoints[i + 1];

                if (prec.X.IsEqualTo(curr.X, tol) && prec.Y.IsEqualTo(curr.Y, tol) && prec.Z.IsEqualTo(curr.Z, tol))
                {
                    if (prec.Radius == 0)
                        programPoints.RemoveAt(i - 1);
                    else
                        programPoints.RemoveAt(i);
                }
                if (succ.X.IsEqualTo(curr.X, tol) && succ.Y.IsEqualTo(curr.Y, tol) && succ.Z.IsEqualTo(curr.Z, tol))
                {
                    if (succ.Radius == 0)
                        programPoints.RemoveAt(i + 1);
                    else
                        programPoints.RemoveAt(i);
                }
            }

            ProgramPoint first = programPoints[0],
                         last = programPoints[programPoints.Count - 1];
           
            if (first.X.IsEqualTo(last.X, tol) && first.Y.IsEqualTo(last.Y, tol)  && first.Z.IsEqualTo(last.Z, tol))
            {
                last.Radius = first.Radius;
            }

        }

        /// <summary>
        /// Get the indices of the points with lowest y and greatest y
        /// </summary>
        /// <param name="ProgramPoints">
        /// List of points
        /// </param>
        /// <param name="indexLower">
        /// Index of the point with lowest y
        /// </param>
        /// <param name="indexUpper">
        /// Index of the point with highest y
        /// </param>
        /// <returns>
        /// true if successful
        /// </returns>
        private static bool GetBoundaryIndexPoints(in List<ProgramPoint> ProgramPoints, out int lowestYIndex, out int greatestYIndex)
        {
            lowestYIndex = -1;
            greatestYIndex = -1;

            if (ProgramPoints != null && ProgramPoints.Count < 2)
                return false;

            // Find the minimum and maximum Y values
            double minY = ProgramPoints.Min(m => m.Y);
            double maxY = ProgramPoints.Max(m => m.Y);

            // Find the indices of points with the lowest and greatest Y values
            lowestYIndex = ProgramPoints.FindIndex(m => Math.Abs(m.Y - minY) < double.Epsilon);
            greatestYIndex = ProgramPoints.FindIndex(m => Math.Abs(m.Y - maxY) < double.Epsilon);

            return true;
        }


        /// <summary>
        /// Given the points defining the original curve described in the dstv that have the maximum y and minimum y obtain 
        /// the point intersecting the line defined by downLeftCorner and downLeftCorner or by upLeftCorner and upRightCorner
        /// </summary>
        /// <param name="pPrev">
        /// Start point of the curve
        /// </param>
        /// <param name="pCurr">
        /// End point of the curve
        /// </param>
        /// <param name="wp">
        /// Workpiece parameters
        /// </param>
        /// <param name="p1">
        /// Intersection macro point
        /// </param>
        /// <returns>
        /// true if succesful
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool GetProgramPointAt(in ProgramPoint pPrev, in ProgramPoint pCurr, in Point3D downLeftCorner, in Point3D downRightCorner, in Point3D upLeftCorner, in Point3D upRightCorner, double tol, in IWorkPiece wp,
                                              out ProgramPoint p1, out ProgramPoint p2)
        {
            p1 = null;
            p2 = null;

            if (pPrev == null || pCurr == null || wp == null)
                return false;

            // Web plane center coordinates
            double xc = wp.Lp / 2,
                   yc = wp.Prf.SA / 2;

            // Lines defining the boundaries of the planar web from the cylindrical web 
            Line downLine = new Line(downLeftCorner, downRightCorner),
                 upLine = new Line(upLeftCorner, upRightCorner);

            // Curve of the original dstv
            ICurve dstvCurve;

            // Intersect the curve of the original dstv with the boundaries of the web retrieving the intersection point
            if (pPrev.Radius > 0)
            {
                Point3D start = new Point3D(pCurr.X, pCurr.Y, pCurr.Z),
                        commonVertex,
                        end = new Point3D(pPrev.X, pPrev.Y, pPrev.Z);

                if (!Utils.Utils.GetQuadrant(start.X, start.Y, tol, out int q, xc, yc))
                    throw new NotImplementedException();

                if (q == 2 || q == 4)
                    commonVertex = new Point3D(pCurr.X, pPrev.Y, pCurr.Z);
                else
                    commonVertex = new Point3D(pPrev.X, pCurr.Y, pCurr.Z);

                Line lPrev = new Line(start, commonVertex);
                Line l = new Line(commonVertex, end);

                if (!EyeGeometryUtils.AddArc(ref lPrev, ref l, pPrev.Radius, out Arc arc))
                    return false;

                dstvCurve = arc;
            }
            else if (pPrev.Radius < 0)
            {
                Point3D start = new Point3D(pCurr.X, pCurr.Y, pCurr.Z),
                        commonVertex,
                        end = new Point3D(pPrev.X, pPrev.Y, pPrev.Z);

                if (!Utils.Utils.GetQuadrant(start.X, start.Y, tol, out int q, xc, yc))
                    throw new NotImplementedException();

                if (q == 2 || q == 4)
                    commonVertex = new Point3D(pCurr.X, pPrev.Y, pCurr.Z);
                else
                    commonVertex = new Point3D(pPrev.X, pCurr.Y, pCurr.Z);

                Line lPrev = new Line(start, commonVertex);
                Line l = new Line(commonVertex, end);

                if (!EyeGeometryUtils.AddArc(ref lPrev, ref l, pPrev.Radius, out Arc arc))
                    return false;

                dstvCurve = arc;
            }
            else
            {
                Point3D a = new Point3D(pPrev.X, pPrev.Y, pPrev.Z),
                        b = new Point3D(pCurr.X, pCurr.Y, pCurr.Z);
                dstvCurve = new Line(a, b);
            }

            Point3D[] topPoint = dstvCurve.IntersectWith(upLine);
            Point3D[] bottomPoint = dstvCurve.IntersectWith(downLine);

            if (topPoint.Length > 1 || bottomPoint.Length > 1 || (topPoint.Length == 0 && bottomPoint.Length == 0))
                return false;

            // if there is a top point and a bottom point the line intersect the top and bottom web boundaries 
            // so two program points have to be computed
            if (topPoint.Length == 1 && bottomPoint.Length == 1)
            { 
                p1 = new ProgramPoint(topPoint.First().X, topPoint.First().Y, topPoint.First().Z, 0);
                p2 = new ProgramPoint(bottomPoint.First().X, bottomPoint.First().Y, bottomPoint.First().Z, 0);
            }
            else
            {
                Point3D intersectionPoint = topPoint.Length != 0 ? topPoint[0] : bottomPoint[0];

                p1 = new ProgramPoint(intersectionPoint.X, intersectionPoint.Y, intersectionPoint.Z, 0);
            }

            return true;
        }
    }
}
