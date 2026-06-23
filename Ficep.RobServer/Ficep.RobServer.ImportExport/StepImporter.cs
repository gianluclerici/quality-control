using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Utility3D;
using Ficep.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.RobServer.ImportExport
{
    // TODO  1300 * 600 profilo max quindi se una delle dimensioni è maggiore di 1300 allora quella è la lunghezza del pezzo
    public partial class StepImporter
    {
        /// <summary>
        /// Translate the solid in order to have it along the positive directions of the axes
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static bool TranslateSolid(ref Brep solid)
        {
            if (!solid.BoxMin.X.IsEqualTo(0, 0.1))
            {
                solid.Translate(-solid.BoxMin.X, 0);
            }
            if (!solid.BoxMin.Y.IsEqualTo(0, 0.1))
            {
                solid.Translate(0, -solid.BoxMin.Y);
            }
            if (!solid.BoxMin.Z.IsEqualTo(0, 0.1))
            {
                solid.Translate(0, 0, -solid.BoxMin.Z);
            }

            return true;
        }

        /// <summary>
        /// Given a brep solid positioned in the origin and having its dimensions along the positive axes,
        /// compute the transformation needed to position the solid in order to have the profile section in the YZ plane.
        /// Compute the WorkPiece informations
        /// </summary>
        /// <param name="solid"></param>
        /// <param name="eyeWp"></param>
        /// <returns></returns>
        public static bool GetProfileInformations(ref Brep solid, out EyeWorkPiece eyeWp)
        {
            if (newMethod)
                return GetProfileInformations2 (ref solid, out eyeWp);

            // Input param
            double tolLinear = 0.1;
            //

            //solid.Regen(0.001); Guardare se fare il regen
            eyeWp = null;
            IEqualityComparer<Point2D> comparer = new Points2DComparer(tolLinear);

            // Compute the bounding box and extract the relevant data

            double maxZ = solid.BoxSize.Z, minZ = 0,
                   maxY = solid.BoxSize.Y, minY = 0,
                   maxX = solid.BoxSize.X, minX = 0;

            double lY = maxY - minY,
                   lZ = maxZ - minZ,
                   lX = maxX - minX;
            
            var verXY = solid.Vertices.Select(v => new Point2D(v.X, v.Y)).Distinct(comparer).ToList();
            var verYX = solid.Vertices.Select(v => new Point2D(v.Y, v.X)).Distinct(comparer).ToList();
            var verYZ = solid.Vertices.Select(v => new Point2D(v.Y, v.Z)).Distinct(comparer).ToList();
            var verZY = solid.Vertices.Select(v => new Point2D(v.Z, v.Y)).Distinct(comparer).ToList();
            var verXZ = solid.Vertices.Select(v => new Point2D(v.X, v.Z)).Distinct(comparer).ToList();
            var verZX = solid.Vertices.Select(v => new Point2D(v.Z, v.X)).Distinct(comparer).ToList();

            // Check for the profile and compute its dimensions
            if (IsIProfile(verXY, verYX, verYZ, verZY, verXZ, verZX, lX, lY, lZ, tolLinear, ref solid, out eyeWp))
                return true;
            else if (IsUProfile(verXY, verYX, verYZ, verZY, verXZ, verZX, lX, lY, lZ, tolLinear, ref solid, out eyeWp))
                return true;
            else if(IsQProfile(verXY, verYZ, verXZ, lX, lY, lZ, tolLinear, ref solid, out eyeWp))
                return true;
            else if (IsRProfile(verXY, verYZ, verXZ, lX, lY, lZ, tolLinear, ref solid, out eyeWp))
                return true;
            else if (IsLProfile(verXY, verYX, verYZ, verZY, verXZ, verZX, lX, lY, lZ, tolLinear, ref solid, out eyeWp))
                return true;
            // Per i profili flat TA <= 50 e TA / width <= 2
 
            return false;
        }

        private static bool IsIProfile(List<Point2D> verXY, List<Point2D> verYX, List<Point2D> verYZ, List<Point2D> verZY, List<Point2D> verXZ,
                                       List<Point2D> verZX, double lX, double lY, double lZ, double tolLinear,
                                       ref Brep solid, out EyeWorkPiece eyeWp)
        {
            if (IsIProfile(verXY, lX, lY, tolLinear, out eyeWp))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsIProfile(verYX, lY, lX, tolLinear, out eyeWp))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsIProfile(verYZ, lY, lZ, tolLinear, out eyeWp))
            {
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsIProfile(verZY, lZ, lY, tolLinear, out eyeWp))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lZ, 0);
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsIProfile(verXZ, lX, lZ, tolLinear, out eyeWp))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisMinusZ);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lY;
                return true;
            }
            else if (IsIProfile(verZX, lZ, lX, tolLinear, out eyeWp))
            {
                eyeWp.Lp = lY;
                return true;
            }
            else
                return false;
        }
        private static bool IsUProfile(List<Point2D> verXY, List<Point2D> verYX, List<Point2D> verYZ, List<Point2D> verZY, List<Point2D> verXZ,
                                       List<Point2D> verZX, double lX, double lY, double lZ, double tolLinear,
                                       ref Brep solid, out EyeWorkPiece eyeWp)
        {
            if (IsUProfile(verXY, lX, lY, tolLinear, out eyeWp))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsUProfile(verYX, lY, lX, tolLinear, out eyeWp))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsUProfile(verYZ, lY, lZ, tolLinear, out eyeWp))
            {
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsUProfile(verZY, lZ, lY, tolLinear, out eyeWp))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lZ, 0);
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsUProfile(verXZ, lX, lZ, tolLinear, out eyeWp))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisMinusZ);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lY;
                return true;
            }
            else if (IsUProfile(verZX, lZ, lX, tolLinear, out eyeWp))
            {
                eyeWp.Lp = lY;
                return true;
            }
            else
                return false;
        }
        private static bool IsQProfile(List<Point2D> verXY, List<Point2D> verYZ, List<Point2D> verXZ,
                                       double lX, double lY, double lZ, double tolLinear,
                                       ref Brep solid, out EyeWorkPiece eyeWp)
        {
            if (IsQProfile(verXY, lX, lY, tolLinear, out eyeWp))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsQProfile(verYZ, lY, lZ, tolLinear, out eyeWp))
            {
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsQProfile(verXZ, lX, lZ, tolLinear, out eyeWp))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisMinusZ);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lY;
                return true;
            }
            else
                return false;
        }
        private static bool IsRProfile(List<Point2D> verXY, List<Point2D> verYZ, List<Point2D> verXZ,
                                       double lX, double lY, double lZ, double tolLinear,
                                       ref Brep solid, out EyeWorkPiece eyeWp)
        {
            if (IsRProfile(verXY, lX, lY, tolLinear, out eyeWp))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsRProfile(verYZ, lY, lZ, tolLinear, out eyeWp))
            {
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsRProfile(verXZ, lX, lZ, tolLinear, out eyeWp))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisMinusZ);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lY;
                return true;
            }
            else
                return false;
        }
        private static bool IsLProfile(List<Point2D> verXY, List<Point2D> verYX, List<Point2D> verYZ, List<Point2D> verZY, List<Point2D> verXZ,
                                       List<Point2D> verZX, double lX, double lY, double lZ, double tolLinear,
                                       ref Brep solid, out EyeWorkPiece eyeWp)
        {
            bool isVerticalPlaneOnRight;
            if (IsLProfile(verXY, lX, lY, tolLinear, out eyeWp, out isVerticalPlaneOnRight))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsLProfile(verYX, lY, lX, tolLinear, out eyeWp, out isVerticalPlaneOnRight))
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);
                solid.Translate(lZ, 0, 0);
                eyeWp.Lp = lZ;
                return true;
            }
            else if (IsLProfile(verYZ, lY, lZ, tolLinear, out eyeWp, out isVerticalPlaneOnRight))
            {
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsLProfile(verZY, lZ, lY, tolLinear, out eyeWp, out isVerticalPlaneOnRight))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisX);
                solid.Translate(0, lZ, 0);
                eyeWp.Lp = lX;
                return true;
            }
            else if (IsLProfile(verXZ, lX, lZ, tolLinear, out eyeWp, out isVerticalPlaneOnRight))
            {
                solid.Rotate(Math.PI / 2, Vector3D.AxisMinusZ);
                solid.Translate(0, lX, 0);
                eyeWp.Lp = lY;
                return true;
            }
            else if (IsLProfile(verZX, lZ, lX, tolLinear, out eyeWp, out isVerticalPlaneOnRight))
            {
                eyeWp.Lp = lY;
                return true;
            }
            else
                return false;
        }
        private static bool IsIProfile(List<Point2D> vertices, double boxWidth, double boxHeight, double tolLinear,
                                       out EyeWorkPiece wp)
        {
            Points2DComparer equalityComparer = new Points2DComparer(tolLinear);
            wp = null;
            List<Point2D> vertOnBottom = vertices.Where(v => v.Y.IsEqualTo(0, tolLinear)).ToList(),
                          vertOnTop = vertices.Where(v => v.Y.IsEqualTo(boxHeight, tolLinear)).ToList();

            // Extract the x of the point layng on the TB distance
            List<Point2D> PointsOnHalfRight = vertOnBottom.Where(v => v.X < boxWidth / 2 && v.X.IsGreaterThan(0, tolLinear))?.OrderByDescending(v => v.X)?.ToList();
            double? tbRef = PointsOnHalfRight?.FirstOrDefault()?.X;
            if (tbRef is null)
                return false;
            double tb = tbRef.Value;
            double leftTb = boxWidth - tb;

            // Extract all the vert laying on TB and on BoxWidth - TB
            List<Point2D> vertOnTBRightFlange = vertices.Where(v => v.X.IsEqualTo(tb, tolLinear * 0.1))?.ToList(),
                          vertOnTBLeftFlange = vertices.Where(v => v.X.IsEqualTo(leftTb, tolLinear * 0.1))?.ToList();

            if (vertOnTBLeftFlange is null || vertOnTBRightFlange is null || vertOnTBLeftFlange.Count < 4 || vertOnTBRightFlange.Count < 4)
                return false;

            // Extract the y closer to the middle, will be the y of a point belonging to the web
            //*********************************************************************************************************************************
            // Extract the List of vertices closer to the half height with different values of y and ordered ascending by
            // distance from half height
            var verticesInsideTheWeb = vertices.Where(v => v.X.IsGreaterThan(tb, tolLinear) && v.X.IsLessThan(leftTb, tolLinear)).ToList();
            var verticesGroupedBySameY = verticesInsideTheWeb.GroupBy(v => Math.Floor(v.Y)).ToList();
            List<Point2D> verCloserToHalfHeight = verticesGroupedBySameY.Select(g => g.First()).OrderBy(v => Math.Abs(v.Y - boxHeight / 2)).ToList();
            // Once obtained the list half TA will be the distance that at least two points have from the half height
            double halfTA = 0;
            for (int i = 0; i < verCloserToHalfHeight.Count - 1; i++)
            {
                double currY = verCloserToHalfHeight[i].Y;
                double nextY = verCloserToHalfHeight[i + 1].Y;

                double candidateHalTA1 = Math.Abs(currY - boxHeight / 2),
                       candidateHalfTA2 = Math.Abs(nextY - boxHeight / 2);
                if (candidateHalTA1.IsEqualTo(candidateHalfTA2, tolLinear))
                {
                    halfTA = candidateHalfTA2;
                    break;
                }
            }
            //****************************************************************************************************************************************
            double webThFlangeWthRatio = halfTA * 2 / boxHeight;
            double limitRatio = 0.2; // should be max 1.6 from online research
            // TODO Chiedere a Marco il rapporto da inserire tra spessore anima e SB
            if (halfTA == 0 || webThFlangeWthRatio.IsGreaterThan(limitRatio, tolLinear))
                return false;
            //*********************************************************************************************************

            // Extract all the points laying on the web
            double topW = boxHeight / 2 + halfTA,
                   bottomW = boxHeight / 2 - halfTA;
            List<Point2D> webTop = vertices.Where(v => v.Y.IsEqualTo(topW, tolLinear))?.ToList(),
                          webBottom = vertices.Where(v => v.Y.IsEqualTo(bottomW, tolLinear))?.ToList();

            if (webTop is null || webBottom is null || webTop.Count < 2 || webBottom.Count < 2 || halfTA.IsEqualTo(boxHeight / 2, tolLinear))
                return false;

            double radius = Math.Abs(topW - vertOnTBRightFlange.OrderBy(v => Math.Abs(topW - v.Y)).First().Y);

            wp = new EyeWorkPiece("I", boxWidth, halfTA * 2, boxHeight, tb, radius);
            return true;
        }

        private static bool IsUProfile(List<Point2D> vertices, double boxWidth, double boxHeight, double tolLinear,
                                       out EyeWorkPiece wp)
        {
            Points2DComparer equalityComparer = new Points2DComparer(tolLinear);
            wp = null;
            // Extract the vertices laying in the lines with y = 0 and y = boxHeight
            List<Point2D> vertOnBottom = vertices.Where(v => v.Y.IsEqualTo(0, tolLinear)).ToList(),
                          vertOnTop = vertices.Where(v => v.Y.IsEqualTo(boxHeight, tolLinear)).ToList();

            // Extract the x of the point layng on the TB distance
            // Extract the greatest x coordinate of the points laying on the top line having x < boxWidth / 2, if the vertices belong to a U profile
            // the greatest x correspond to the thickness of the flange
            List<Point2D> PointsOnHalfRight = vertOnTop.Where(v => v.X < boxWidth / 2 && v.X.IsGreaterThan(0, tolLinear))?.OrderByDescending(v => v.X)?.ToList();
            double? tbRef = PointsOnHalfRight?.FirstOrDefault()?.X;
            if (tbRef is null)
                return false;
            double tb = tbRef.Value;
            double leftTb = boxWidth - tb;

            // Extract all the vert laying on TB and on BoxWidth - TB
            List<Point2D> vertOnTBRightFlange = vertices.Where(v => v.X.IsEqualTo(tb, tolLinear * 0.1))?.ToList(),
                          vertOnTBLeftFlange = vertices.Where(v => v.X.IsEqualTo(leftTb, tolLinear * 0.1))?.ToList();

            if (vertOnTBLeftFlange is null || vertOnTBRightFlange is null || vertOnTBLeftFlange.Count < 2 || vertOnTBRightFlange.Count < 2)
                return false;

            ToleranceEqualityComparer tolComparer = new ToleranceEqualityComparer(tolLinear);
            // Extract the vertices having the x greater than the x of the right flange (TB) and less than the left flange (leftTB)
            // in this area we will find vertices belonging to the radius or to the web:
            // To skip vertices belonging to the radius area we will take the vertices having the x closer to boxWidth / 2
            // To skip the vertices on the chamfer we will take the vertices having the y closer to boxHeight
            var verticesInsideTheWeb = vertices.Where(v => v.X.IsGreaterThan(tb, tolLinear) && v.X.IsLessThan(leftTb, tolLinear)).ToList();
            // If there are vertices in  the web area having the y greater than boxHeight / 2 the profile is not U
            if (verticesInsideTheWeb.Any(v => v.Y > boxHeight / 2))
                return false;

            // Group the vertices by y coordinate and discard the groups that contain less than 2 vertices
            var verticesGroupedBySameY = verticesInsideTheWeb.GroupBy(v => v.Y, tolComparer).Where(g => g.Count() >= 2).ToList();
            // Order the groups by the number of elements in them and then by the distance from boxHeight / 2
            // the vertices laying on the web thickness will be the group having the highest number of elements and for the groups
            // having the same number of elements select the one closer to half height
            var verticesLayingOnTa = verticesGroupedBySameY.OrderBy(g => g.Count()).ThenBy(g => Math.Abs(g.Key - boxHeight / 2)).FirstOrDefault()?.ToList();

            if (verticesLayingOnTa is null || verticesLayingOnTa.Count < 2)
                return false;

            // Once obtained the list TA will be the y coordinate of the first one in the list
            double ta = verticesLayingOnTa.First().Y;
            if (ta.IsEqualTo(boxHeight / 2, tolLinear))
                return false;

            double webThFlangeWthRatio = ta / boxHeight;
            double limitRatio = 0.2; // should be max 1.6 from online research

            if (ta == 0 || webThFlangeWthRatio.IsGreaterThan(limitRatio, tolLinear))
                return false;

            double radius = Math.Abs(ta - vertOnTBRightFlange.OrderBy(v => Math.Abs(ta - v.Y)).First().Y);

            wp = new EyeWorkPiece("U", boxWidth, ta, boxHeight, tb, radius);
            return true;
        }

        private static bool IsQProfile(List<Point2D> vertices, double boxWidth, double boxHeight, double tolLinear,
                                       out EyeWorkPiece wp)
        {
            wp = null;

            // Check if there are vertices on the bounding box
            List<Point2D> bbBottom = vertices.Where(v => v.Y.IsEqualTo(0, tolLinear))?.ToList(),
                          bbRight = vertices.Where(v => v.X.IsEqualTo(0, tolLinear))?.ToList(),
                          bbLeft = vertices.Where(v => v.X.IsEqualTo(boxWidth,tolLinear))?.ToList(),
                          bbTop = vertices.Where(v => v.Y.IsEqualTo(boxHeight, tolLinear))?.ToList();

            // If the list are null or have less than 2 elements return null because it cannot be a square tube
            if (bbBottom is null || bbRight is null || bbLeft is null || bbTop is null||
                bbBottom.Count < 2 || bbRight.Count < 2 || bbLeft.Count < 2 || bbTop.Count < 2)
                return false;

            // Used to group with a tolerance
            ToleranceEqualityComparer tol = new ToleranceEqualityComparer(tolLinear);
            // Get the list of vertices inside the bounding box
            List<Point2D> pointsInsideTheBoundingBox = vertices.Where(v => !v.Y.IsEqualTo(0, tolLinear) && !v.Y.IsEqualTo(boxHeight, tolLinear) &&
                                                        !v.X.IsEqualTo(0, tolLinear) && !v.X.IsEqualTo(boxWidth, tolLinear)).ToList();
            // Group the vertices inside the bbox with the same x 
            var verticesVertical = pointsInsideTheBoundingBox.GroupBy(v => v.X, tol)
                                  .Where(g => !g.Key.IsEqualTo(0, tolLinear) && !g.Key.IsEqualTo(boxWidth, tolLinear))
                                  .ToList();
            // Group the vertices inside the bbox with the same Y
            var verticesHorizontal = pointsInsideTheBoundingBox.GroupBy(v => v.Y, tol)
                                    .Where(g => !g.Key.IsEqualTo(0, tolLinear) && !g.Key.IsEqualTo(boxHeight, tolLinear))
                                    .ToList();

            // Compute a dictionary where the key will be TA and the group inside the key are all the points having the same TA,
            // in case of points having the same x the TA will be computed as the distance from 0 if the x is lower than bowWidth /2 
            // or as |x - boxWidth| if greater than boxWidth / 2
            Dictionary<double, List<IGrouping<double, Point2D>>> groupedByThicknessY = GroupByThickness(verticesHorizontal, boxHeight);
            Dictionary<double, List<IGrouping<double, Point2D>>> groupedByThicknessX = GroupByThickness(verticesVertical, boxWidth);

            List<double> candidatesTA = new List<double>();
            foreach (var groupY in groupedByThicknessY)
            {
                // Check if groupedByThicknessX contains the same key of the gorup of groupedByThicknessY where the key is the computed thickness
                if (groupedByThicknessX.ContainsKey(groupY.Key))
                {
                    double ta = groupY.Key;

                    // Check if there are points STRICTLY inside the square of size (boxWidth - TA) X (boxWidth - TA)
                    // if not it is a square tube but we don't know the TA.
                    // so add all the possible values of Ta such that the all the vertices are outside the rectange (boxWidth - TA) X (boxHeight - TA)
                    if (!vertices.Any(v => (v.X.IsGreaterThan(ta, tolLinear) && v.X.IsLessThan(boxWidth - ta, tolLinear) ||
                                            v.X.IsEqualTo(ta, tolLinear) && v.X.IsEqualTo(boxWidth - ta, tolLinear)) &&
                                           (v.Y.IsGreaterThan(ta, tolLinear) && v.Y.IsLessThan(boxWidth - ta, tolLinear) ||
                                           v.Y.IsEqualTo(ta, tolLinear) && v.Y.IsEqualTo(boxWidth - ta, tolLinear))))
                    {
                        candidatesTA.Add(ta);
                    }

                }
            }

            if (candidatesTA.Count >= 1)
            {
                // If there is at leat 1 element in the list candidatesTA then TA is the minimum value of the list because is the 
                // greater Square such that all the vertices are on it or outside it 
                double ta = candidatesTA.Min();
                List<Point2D> pointsOnRightInternalSide = groupedByThicknessX[ta].Select(g => g.ToList()).Where(points => points.First().X < boxWidth / 2).First().ToList();
                double minY = pointsOnRightInternalSide.Min(p => p.Y);
                double innerRadius = minY - ta;

                List<Point2D> pointsOnRightExternalSide = bbRight;
                double outerRadius = pointsOnRightExternalSide.Min(p => p.Y);

                wp = new EyeWorkPiece("Q", boxWidth, ta, boxHeight, ta, outerRadius);
                return true;
            }

            return false;
        }

        private static bool IsRProfile(List<Point2D> vertices, double boxWidth, double boxHeight, double tolLinear,
                                       out EyeWorkPiece wp)
        {
            wp = null;

            if (!boxWidth.IsEqualTo(boxHeight, tolLinear))
                return false;

            double outerRadius = boxHeight / 2;
            Point2D center = new Point2D(outerRadius, outerRadius);
            // Check if there are vertices outside the external radius i.e not inside and not on the circle
            bool verticesOutsideOuterCircle = vertices.Any(v => !EyeUtils.IsInsideCircle(center, outerRadius, v, tolLinear) &&
                                                                !EyeUtils.IsOnCircle(center, outerRadius, v, tolLinear));

            if (verticesOutsideOuterCircle)
                return false;

            // Group the vertices by distance to the center, then order ascending the groups by the distance to the center and
            // select the first group i.e the group of vertices having the same distance from the center with lower distance with respect 
            // to the other group of vertices
            var groupOfVerWithLowerDistToCenter = vertices.GroupBy(v => v.DistanceTo(center)).OrderBy(g => g.Key).First();
            double innerRadius = groupOfVerWithLowerDistToCenter.Key,
                   ta = outerRadius - innerRadius;

            wp = new EyeWorkPiece("R", boxWidth, ta, 0, 0, 0);

            return true;
        }

        private static bool IsLProfile(List<Point2D> vertices, double boxWidth, double boxHeight, double tolLinear,
                                       out EyeWorkPiece wp, out bool isVerticalPlaneInOrigin)
        {
            Points2DComparer equalityComparer = new Points2DComparer(tolLinear);
            wp = null;
            isVerticalPlaneInOrigin = false;
            // Extract the vertices laying in the lines with y = 0 and y = boxHeight
            List<Point2D> vertOnBottom = vertices.Where(v => v.Y.IsEqualTo(0, tolLinear)).ToList(),
                          vertOnTop = vertices.Where(v => v.Y.IsEqualTo(boxHeight, tolLinear)).ToList();

            // Extract the x of the point layng on the TA distance
            // Extract the greatest x coordinate of the points laying on the top line having x < boxWidth / 2 and x > boxWidth / 2,
            // if the vertices belong to a L profile the greatest x correspond to the thickness of the flange
            List<Point2D> pointsOnHalfRight = vertOnTop.Where(v => v.X < boxWidth / 2 && v.X.IsGreaterThan(0, tolLinear))?.OrderByDescending(v => v.X)?.ToList(),
                          pointsOnHalfLeft = vertOnTop.Where(v => v.X > boxWidth / 2 && v.X.IsGreaterThan(0, tolLinear))?.OrderByDescending(v => v.X)?.ToList();

            bool arePointsOnTopRightPresent = !(pointsOnHalfRight is null || pointsOnHalfRight.Count == 0),
                arePointsOnTopLeftPresent = !(pointsOnHalfLeft is null || pointsOnHalfLeft.Count == 0);
            if (arePointsOnTopLeftPresent && !arePointsOnTopRightPresent)
                isVerticalPlaneInOrigin = false;
            else if (!arePointsOnTopLeftPresent && arePointsOnTopRightPresent)
                isVerticalPlaneInOrigin = true;
            else
                return false;

            // If there are vertices in the top left quadrant it is not a L profile (in case of isVerticalPlaneOnRight)
            List<Point2D> pointsOnHalfTopHalfLeft = isVerticalPlaneInOrigin ? vertices.Where(v => v.Y > boxHeight / 2 && v.X > boxWidth / 2)?.OrderByDescending(v => v.X)?.ToList():
                                                                             vertices.Where(v => v.Y > boxHeight / 2 && v.X < boxWidth / 2)?.OrderByDescending(v => v.X)?.ToList();

            if (pointsOnHalfLeft is null || pointsOnHalfLeft.Count > 0)
                return false;

            double? taRef = arePointsOnTopRightPresent ? pointsOnHalfRight?.FirstOrDefault()?.X : boxWidth - pointsOnHalfLeft?.LastOrDefault()?.X;
            if (taRef is null)
                return false;
            double ta = taRef.Value,
                   thFlange = arePointsOnTopRightPresent ? ta : boxWidth - ta;

            // Extract all the vert laying on the flange thickness
            List<Point2D> vertOnThFlange = vertices.Where(v => v.X.IsEqualTo(thFlange, tolLinear * 0.1))?.ToList();

            if (vertOnThFlange is null || vertOnThFlange.Count < 2)
                return false;

            ToleranceEqualityComparer tolComparer = new ToleranceEqualityComparer(tolLinear);
            // Extract the vertices having the x greater than the x of the right flange (TA)
            // in this area we will find vertices belonging to the radius or to the web:
            // To skip vertices belonging to the radius area we will take the vertices having the x closer to boxWidth / 2
            // To skip the vertices on the chamfer we will take the vertices having the y closer to boxHeight
            var verticesInsideTheWeb = arePointsOnTopRightPresent ? vertices.Where(v => v.X.IsGreaterThan(thFlange, tolLinear)).ToList() :
                                                                    vertices.Where(v => v.Y.IsLessThan(thFlange, tolLinear)).ToList();
            // If there are vertices in  the web area having the y greater than boxHeight / 2 the profile is not U
            if (verticesInsideTheWeb.Any(v => v.Y > boxHeight / 2))
                return false;

            // Group the vertices by y coordinate and discard the groups that contain less than 2 vertices
            var verticesGroupedBySameY = verticesInsideTheWeb.GroupBy(v => v.Y, tolComparer).Where(g => g.Count() >= 2).ToList();
            // Order the groups by the number of elements in them and then by the distance from boxHeight / 2
            // the vertices laying on the web thickness will be the group having the highest number of elements and for the groups
            // having the same number of elements select the one closer to half height
            var verticesLayingOnTa = verticesGroupedBySameY.OrderBy(g => g.Count()).ThenBy(g => Math.Abs(g.Key - boxHeight / 2)).FirstOrDefault()?.ToList();

            if (verticesLayingOnTa is null || verticesLayingOnTa.Count < 2)
                return false;

            // Once obtained the list TB will be the y coordinate of the first one in the list
            double tb = verticesLayingOnTa.First().Y;
            if (tb.IsEqualTo(boxHeight / 2, tolLinear))
                return false;

            double webThFlangeWthRatio = tb / boxHeight;
            double limitRatio = 0.2; // should be max 1.6 from online research

            if (tb == 0 || webThFlangeWthRatio.IsGreaterThan(limitRatio, tolLinear))
                return false;

            double radius = Math.Abs(tb - vertOnThFlange.OrderBy(v => Math.Abs(tb - v.Y)).First().Y);

            wp = new EyeWorkPiece("L", boxWidth, ta, boxHeight, tb, radius);
            return true;
        }

        /// <summary>
        /// Compute a Dicitionary of goups where the key is the thickness of the group and the element is the lis of groups having that thickness
        /// </summary>
        /// <param name="igroupingList">
        /// List of groups where the key is the coordinate that have in common the 2D points inside the group
        /// </param>
        /// <param name="boxDimension"></param>
        /// <returns></returns>
        static Dictionary<double, List<IGrouping<double, Point2D>>> GroupByThickness(List<IGrouping<double, Point2D>> igroupingList, double boxDimension)
        {
            Dictionary<double, List<IGrouping<double, Point2D>>> result = new Dictionary<double, List<IGrouping<double, Point2D>>>();

            foreach (var group in igroupingList)
            {
                double distance = Math.Round(group.Key > boxDimension / 2 ?  Math.Abs(group.Key - boxDimension) : group.Key);

                if (!result.ContainsKey(distance))
                {
                    result[distance] = new List<IGrouping<double, Point2D>>();
                }

                result[distance].Add(group);
            }

            result = result.Where(kvp => kvp.Value.Count == 2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return result;
        }

        public class Points2DComparer : IEqualityComparer<Point2D>
        {
            public double Tol { get; set; }
            public Points2DComparer(double tol)
            {
                Tol = tol;
            }

            public bool Equals(Point2D p1, Point2D p2)
            {
                if (p1 is null || p2 is null)
                    return false;
                if (p1.Equals(p2))
                    return true;

                if (p1.X.IsEqualTo(p2.X, Tol) &&
                    p1.Y.IsEqualTo(p2.Y, Tol))
                    return true;
                else
                    return false;
            }

            public int GetHashCode(Point2D obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + obj.X.GetHashCode();
                    hash = hash * 23 + obj.Y.GetHashCode();
                    return hash;
                }
            }
        }
        public class ToleranceEqualityComparer : IEqualityComparer<double>
        {
            public double Tolerance { get; set; }
            public bool Equals(double x, double y)
            {
                return x - Tolerance <= y && x + Tolerance > y;
            }

            public ToleranceEqualityComparer(double tol)
            {
                Tolerance = tol;
            }

            //This is to force the use of Equals methods.
            public int GetHashCode(double obj) => 1;
        }
    }
}
