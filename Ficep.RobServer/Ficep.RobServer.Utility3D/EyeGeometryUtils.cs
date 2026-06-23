using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Ficep.RobServer.Data;
using Ficep.Utils;
using static System.Net.Mime.MediaTypeNames;
using QUT.Gppg;

namespace Ficep.RobServer.Utility3D
{
    public class EyeGeometryUtils
    {
        /// <summary>
        /// Creates an arc connecting 2 adjacent lines in correspondence of the common vertex
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <param name="Radius"></param>
        public static bool AddArc(ref List<ICurve> curves, ref Line l1, ref Line l2, double Radius)
        {
            Arc arc = null;

            if (!Curve.Fillet(l1, l2, Radius, false, false, true, true, out arc))
                return false;

            curves.Add(arc);

            return true;
        }

        public static bool AddArc(ref Line l1, ref Line l2, double Radius, out Arc arc)
        {
            arc = null;

            if (!Curve.Fillet(l1, l2, Radius, false, false, true, true, out arc))
                return false;

            return true;
        }
        public static bool AddExagon()
        {
            return false; 
        }
        //
        //  Applica l'offeset Y ai punti delle ali per riportare le coordinate al centro del piano (serve per il mirroring)
        //
        private static bool ApplyOffsetYMirroring (in List<ProgramPoint> macroPoint, in IWorkPiece wp, in string extrusionPlane)
        {
            double offsetY = 0;

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            if (verticalPlane)
            {
                if (wp.Prf.CodePrf == "I")
                    offsetY = -wp.Prf.SB / 2;

                foreach (var p in macroPoint)
                    p.Y += offsetY;
            }

            return true;
        }

        //
        //  Applica l'offeset Y al punto passato per riportare le coordinate al centro del piano (serve per il mirroring)
        //  Si usa questo overload per AddCircleExtrusion e AddSlotExtrusion
        //
        private static bool ApplyOffsetYMirroring(in IWorkPiece wp, in string extrusionPlane, ref Point2D point)
        {
            double offsetY = 0;

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            if (verticalPlane)
            {
                if (wp.Prf.CodePrf == "I")
                    offsetY = -wp.Prf.SB / 2;

                point.Y += offsetY;
            }

            return true;
        }

        //
        //  Calcolo gli offset da applicare al volume estruso da sottrarre in funzione del workpiece e del piano
        //
        public static bool GetSolidSubtractOffsetAmount (in IWorkPiece wp, in string extrusionPlane, in double extrusionDepth, in double tolBrep, in double tolAngle, in double surplus, 
            ref Vector3D amount, ref double offsetX, ref double offsetY, ref double offsetZ, double verticalAxisRadAngle = 0, double normalAxisRadAngle = 0)
        {
            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";
            double amountY = 0, amountZ = 0;

            if (horizontalPlane)
            {
                amountZ = extrusionDepth;

                if (verticalAxisRadAngle.IsEqualTo(0, tolAngle))
                    amount = new Vector3D(0, 0, extrusionDepth);
                else
                    amount = new Vector3D(0, 0, extrusionDepth/Math.Cos(verticalAxisRadAngle));

                Rotation verticalRot = new Rotation(verticalAxisRadAngle, Vector3D.AxisY);
                Rotation normalRot = new Rotation(normalAxisRadAngle, Vector3D.AxisZ);
                amount.TransformBy(verticalRot);
                amount.TransformBy(normalRot);
            }
            else
            {
                amountY = extrusionDepth;

                if(verticalAxisRadAngle.IsEqualTo(0, tolAngle))
                    amount = new Vector3D(0, extrusionPlane == "A" ? extrusionDepth : -extrusionDepth, 0);
                else
                    amount = new Vector3D(0, extrusionPlane == "A" ? extrusionDepth / Math.Cos(verticalAxisRadAngle) : -extrusionDepth / Math.Cos(verticalAxisRadAngle), 0);

                Rotation verticalRot = extrusionPlane == "A" ? new Rotation(verticalAxisRadAngle, Vector3D.AxisZ) : new Rotation(-verticalAxisRadAngle, Vector3D.AxisZ);
                Rotation normalRot = new Rotation(normalAxisRadAngle, Vector3D.AxisY);
                amount.TransformBy(verticalRot);
                amount.TransformBy(normalRot);
            }

            offsetX = offsetY = offsetZ = 0;

            if (horizontalPlane)
            {
                if (extrusionPlane == "C")
                {
                    if (wp.Prf.CodePrf == "F" || wp.Prf.CodePrf == "L" || wp.Prf.CodePrf == "U")
                    offsetZ = -surplus;
                    else if (wp.Prf.CodePrf == "Q")
                        offsetZ = extrusionDepth <= wp.Prf.SB / 2 ? wp.Prf.SB - wp.Prf.TA / 2 - amount.Z / 2 : wp.Prf.SB / 2 - amount.Z / 2;
                    else
                    {
                        offsetZ = wp.Prf.SB / 2 - amount.Z / 2;

                        // Calcolo degli offsetX e offsetY se la direzione di estrusione è differente da quella normale  
                        // in modo tale da ottenere una sottrazione nella posizione corretta
                        // TODO implementata solo per profili ad I
                        if (!verticalAxisRadAngle.IsEqualTo(0, tolAngle))
                        {
                            if (!normalAxisRadAngle.IsEqualTo(0, tolAngle))
                            {
                                offsetX = -(amount.Z / 2 + wp.Prf.TA / 2) * Math.Tan(verticalAxisRadAngle) * Math.Cos(normalAxisRadAngle);
                                offsetY = -(amount.Z / 2 + wp.Prf.TA / 2) * Math.Tan(verticalAxisRadAngle) * Math.Sin(normalAxisRadAngle);
                            }
                            else
                                offsetX = -(amount.Z / 2 + wp.Prf.TA / 2) * Math.Tan(verticalAxisRadAngle);
                        }
                    }
                }
                else if (extrusionPlane == "B" && wp.Prf.CodePrf == "L")
                {
                    offsetZ = -surplus;
                }
                else if (extrusionPlane == "D")
                {
                    offsetZ = -surplus;
                }
            }
            else
            {
                // Calcolo degli offsetX e offsetZ se la direzione di estrusione è differente da quella normale  
                // in modo tale da ottenere una sottrazione nella posizione corretta
                // TODO implementata solo per profili ad I
                if (!verticalAxisRadAngle.IsEqualTo(0, tolAngle))
                {
                    if (!normalAxisRadAngle.IsEqualTo(0, tolAngle))
                    {
                        offsetX = surplus * Math.Tan(verticalAxisRadAngle) * Math.Cos(normalAxisRadAngle);
                        offsetZ = -surplus * Math.Tan(verticalAxisRadAngle) * Math.Sin(normalAxisRadAngle);
                    }
                    else
                        offsetX = surplus * Math.Tan(verticalAxisRadAngle);
                }

                if (extrusionPlane == "A")
                {
                    offsetY = -surplus;
                }
                else if (extrusionPlane == "B")
                {
                    offsetY = wp.Prf.SA + surplus;
                }
            }

            offsetX += 2 * tolBrep;

            return true;
        }

        //
        //  Aggiunge TolWebFlange ai punti che giacciono sull'altezza degli edge del web e all'altezza del web + radius se la lista di punti è programmata sull'ala, 
        //  se la lista è programmata sul web agginge TolWebFlange ai punti che giacciono sull'ala A o B
        //
        public static bool ApplyTolWebFlange(in IWorkPiece wp, in string extrusionPlane, in double tolWebFlange, in double tolLinear, ref List<ProgramPoint> macroPoint)
        {
            for (int i = 0; i < macroPoint.Count; i++)
            {
                var py = macroPoint[i].Y;
                
                if (!ApplyTolWebFlange(wp, extrusionPlane, tolWebFlange, tolLinear, ref py))
                    return false;

                if (!macroPoint[i].Y.IsEqualTo(py, tolWebFlange / 2))
                {
                    int idxNext = i == macroPoint.Count - 1 ? 0 : i + 1,
                        idxPrev = i == 0 ? macroPoint.Count - 1 : i - 1;

                    macroPoint[i].Y = py;

                    if (macroPoint[idxNext].Radius > tolWebFlange)
                        macroPoint[idxNext].Radius -= tolWebFlange;

                    if (macroPoint[idxPrev].Radius > tolWebFlange)
                        macroPoint[idxPrev].Radius -= tolWebFlange;
                }

            }

            return true;
        }

        //
        //  Aggiunge TolWebFlange alla coordinata y se giace sull'altezza degli edge del web se la lista di punti è programmata sull'ala, 
        //  se la lista è programmata sul web agginge TolWebFlange alla cordinata y che giace sull'ala A o B.
        //  Il parametro radius ricevuto dalla funzione serve quando la coordinata y passata è la coordinata del centro di un cerchio/arco.
        //  Il parametro isOffsetyApplied indica quando è applicato l'offest sulla y dalla funzione ApplyOffsetYMirroring 
        //
        public static bool ApplyTolWebFlange(in IWorkPiece wp, in string extrusionPlane, in double tolWebFlange, in double tolLinear, ref double y, double radius = 0, bool isOffsetYApplied = true)
        {
            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = !horizontalPlane;

            if (horizontalPlane)
            {
                if (extrusionPlane == "C" || extrusionPlane == "D")
                {
                    // cordinate y dell'inizio ala A e B
                    double yFlangeA = wp.Prf.TB + radius, yFlangeB = wp.Prf.SA - wp.Prf.TB - radius;
                    // cordinate y dell'inizio del raccordo di raggio wp.prf.Radius tra web e flange
                    double yBottom = wp.Prf.TB + radius + wp.Prf.Radius, yUp = wp.Prf.SA - wp.Prf.TB - radius - wp.Prf.Radius;

                    y = y.IsEqualTo(yFlangeA, tolLinear) || y.IsEqualTo(yBottom, tolLinear) ? y + tolWebFlange :
                        y.IsEqualTo(yFlangeB, tolLinear) || y.IsEqualTo(yUp, tolLinear) ? y - tolWebFlange : y;
                }
                else if (extrusionPlane == "B" && wp.Prf.CodePrf == "L")
                {
                    // cordinate y dell'inizio ala A 
                    double yFlangeA = wp.Prf.TA + radius;
                    // cordinate y dell'inizio del raccordo di raggio wp.prf.Radius tra web e flange
                    double yBottom = wp.Prf.TA + radius + wp.Prf.Radius;

                    y = y.IsEqualTo(yFlangeA, tolLinear) || y.IsEqualTo(yBottom, tolLinear) ? y + tolWebFlange : y;
                }
            }
            else
            {
                if (wp.Prf.CodePrf == "I")
                {
                    // cordinate y degli edges superiore e inferiore del web con sistema di riferimento in y = 0
                    double yWebUp = wp.Prf.TA / 2 + radius + wp.Prf.SB / 2,
                           yWebDown = -wp.Prf.TA / 2 - radius + wp.Prf.SB / 2;
                    // cordinate y dell'inizio del raccordo di raggio wp.prf.Radius tra web e flange
                    double yUp = wp.Prf.TA / 2 + radius + wp.Prf.Radius + wp.Prf.SB / 2,
                           yDown = -wp.Prf.TA / 2 - radius - wp.Prf.Radius + wp.Prf.SB / 2;

                    y = y.IsEqualTo(yWebUp, tolLinear) || y.IsEqualTo(yUp, tolLinear) ? y + tolWebFlange :
                        y.IsEqualTo(yWebDown, tolLinear) || y.IsEqualTo(yDown, tolLinear) ? y - tolWebFlange : y;
                }
                else if (wp.Prf.CodePrf == "U")
                {
                    // cordinate y dell'edge superiore del web con sistema di riferimento in y = 0
                    double yWebUp = wp.Prf.TA + radius;
                    // cordinate y dell'inizio del raccordo di raggio wp.prf.Radius tra web e flange
                    double yUp = wp.Prf.TA + radius + wp.Prf.Radius;

                    y = y.IsEqualTo(yWebUp, tolLinear) || y.IsEqualTo(yUp, tolLinear) ? y + tolWebFlange : y;
                }
                else if (wp.Prf.CodePrf == "L")
                {
                    // cordinate y dell'edge superiore del web con sistema di riferimento in y = 0
                    double yWebUp = wp.Prf.TB + radius;
                    // cordinate y dell'inizio del raccordo di raggio wp.prf.Radius tra web e flange
                    double yUp = wp.Prf.TB + radius + wp.Prf.Radius;

                    y = y.IsEqualTo(yWebUp, tolLinear) || y.IsEqualTo(yUp, tolLinear) ? y + tolWebFlange : y;
                }
                else if (wp.Prf.CodePrf == "Q")
                {
                    // cordinate y dell'edge superiore del piano D e dell'edge inferiore del piano c con sistema di riferimento in y = 0
                    double yWebD = wp.Prf.TA + radius, yWebC = wp.Prf.SB - wp.Prf.TA - radius;
                    // cordinate y dell'inizio del raccordo di raggio wp.prf.Radius tra web e flange
                    double yD = wp.Prf.TA + radius + wp.Prf.Radius, yC = wp.Prf.SB - wp.Prf.TA - radius - wp.Prf.Radius;

                    y = y.IsEqualTo(yWebD, tolLinear) ? y + tolWebFlange : y.IsEqualTo(yWebC, tolLinear) ? y - tolWebFlange: y;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates an arc connecting 2 adjacent lines in correspondence of the common vertex
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <param name="Radius"></param>
        public static bool AddArc(ref List<IEyeCurve> curves, string side, EyeContourLine l1, EyeContourLine l2, double Radius, double tolLinear)
        {
            Arc arc = null;

            if (!Curve.Fillet(l1, l2, Radius, false, false, true, true, out arc))
                return false;

            if (l1.Length().IsEqualTo(Radius, tolLinear))
                curves.Remove(l1);
            if (l2.Length().IsEqualTo(Radius, tolLinear))
                curves.Remove(l2);
        
            curves.Add(new EyeContourArc(arc, side));

            return true;
        }

        /// <summary>
        /// Creates an arc connecting 2 adjacent lines in correspondence of the common vertex
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <param name="Radius"></param>
        private static bool AddArc(ref List<ICurve> curves, Line l1, Line l2, double Radius, double tolLinear)
        {
            Arc arc = null;

            if (!Curve.Fillet(l1, l2, Radius, false, false, true, true, out arc))
                return false;

            if (l1.Length().IsEqualTo(Radius, tolLinear))
                curves.Remove(l1);
            if (l2.Length().IsEqualTo(Radius, tolLinear))
                curves.Remove(l2);

            curves.Add(arc);

            return true;
        }

        /// <summary>
        /// Creates an arc in the XY plane connecting 2 points.
        /// If the radius is  > 0 the arc from p1 to p2 is created counterclockwise
        /// </summary>
        /// <param name="extrusionPlane"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="radius"></param>
        /// <param name="tolLinear"></param>
        /// <param name="arc"></param>
        /// <returns></returns>
        private static bool AddArc(Point3D p1, Point3D p2, string side, double radius, double tolLinear, out IEyeCurve arc)
        {
            arc = null;

            if (radius.IsEqualTo(0, tolLinear))
                return false;

            double absRadius = Math.Abs(radius);
            Plane plane = Plane.XY;

            // Find the centers of the two circumference passing for p1, p2 and having radius = absRadius
            FindCircumferenceCenters(plane, p1, p2, absRadius, out Point3D c1, out  Point3D c2);

            //
            // Check if arc1 is counterclockwise (ccw)
            //

            // Check if the two points define an arc of 180 degrees
            IEyeCurve arc1,
                   arc2;

           if(p1.DistanceTo(p2).IsEqualTo(2 * absRadius, tolLinear))
            {
                // If the distance from p1 to p2 is equal to the diameter and FindCircumferenceCenters finds 2 centers 
                // take the media from the 2 found centers as center in order to reduce the error.
                // Usually this happens when tolWebFlange is applied to one point and not to the other.
                if (c2 != null)
                {
                    c1 = (c1 + c2) / 2;
                }

                // Compute the other 2 points on the circumference defining an arc of 90 degrees
                Point3D p3 = p1.Clone() as Point3D,
                        p4 = p2.Clone() as Point3D;
                Vector3D rotationAxis = plane == Plane.XY ? Vector3D.AxisZ : Vector3D.AxisY;
                Rotation rot = new Rotation(Math.PI / 2, rotationAxis, c1);
                p3.TransformBy(rot);
                p4.TransformBy(rot);


                EyeContourArc arc11 = new EyeContourArc(c1, p1, p3, side),// Split the first arc in two arcs
                    arc12 = new EyeContourArc(c1, p3, p2, side);// of 90 degrees
                EyeContourArc arc21 = new EyeContourArc(c1, p1, p4, side),// Split the second arc in two arcs
                    arc22 = new EyeContourArc(c1, p4, p2, side);// of 90 degrees

                arc1 = new EyeContourCompositeCurve(side, arc11, arc12);
                arc2 = new EyeContourCompositeCurve(side, arc21, arc22);
            }
            else
            {
                arc1 = new EyeContourArc(c1, p1, p2, side);
                arc2 = new EyeContourArc(c2, p1, p2, side);
            }

            Point3D mid1 = arc1.PointAt(arc1.Domain.Mid);

            Vector3D v1 = new Vector3D(p1, mid1), 
                     v2 = new Vector3D(mid1, p2);

            Vector3D cross = Vector3D.Cross(v1, v2);

            bool ccw = cross.Z > 0;

            if (ccw && radius > 0)
                arc = arc1;
            else 
                arc = arc2;

            return true;
        }
        private static bool AddArc(Point3D p1, Point3D p2, string extrusionPlane, double radius, double tolLinear, out ICurve arc)
        {
            arc = null;

            if (radius.IsEqualTo(0, tolLinear))
                return false;

            double absRadius = Math.Abs(radius);
            // Il piano è messo di default a XY, questo serve per le funzioni come addContourExtrusion che poi fanno il mirror delle curve.
            // mentre per le funzioni come la addExternalChamfer che non fanno il mirror delle curve va passato il piano
            Plane plane = Plane.XY;
            if (extrusionPlane != string.Empty)
            {
                if (extrusionPlane == "C")
                    plane = Plane.XY;
                else
                {
                    plane = Plane.XZ;

                    if (!p1.Y.IsEqualTo(0,tolLinear))
                        plane.Translate(0, p1.Y);
                }
                    
            }
            // Find the centers of the two circumference passing for p1, p2 and having radius = absRadius
            FindCircumferenceCenters(plane, p1, p2, absRadius, out Point3D c1, out Point3D c2);

            //
            // Check if arc1 is counterclockwise (ccw)
            //

            // Check if the two points define an arc of 180 degrees
            ICurve arc1,
                   arc2;

            if (p1.DistanceTo(p2).IsEqualTo(2 * absRadius, tolLinear))
            {
                c1 = new Line(p1, p2).MidPoint;
                // If the distance from p1 to p2 is equal to the diameter and FindCircumferenceCenters finds 2 centers 
                // take the media from the 2 found centers as center in order to reduce the error.
                // Usually this happens when tolWebFlange is applied to one point and not to the other.
                //if (c2 != null)
                //{
                //    c1 = (c1 + c2) / 2;
                //}

                // Compute the other 2 points on the circumference defining an arc of 90 degrees
                Point3D p3 = p1.Clone() as Point3D,
                        p4 = p2.Clone() as Point3D;
                Vector3D rotationAxis = plane == Plane.XY ? Vector3D.AxisZ : Vector3D.AxisY;
                Rotation rot = new Rotation(Math.PI / 2, rotationAxis, c1);
                p3.TransformBy(rot);
                p4.TransformBy(rot);


                Arc arc11 = new Arc(c1, p1, p3),// Split the first arc in two arcs
                    arc12 = new Arc(c1, p3, p2);// of 90 degrees
                Arc arc21 = new Arc(c1, p1, p4),// Split the second arc in two arcs
                    arc22 = new Arc(c1, p4, p2);// of 90 degrees

                arc1 = new CompositeCurve(arc11, arc12);
                arc2 = new CompositeCurve(arc21, arc22);
            }
            else
            {
                arc1 = new Arc(c1, p1, p2);
                arc2 = new Arc(c2, p1, p2);
            }

            Point3D mid1 = arc1.PointAt(arc1.Domain.Mid);

            Vector3D v1 = new Vector3D(p1, mid1),
                     v2 = new Vector3D(mid1, p2);

            Vector3D cross = Vector3D.Cross(v1, v2);

            bool ccw = extrusionPlane == "C" || extrusionPlane == string.Empty ? cross.Z > 0 : extrusionPlane == "A" ? cross.Y < 0 : cross.Y > 0;

            if ((ccw && radius > 0) || (!ccw && radius < 0))
                arc = arc1;
            else
                arc = arc2;

            return true;
        }
        /// <summary>
        /// Compute the two centers of the circumference having the same radius and passing for p1 and p2.
        /// </summary>
        /// <param name="plane">Plane in which p1, p2 lay </param>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        /// <param name="radius"></param>
        /// <param name="c1">First Center</param>
        /// <param name="c2">Second Center</param>
        /// <returns></returns>
        public static bool FindCircumferenceCenters(Plane plane, Point3D p1, Point3D p2, double radius, out Point3D c1, out Point3D c2)
        {
            Circle circle1 = new Circle(plane, p1, radius),
                   circle2 = new Circle(plane, p2, radius);

            Utility.IntersectionCircleCircle(circle1, circle2, plane, out c1, out c2);

            return true;
        }

        //
        //  Lo scopo è avere una lista di punti non duplicati
        //  Questo ha senso perchè i punti del contorno hanno il punto finale distinto dall'iniziale.
        //  Essendo poi contorni chiusi per definizione, non posso avere più connessioni al suo interno
        //  allo stesso punto
        //
        private static List<ProgramPoint> RemoveDuplicates(List<ProgramPoint> points)
        {
            // Usa Distinct per ottenere i punti unici
            List<ProgramPoint> uniquePoints = points.Distinct().ToList();

            return uniquePoints;
        }

        //
        //  Applica in automatico ai punti del contorno CHIUSO un allargamento in X/Y del valore surplus laddove necessario
        //  per garantire il successo delle funzioni Eyeshot 
        //
        public static bool ApplyContourSurplusXY(in List<ProgramPoint> macroPoint, double widthPlane, double surplus, in double tolLinear, out List<ProgramPoint> pointsWithSurplus)
        {
            // Surplus che viene applicato agli archi quando sono sugli edge
            double arcSurplus = 0.1;
            // Lista clonata da macroPoint a cui verranno applicati i surplus
            pointsWithSurplus = macroPoint.Select(m => m.Clone() as ProgramPoint).ToList();

            if (surplus <= 0)
                return true;

            double YMin = macroPoint.Min(p => p.Y), YMax = macroPoint.Max(p => p.Y);

            ProgramPoint originalP = null, prev = null, next = null,
                         computedP;
            bool firstPoint = false, lastPoint = false;

            for (int idx = 0; idx < macroPoint.Count; idx++)
            {
                firstPoint = idx == 0;
                lastPoint = idx == (macroPoint.Count - 1);

                // Punto originale che non verrà modificato
                originalP = macroPoint[idx];
                // Punto che sarà modificato se bisognerà applicare un delta altrimenti rimmarrà con le coordinate del punto originale
                computedP = pointsWithSurplus[idx];

                if (firstPoint)
                    prev = macroPoint[macroPoint.Count - 1];
                else
                    prev = macroPoint[idx - 1];

                if (lastPoint)
                    next = macroPoint[0];
                else
                    next = macroPoint[idx + 1];

                bool pointOnXZero = originalP.X.IsEqualTo(0, tolLinear),
                     pointOnYZero = originalP.Y.IsEqualTo(0, tolLinear),
                     pointOnYMax = originalP.Y.IsEqualTo(widthPlane, tolLinear),
                     pointOnContourYMinYMax = originalP.Y.IsEqualTo(YMin, tolLinear) || originalP.Y.IsEqualTo(YMax, tolLinear);

                // Se il punto non è in x = 0 o y = 0 o y = widthPlane allora vuol dire che il punto è interno al piano e non lo devo 
                // modificare
                if (!pointOnXZero && !pointOnYZero && !pointOnYMax)
                    continue;

                bool isPrevWithRadius = false, isCurrWithRadius = false, isNextWithRadius = false;
                double prevm = 0, nextm = 0;
                double prevdx = originalP.X - prev.X, prevdy = originalP.Y - prev.Y;
                double nextdx = next.X - originalP.X, nextdy = next.Y - originalP.Y;

                // Controllo se il punto precedente, corrente, successivo hanno il raggio
                if (!next.Radius.IsEqualTo(0, tolLinear))
                    isNextWithRadius = true;
                if (!originalP.Radius.IsEqualTo(0, tolLinear))
                    isCurrWithRadius = true;
                if (!prev.Radius.IsEqualTo(0, tolLinear))
                    isPrevWithRadius = true;
                
                
                if (!prevdx.IsEqualTo(0, tolLinear))
                    prevm = prevdy / prevdx;
                else 
                   prevm = double.PositiveInfinity;

                if (!nextdx.IsEqualTo(0, tolLinear))
                    nextm = nextdy / nextdx;
                else
                    nextm = double.PositiveInfinity;

                bool isPrevmInfinity = Math.Abs(prevm) > 50,
                     isNextmInfinity = Math.Abs(nextm) > 50,
                     isPrevmZero = prevm.IsEqualTo(0, tolLinear),
                     isNextmZero = nextm.IsEqualTo(0, tolLinear),
                     isPrevmSloped = !isPrevmInfinity && !isPrevmZero,
                     isNextmSloped = !isNextmInfinity && !isNextmZero;

                bool isSurplusAppliedY = true;

                double dx = 0, dy = 0;

                // Se il punto è in y = 0 o in y = widthPlane sottraggo e sommo rispettivamente surplus e calcolo
                // il delta x successivamente (nei casi in cui va calcolato i.e. retta pendente)
                if (pointOnYZero)
                {
                    if (isNextWithRadius || isPrevWithRadius || isCurrWithRadius)
                        dy = -arcSurplus;
                    else
                        dy = -surplus;

                    computedP.Y = dy;
                    isSurplusAppliedY = true;
                }
                else if (pointOnYMax)
                {
                    if (isNextWithRadius || isPrevWithRadius || isCurrWithRadius)
                        dy = arcSurplus;
                    else
                        dy = surplus;

                    computedP.Y += dy;
                    isSurplusAppliedY = true;
                }
                else 
                {
                    //
                    //  Ai punti interni al contorno in direzione Y (interni all'intervallo (YMin, YMax))
                    //  applico solo metà del surplus per garantire che il contorno chiuso generato sulle
                    //  estremità non degeneri mai in uno spessore 0
                    //
                    if (pointOnContourYMinYMax)
                    {
                        if (isNextWithRadius || isPrevWithRadius || isCurrWithRadius)
                            dx = -arcSurplus;
                        else
                            dx = -surplus;
                    }
                    else
                    {
                        if (isNextWithRadius || isPrevWithRadius || isCurrWithRadius)
                            dx = -arcSurplus / 2;
                        else
                            dx = -surplus / 2;
                    }
                    isSurplusAppliedY = false;
                }

                // Caso in cui il punto è in x = 0 e si trova in un corner di 90 gradi 
                if (pointOnXZero && (isPrevmInfinity && isNextmZero || isNextmInfinity && isPrevmZero))
                {
                    //
                    //  Ai punti interni al contorno in direzione Y (interni all'intervallo (YMin, YMax))
                    //  applico solo metà del surplus per garantire che il contorno chiuso generato sulle
                    //  estremità non degeneri mai in uno spessore 0
                    //
                    if (pointOnContourYMinYMax)
                    {
                        if (isNextWithRadius || isPrevWithRadius || isCurrWithRadius)
                            dx = -arcSurplus;
                        else
                            dx = -surplus;
                    }
                    else
                    { 
                        if (isNextWithRadius || isPrevWithRadius || isCurrWithRadius)
                            dx = -arcSurplus / 2;
                        else
                            dx = -surplus / 2;
                    }

                    computedP.X = dx;
                }
                else
                {
                    //  Se prima e dopo ho dei tratti Slopped esco senza applicare surplus
                    if (isPrevmSloped && isNextmSloped)
                        continue;

                    if (isSurplusAppliedY)
                    { 
                        if (isPrevmSloped)
                                computedP.X += dy / prevm;
                        else if(isNextmSloped)
                                computedP.X += dy / nextm;
                    }
                    else
                    {
                        computedP.X = dx;

                        if (isPrevmSloped)
                            computedP.Y += dx * prevm;
                        else if (isNextmSloped)
                            computedP.Y += dx * nextm;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Applies a surplus to the contour points laying on the bondingbox contour along either the XZ or XY plane.
        /// </summary>
        /// <param name="macroPoint">The original list of program points representing the contour.</param>
        /// <param name="planeWidth">The width of the plane (Z for XZ plane, Y for XY plane).</param>
        /// <param name="planeLength">The length of the plane (X for both planes).</param>
        /// <param name="surplus">The amount of surplus to apply.</param>
        /// <param name="tolLinear">The linear tolerance for comparisons.</param>
        /// <param name="applyToXZ">If true, surplus is applied in the XZ plane (using Z coordinate). If false, it's applied in the XY plane (using Y coordinate).</param>
        /// <param name="pointsWithSurplus">The output list of points with the surplus applied.</param>
        /// <returns>True if the operation was successful, false otherwise (though currently always returns true).</returns>
        public static bool ApplyContourBBSurplus(in List<ProgramPoint> macroPoint, double surplus, double tolLinear, bool applyToXZ, out List<ProgramPoint> pointsWithSurplus)
        {
            // Surplus that is applied to arcs when they are on the edges
            double arcSurplus = 0.2;
            // Cloned list from macroPoint to which surpluses will be applied
            pointsWithSurplus = macroPoint.Select(m => m.Clone() as ProgramPoint).ToList();

            if (surplus <= 0)
                return true;


            // Determine which coordinate to use based on applyToXZ
            Func<ProgramPoint, double> getPlaneCoord = applyToXZ ? (p => p.Z) : (p => p.Y);
            Action<ProgramPoint, double> setPlaneCoord = applyToXZ ? ((p, val) => p.Z = val) : ((p, val) => p.Y = val);
            Func<ProgramPoint, double, double, bool> isOnPlaneEdge = applyToXZ ?
                ((p, min, max) => p.Z.IsEqualTo(min, tolLinear) || p.Z.IsEqualTo(max, tolLinear)) :
                ((p, min, max) => p.Y.IsEqualTo(min, tolLinear) || p.Y.IsEqualTo(max, tolLinear));

            double minPlaneHeightCoord = macroPoint.Min(getPlaneCoord),
                   maxPlaneHeightCoord = macroPoint.Max(getPlaneCoord),
                   minPlaneLengthCoord = macroPoint.Min(m => m.X),
                   maxPlaneLengthCoord = macroPoint.Max(m => m.X);

            ProgramPoint originalP = null, prev = null, next = null, computedP;
            bool firstPoint, lastPoint;

            for (int idx = 0; idx < macroPoint.Count; idx++)
            {
                firstPoint = idx == 0;
                lastPoint = idx == (macroPoint.Count - 1);

                // Original point that will not be modified
                originalP = macroPoint[idx];
                // Point that will be modified if a delta needs to be applied, otherwise it will remain with the original point's coordinates
                computedP = pointsWithSurplus[idx];

                prev = firstPoint ? macroPoint[macroPoint.Count - 1] : macroPoint[idx - 1];
                next = lastPoint ? macroPoint[0] : macroPoint[idx + 1];

                bool pointOnXZero = originalP.X.IsEqualTo(minPlaneLengthCoord, tolLinear);
                bool pointOnXMax = originalP.X.IsEqualTo(maxPlaneLengthCoord, tolLinear);
                bool pointOnPlaneHeightCoordZero = getPlaneCoord(originalP).IsEqualTo(minPlaneHeightCoord, tolLinear);
                bool pointOnPlaneHeightCoordMax = getPlaneCoord(originalP).IsEqualTo(maxPlaneHeightCoord, tolLinear);

                // If the point is not at X = 0 or PlaneCoord = 0 or PlaneCoord = planeWidth,
                // then the point is inside the plane and should not be modified
                if (!pointOnXZero && !pointOnXMax && !pointOnPlaneHeightCoordZero && !pointOnPlaneHeightCoordMax)
                    continue;

                bool isPrevWithRadius = !prev.Radius.IsEqualTo(0, tolLinear);
                bool isCurrWithRadius = !originalP.Radius.IsEqualTo(0, tolLinear);
                bool isNextWithRadius = !next.Radius.IsEqualTo(0, tolLinear);

                double prevdX = originalP.X - prev.X;
                double prevdPlaneCoord = getPlaneCoord(originalP) - getPlaneCoord(prev);
                double nextdX = next.X - originalP.X;
                double nextdPlaneCoord = getPlaneCoord(next) - getPlaneCoord(originalP);

                double prevm = 0;
                if (!prevdX.IsEqualTo(0, tolLinear))
                    prevm = prevdPlaneCoord / prevdX;
                else
                    prevm = double.PositiveInfinity;

                double nextm = 0;
                if (!nextdX.IsEqualTo(0, tolLinear))
                    nextm = nextdPlaneCoord / nextdX;
                else
                    nextm = double.PositiveInfinity;

                bool isPrevmInfinitPlaneCoord = Math.Abs(prevm) > 50; // Approximating infinity for slope
                bool isNextmInfinitPlaneCoord = Math.Abs(nextm) > 50; // Approximating infinity for slope
                bool isPrevmZero = prevm.IsEqualTo(0, tolLinear);
                bool isNextmZero = nextm.IsEqualTo(0, tolLinear);
                bool isPrevmSloped = !isPrevmInfinitPlaneCoord && !isPrevmZero;
                bool isNextmSloped = !isNextmInfinitPlaneCoord && !isNextmZero;

                bool isSurplusAppliedToPlaneCoord = true;
                double dx = 0, dPlaneCoord = 0;

                // If the point is at PlaneCoord = 0 or PlaneCoord = planeWidth,
                // subtract/add surplus respectively and then calculate delta X (if applicable, e.g., sloped line)
                if (pointOnPlaneHeightCoordZero)
                {
                    dPlaneCoord = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;
                    setPlaneCoord(computedP, dPlaneCoord);
                    isSurplusAppliedToPlaneCoord = true;
                }
                else if (pointOnPlaneHeightCoordMax)
                {
                    dPlaneCoord = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? arcSurplus : surplus;
                    setPlaneCoord(computedP, getPlaneCoord(computedP) + dPlaneCoord);
                    isSurplusAppliedToPlaneCoord = true;
                }
                else
                {
                    dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;

                    isSurplusAppliedToPlaneCoord = false;

                    if (pointOnXMax)
                        dx = -dx;
                }

                // Case where the point is at X = 0 and is in a 90-degree corner
                if (pointOnXZero && ((isPrevmInfinitPlaneCoord && isNextmZero) || (isNextmInfinitPlaneCoord && isPrevmZero)))
                {
                        dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;

                    computedP.X = dx;
                }
                else if (pointOnXMax && ((isPrevmInfinitPlaneCoord && isNextmZero) || (isNextmInfinitPlaneCoord && isPrevmZero)))
                {
                    dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? arcSurplus : surplus;

                    computedP.X += dx;
                }
                else if (pointOnXZero)
                {
                    // If there are sloped segments before and after, exit without applying surplus
                    if (isPrevmSloped && isNextmSloped)
                        continue;

                    if (isSurplusAppliedToPlaneCoord)
                    {
                        if (isPrevmSloped)
                            computedP.X += dPlaneCoord / prevm;
                        else if (isNextmSloped)
                            computedP.X += dPlaneCoord / nextm;
                    }
                    else
                    {
                        computedP.X = dx;
                        if (isPrevmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * prevm);
                        else if (isNextmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * nextm);
                    }
                }
                else if (pointOnXMax) // This condition was missing in the original logic, added for completeness
                {
                    // If there are sloped segments before and after, exit without applying surplus
                    if (isPrevmSloped && isNextmSloped)
                        continue;

                    if (isSurplusAppliedToPlaneCoord)
                    {
                        if (isPrevmSloped)
                            computedP.X += dPlaneCoord / prevm;
                        else if (isNextmSloped)
                            computedP.X += dPlaneCoord / nextm;
                    }
                    else
                    {
                        computedP.X += dx;
                        if (isPrevmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * prevm);
                        else if (isNextmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * nextm);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Applies a surplus to the contour points along either the XZ or XY plane.
        /// </summary>
        /// <param name="macroPoint">The original list of program points representing the contour.</param>
        /// <param name="planeWidth">The width of the plane (Z for XZ plane, Y for XY plane).</param>
        /// <param name="planeLength">The length of the plane (X for both planes).</param>
        /// <param name="surplus">The amount of surplus to apply.</param>
        /// <param name="tolLinear">The linear tolerance for comparisons.</param>
        /// <param name="applyToXZ">If true, surplus is applied in the XZ plane (using Z coordinate). If false, it's applied in the XY plane (using Y coordinate).</param>
        /// <param name="pointsWithSurplus">The output list of points with the surplus applied.</param>
        /// <returns>True if the operation was successful, false otherwise (though currently always returns true).</returns>
        public static bool ApplyContourSurplus(in List<ProgramPoint> macroPoint, double planeWidth, double planeLength, double surplus, double tolLinear, bool applyToXZ, out List<ProgramPoint> pointsWithSurplus)
        {
            // Surplus that is applied to arcs when they are on the edges
            double arcSurplus = 0.2;
            // Cloned list from macroPoint to which surpluses will be applied
            pointsWithSurplus = macroPoint.Select(m => m.Clone() as ProgramPoint).ToList();

            if (surplus <= 0)
                return true;


            // Determine which coordinate to use based on applyToXZ
            Func<ProgramPoint, double> getPlaneCoord = applyToXZ ? (p => p.Z) : (p => p.Y);
            Action<ProgramPoint, double> setPlaneCoord = applyToXZ ? ((p, val) => p.Z = val) : ((p, val) => p.Y = val);
            Func<ProgramPoint, double, double, bool> isOnPlaneEdge = applyToXZ ?
                ((p, min, max) => p.Z.IsEqualTo(min, tolLinear) || p.Z.IsEqualTo(max, tolLinear)) :
                ((p, min, max) => p.Y.IsEqualTo(min, tolLinear) || p.Y.IsEqualTo(max, tolLinear));

            double minPlaneCoord = macroPoint.Min(getPlaneCoord);
            double maxPlaneCoord = macroPoint.Max(getPlaneCoord);

            ProgramPoint originalP = null, prev = null, next = null, computedP;
            bool firstPoint, lastPoint;

            for (int idx = 0; idx < macroPoint.Count; idx++)
            {
                firstPoint = idx == 0;
                lastPoint = idx == (macroPoint.Count - 1);

                // Original point that will not be modified
                originalP = macroPoint[idx];
                // Point that will be modified if a delta needs to be applied, otherwise it will remain with the original point's coordinates
                computedP = pointsWithSurplus[idx];

                prev = firstPoint ? macroPoint[macroPoint.Count - 1] : macroPoint[idx - 1];
                next = lastPoint ? macroPoint[0] : macroPoint[idx + 1];

                bool pointOnXZero = originalP.X.IsEqualTo(0, tolLinear);
                bool pointOnXMax = originalP.X.IsEqualTo(planeLength, tolLinear);
                bool pointOnPlaneCoordZero = getPlaneCoord(originalP).IsEqualTo(0, tolLinear);
                bool pointOnPlaneCoordMax = getPlaneCoord(originalP).IsEqualTo(planeWidth, tolLinear);
                bool pointOnContourPlaneCoordMinMax = isOnPlaneEdge(originalP, minPlaneCoord, maxPlaneCoord);

                // If the point is not at X = 0 or PlaneCoord = 0 or PlaneCoord = planeWidth,
                // then the point is inside the plane and should not be modified
                if (!pointOnXZero && !pointOnXMax && !pointOnPlaneCoordZero && !pointOnPlaneCoordMax)
                    continue;

                bool isPrevWithRadius = !prev.Radius.IsEqualTo(0, tolLinear);
                bool isCurrWithRadius = !originalP.Radius.IsEqualTo(0, tolLinear);
                bool isNextWithRadius = !next.Radius.IsEqualTo(0, tolLinear);

                double prevdX = originalP.X - prev.X;
                double prevdPlaneCoord = getPlaneCoord(originalP) - getPlaneCoord(prev);
                double nextdX = next.X - originalP.X;
                double nextdPlaneCoord = getPlaneCoord(next) - getPlaneCoord(originalP);

                double prevm = 0;
                if (!prevdX.IsEqualTo(0, tolLinear))
                    prevm = prevdPlaneCoord / prevdX;
                else
                    prevm = double.PositiveInfinity;

                double nextm = 0;
                if (!nextdX.IsEqualTo(0, tolLinear))
                    nextm = nextdPlaneCoord / nextdX;
                else
                    nextm = double.PositiveInfinity;

                bool isPrevmInfinitPlaneCoord = Math.Abs(prevm) > 50; // Approximating infinity for slope
                bool isNextmInfinitPlaneCoord = Math.Abs(nextm) > 50; // Approximating infinity for slope
                bool isPrevmZero = prevm.IsEqualTo(0, tolLinear);
                bool isNextmZero = nextm.IsEqualTo(0, tolLinear);
                bool isPrevmSloped = !isPrevmInfinitPlaneCoord && !isPrevmZero;
                bool isNextmSloped = !isNextmInfinitPlaneCoord && !isNextmZero;

                bool isSurplusAppliedToPlaneCoord = true;
                double dx = 0, dPlaneCoord = 0;

                // If the point is at PlaneCoord = 0 or PlaneCoord = planeWidth,
                // subtract/add surplus respectively and then calculate delta X (if applicable, e.g., sloped line)
                if (pointOnPlaneCoordZero)
                {
                    dPlaneCoord = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;
                    setPlaneCoord(computedP, dPlaneCoord);
                    isSurplusAppliedToPlaneCoord = true;
                }
                else if (pointOnPlaneCoordMax)
                {
                    dPlaneCoord = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? arcSurplus : surplus;
                    setPlaneCoord(computedP, getPlaneCoord(computedP) + dPlaneCoord);
                    isSurplusAppliedToPlaneCoord = true;
                }
                else
                {
                    // For points inside the contour in the PlaneCoord direction (inside the (MinPlaneCoord, MaxPlaneCoord) interval),
                    // apply only half the surplus to ensure the closed contour generated at the ends never degenerates to zero thickness.
                    if (pointOnContourPlaneCoordMinMax)
                    {
                        
                        if (false)
                        {
                            // This is the original code used in the macro but for the dstv the isNextWithradius should not be used
                            dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;
                        }
                        dx = (isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;
                    }
                    else
                    {
                        if (false)
                        {
                            // This is the original code used in the macro but for the dstv the isNextWithradius should not be used
                            dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus / 2 : -surplus / 2;
                        }
                        dx = (isPrevWithRadius || isCurrWithRadius) ? -arcSurplus / 2 : -surplus / 2;
                    }
                    isSurplusAppliedToPlaneCoord = false;

                    if (pointOnXMax)
                        dx = -dx;
                }

                // Case where the point is at X = 0 and is in a 90-degree corner
                if (pointOnXZero && ((isPrevmInfinitPlaneCoord && isNextmZero) || (isNextmInfinitPlaneCoord && isPrevmZero)))
                {
                    // For points inside the contour in the PlaneCoord direction (inside the (MinPlaneCoord, MaxPlaneCoord) interval),
                    // apply only half the surplus to ensure the closed contour generated at the ends never degenerates to zero thickness.
                    if (pointOnContourPlaneCoordMinMax)
                    {
                        if (false)
                        {
                            // This is the original code used in the macro but for the dstv the isNextWithradius should not be used
                            dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;
                        }
                        dx = (isPrevWithRadius || isCurrWithRadius) ? -arcSurplus : -surplus;
                    }
                    else
                    {
                        if (false)
                        {
                            // This is the original code used in the macro but for the dstv the isNextWithradius should not be used
                            dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? -arcSurplus / 2 : -surplus / 2;
                        }
                        dx = (isPrevWithRadius || isCurrWithRadius) ? -arcSurplus / 2 : -surplus / 2;

                    }
                    computedP.X = dx;
                }
                else if (pointOnXMax && ((isPrevmInfinitPlaneCoord && isNextmZero) || (isNextmInfinitPlaneCoord && isPrevmZero)))
                {
                    // For points inside the contour in the PlaneCoord direction (inside the (MinPlaneCoord, MaxPlaneCoord) interval),
                    // apply only half the surplus to ensure the closed contour generated at the ends never degenerates to zero thickness.
                    if (pointOnContourPlaneCoordMinMax)
                    {
                        if (false)
                        {
                            // This is the original code used in the macro but for the dstv the isNextWithradius should not be used
                            dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? arcSurplus : surplus;
                        }
                        dx = (isPrevWithRadius || isCurrWithRadius) ? arcSurplus : surplus;
                    }
                    else
                    {
                        if (false)
                        {
                            // This is the original code used in the macro but for the dstv the isNextWithradius should not be used
                            dx = (isNextWithRadius || isPrevWithRadius || isCurrWithRadius) ? arcSurplus / 2 : surplus / 2;
                        }
                        dx = (isPrevWithRadius || isCurrWithRadius) ? arcSurplus / 2 : surplus / 2;
                    }
                    computedP.X += dx;
                }
                else if (pointOnXZero)
                {
                    // If there are sloped segments before and after, exit without applying surplus
                    if (isPrevmSloped && isNextmSloped)
                        continue;

                    if (isSurplusAppliedToPlaneCoord)
                    {
                        if (isPrevmSloped)
                            computedP.X += dPlaneCoord / prevm;
                        else if (isNextmSloped)
                            computedP.X += dPlaneCoord / nextm;
                    }
                    else
                    {
                        computedP.X = dx;
                        if (isPrevmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * prevm);
                        else if (isNextmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * nextm);
                    }
                }
                else if (pointOnXMax) // This condition was missing in the original logic, added for completeness
                {
                    // If there are sloped segments before and after, exit without applying surplus
                    if (isPrevmSloped && isNextmSloped)
                        continue;

                    if (isSurplusAppliedToPlaneCoord)
                    {
                        if (isPrevmSloped)
                            computedP.X += dPlaneCoord / prevm;
                        else if (isNextmSloped)
                            computedP.X += dPlaneCoord / nextm;
                    }
                    else
                    {
                        computedP.X += dx;
                        if (isPrevmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * prevm);
                        else if (isNextmSloped)
                            setPlaneCoord(computedP, getPlaneCoord(computedP) + dx * nextm);
                    }
                }
                else
                {
                    //  Se prima e dopo ho dei tratti Slopped esco senza applicare surplus
                    if (isPrevmSloped && isNextmSloped)
                        continue;

                    if (isSurplusAppliedToPlaneCoord)
                    {
                        if (isPrevmSloped)
                            computedP.X += dPlaneCoord / prevm;
                        else if (isNextmSloped)
                            computedP.X += dPlaneCoord / nextm;
                    }
                    else
                    {
                        computedP.X = dx;

                        if (isPrevmSloped)
                            computedP.Y += dx * prevm;
                        else if (isNextmSloped)
                            computedP.Y += dx * nextm;
                    }
                }
            }
            return true;
        }

        //
        //  Applica in automatico ai punti della linea su cui eseguire il cianfrino un allargamento in X/Y
        //  del valore surplus laddove necessario
        //
        private static bool ApplyChamferSurplus(ref ProgramPoint startPoint, ref ProgramPoint endPoint, double widthPlane, bool horizontalPlane, double surplus, in double tolLinear)
        {
            if (surplus <= 0)
                return true;

            if (horizontalPlane)
                return true;
            if (!horizontalPlane && !startPoint.X.IsEqualTo(endPoint.X, tolLinear) && startPoint.Y.IsEqualTo(endPoint.Y, tolLinear))
                return true;

            // Check if it is a line or an arc  
            bool isArc = startPoint.Radius > 0 || endPoint.ArcRadius > 0;
            ProgramPoint currentP = null, other = null;

            for (int idx = 0; idx < 2; idx++)
            {
                if (idx == 0)
                {
                    currentP = startPoint;
                    other = endPoint;
                }
                else
                {
                    currentP = endPoint;
                    other = startPoint;
                }

                bool pointOnXZero = currentP.X.IsEqualTo(0, tolLinear),
                     pointOnYZero = currentP.Y.IsEqualTo(0, tolLinear),
                     pointOnYMax = currentP.Y.IsEqualTo(widthPlane, tolLinear);

                // Se il punto non è in x = 0 o y = 0 o y = widthPlane allora vuol dire che il punto è interno al piano e non lo devo 
                // modificare
                if (!pointOnXZero && !pointOnYZero && !pointOnYMax)
                    continue;

                double prevm = 0, nextm = 0;
                double nextdx = other.X - currentP.X, nextdy = other.Y - currentP.Y;

                if (!nextdx.IsEqualTo(0, tolLinear))
                    nextm = nextdy / nextdx;
                else
                    nextm = double.PositiveInfinity;

                bool isNextmInfinity = Math.Abs(nextm) > 50,

                     isNextmZero = nextm.IsEqualTo(0, tolLinear * 0.1), // Ridotta la tolleranza di comparazione perchè 
                                                                        // ci sono macro che hanno una inclinazione anche di 0.1
                                                                        // ESTI149 ad esempio 
                     isNextmSloped = !isNextmInfinity && !isNextmZero;

                bool isSurplusAppliedY = false;

                double dx = surplus, dy = surplus;

                // Se il punto è in y = 0 o in y = widthPlane sottraggo e sommo rispettivamente surplus e calcolo
                // il delta x successivamente (nei casi in cui va calcolato i.e. retta pendente)
                if (pointOnYZero)
                {
                    dy = -surplus;
                    currentP.Y = dy;
                    isSurplusAppliedY = true;
                }
                else if (pointOnYMax)
                {
                    dy = surplus;
                    currentP.Y += dy;
                    isSurplusAppliedY = true;
                }

                if (isSurplusAppliedY)
                {
                    if (isNextmSloped && !isArc)
                    currentP.X += dy / nextm;
                }
                else
                {
                    if (isNextmSloped && !isArc)
                    {
                        currentP.X = -dx;
                        currentP.Y += -dx * nextm;
                    }
                }
            }
            return true;
        }
        //
        //  Versione dell'AddContourExtrusion che applica in automatico ai punti del contorno un allargamento
        //  in X/Y del valore surplus laddove necessario per garantire il successo delle funzioni Eyeshot
        //
        public static bool AddContourExtrusion(in List<ProgramPoint> macroPoint, string extrusionPlane, double extrusionDepth,
            bool mirrorYZ, bool mirrorXZ, bool mirrorXY,
            in IWorkPiece wp, in double TolBrep, in double tolLinear, in double tolAngle, in double tolWebFlange, ref List<Brep> features,
            double surplus, double verticalAxisRadAngle = 0, double normalAxisRadAngle = 0)
        {
            List<ProgramPoint> uniquePoints = RemoveDuplicates(macroPoint);

            double planeWidth = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L" ? wp.Prf.SA : wp.Prf.SB;

            if (!ApplyContourSurplusXY(uniquePoints, planeWidth, surplus, tolLinear, out List<ProgramPoint> pointsWithSurplus))
                return false;

            //
            //  Applica tolwebflange ai punti delle ali coincidenti con l'anima o ai punti del web coincidenti con le ali
            //  (serve per non far fallire eyeshot nei casi limite)
            //
            if (!ApplyTolWebFlange(wp, extrusionPlane, tolWebFlange, tolLinear, ref pointsWithSurplus))
                return false;

            //
            //  Applica l'offeset Y ai punti delle ali per riportare le coordinate al centro del piano (serve per il mirroring)
            //
            if (!ApplyOffsetYMirroring(pointsWithSurplus, wp, extrusionPlane))
                return false;
            
            //
            //  Il surplus alla extrusionDepth viene applicato in automatico (in questo caso deve scomparire nel codice delle macro)
            //
            return AddContourExtrusion(pointsWithSurplus, extrusionPlane, extrusionDepth + 2 * surplus, mirrorYZ, mirrorXZ, mirrorXY, wp, TolBrep, tolLinear, tolAngle, ref features, verticalAxisRadAngle, normalAxisRadAngle);
        }

        /// <summary>
        /// Creates a contour extrusion from the list of points and subtracts it to the part
        /// </summary>
        /// <param name="macroPoint">List of points describing the close contour</param>
        /// <param name="extrusionPlane">A B C D</param>
        /// <param name="extrusionDepth">Extrusion depth</param>
        /// <param name="vx"></param>
        /// <param name="side"></param>
        /// <param name="wp"></param>
        /// <param name="part"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        private static bool AddContourExtrusion(List<ProgramPoint> macroPoint, string extrusionPlane, double extrusionDepth, 
            bool mirrorYZ, bool mirrorXZ, bool mirrorXY,
            IWorkPiece wp, double TolBrep, double tolLinear, double tolAngle, ref List<Brep> features, double verticalAxisRadAngle = 0, double normalAxisRadAngle = 0, bool splitWeb = false)
        {
            Brep feature = null;
            ComputeCurves(macroPoint, string.Empty, tolLinear, tolAngle, out List<ICurve> curves, true);

            double offsetX = 0, offsetY = 0, offsetZ = 0;
            double surplus = 1;
            Vector3D amount = null;
            GetSolidSubtractOffsetAmount(wp, extrusionPlane, extrusionDepth, TolBrep, tolAngle,  surplus, ref amount, ref offsetX, ref offsetY, ref offsetZ, verticalAxisRadAngle, normalAxisRadAngle);

            //
            // Forzo lo split dell'anima per testare la funzionalità
            //
            if (true)
                splitWeb = true;

            if (extrusionPlane == "C")
            {
                //
                // Analizzo i punti del contorno per capire se devo splittare l'estrusione in semiali (superiore e inferiore) e web
                // Devo farlo quando il solido da sottrarre rimuove l'ala, questo succede quando y <0 o y > SA
                //
                bool sideA = curves.Any(c => curves.Any(c => c.StartPoint.Y + offsetY < 0)),
                     sideB = curves.Any(c => curves.Any(c => c.StartPoint.Y + offsetY > wp.Prf.SA));
                //
                // Analizzo i punti del contorno per capire se devo splittare l'estrusione in semiali (superiore e inferiore) SENZA web
                // Devo farlo quando il solido da sottrarre rimuove solo semiala superiore e semiala inferiore, questo succede quando y è all'interno dello spessore ala 
                //
                bool flangeAThickness = curves.All(c => c.StartPoint.Y >= 0 && c.StartPoint.Y <= wp.Prf.TB &&
                                                        c.EndPoint.Y >= 0 && c.EndPoint.Y <= wp.Prf.TB);

                bool flangeBThickness = curves.All(c => c.StartPoint.Y >= wp.Prf.SA - wp.Prf.TB && c.StartPoint.Y <= wp.Prf.SA &&
                                                        c.EndPoint.Y >= wp.Prf.SA - wp.Prf.TB && c.EndPoint.Y <= wp.Prf.SA);

                // Rimuovo semiala suoperiore e inferiore ed anima
                if (splitWeb && (sideA || sideB))
                    features.AddRange(GetWebSolids(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY));
                // Rimuovo solo semiala superiore e semiala inferiore senza anima
                else if (splitWeb && (flangeAThickness || flangeBThickness))
                    features.AddRange(GetWebSolids(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY));
                // Rimuovo tutto normalemente
                else
                    EyeUtils.SolidSubtract(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY);
            }
            else
                EyeUtils.SolidSubtract(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY);
            
            features.Add(feature);
            double zDown = wp.Prf.CodePrf == "I" ? wp.Prf.SB / 2 - wp.Prf.TA / 2 : 0, // Z coordinate of the web lower planar face
                       zUp = wp.Prf.CodePrf == "I" ? wp.Prf.SB / 2 + wp.Prf.TA / 2 : wp.Prf.TA, // Z coordinate of the web upper planar face
                       yA = wp.Prf.TB, // Y coordinate of the boundary between web and flange A
                       yB = wp.Prf.SA - wp.Prf.TB, // Y coordinate of the boundary between web and flange O,
                       flange_surplus = 0, flange_web_clearance = 0, flange_x_translation = 0, web_x_translation_flange = 0, web_x_translation = 0;

            if (extrusionPlane == "A" || extrusionPlane == "B")
            {
                amount.Y += amount.Y > 0 ? wp.Prf.Radius : -wp.Prf.Radius;
                EyeUtils.SolidSubtract(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY);
                
                if (feature.BoxMax == null || feature.BoxMin == null)
                    feature.Regen(0.01);

                if (feature.BoxMax.Z < zDown - wp.Prf.Radius || feature.BoxMin.Z > zUp + wp.Prf.Radius)
                    return true;

                var filletScraps = EyeUtils.ProcessFlangeScraps(new List<Brep> { feature }, extrusionPlane, true, wp.Lp, yA, yB, zUp, zDown, flange_surplus, flange_web_clearance, flange_x_translation, web_x_translation_flange, tolLinear);

                foreach (var fs in filletScraps)
                    features.Add(fs.solid);
            }
                return true;
        }


        //
        // Data la lista di curve del contorno da estrudere, crea i solidi relativi alle semiali superiore e inferiore e web 
        //
        private static List<Brep> GetWebSolids(List<ICurve> curves, IWorkPiece wp, string extrusionPlane, Vector3D amount, double offsetX, double offsetY, double offsetZ, double TolBrep, ref Brep feature, bool mirrorYZ, bool mirrorXZ, bool mirrorXY, bool removeWeb = true)
        {
            List<Brep> webSolids = new List<Brep>();

            double flange_web_clearance = 1; // Surplus applicato allo spessore dell'anima per evitare errori di sottrazione solida
            double distanceFromWebToFlange = 0;
            // traslo le curve con i rispettivi offset
            curves.ForEach(c => c.Translate(offsetX, offsetY));

            var curvesWeb = curves.Where(c => c.StartPoint.Y < 0 && c.EndPoint.Y < 0).ToList();

            for (var i = 0; i < curves.Count; i++)
            {
                ICurve curve = curves[i],
                       nextCurve = i != curves.Count - 1 ? curves[i+1] : curves[0];

                // Side A, Side B
                if (curve.StartPoint.Y < 0 && curve.EndPoint.Y < 0 || curve.StartPoint.Y > wp.Prf.SA && curve.EndPoint.Y > wp.Prf.SA)
                {
                    // Estrudo la parte inferiore e superiore dell'anima
                    devDept.Eyeshot.Entities.Region rInf = curve.ExtrudeAsSurface(0, 0, wp.Prf.SB / 2 - wp.Prf.TA / 2 - distanceFromWebToFlange)[0].ConvertToRegion();

                    Vector3D direction = nextCurve is Line ? ((Line)nextCurve).Direction: new Vector3D(0,1,0);
                    direction.Normalize();
                    direction *= ((wp.Prf.Radius + wp.Prf.TB+1) / Math.Cos(direction.AngleInXY - Math.PI / 2) );

                    bool planeNormalInPositiveDirection = rInf.Plane.Equation.Y >= 0;
                    if (planeNormalInPositiveDirection)
                        direction.Negate();

                    Brep inf = rInf.ExtrudeAsBrep(direction);

                    EyeUtils.MirrorBrep(wp, extrusionPlane, ref inf, mirrorYZ, mirrorXZ, mirrorXY);

                    Brep sup = inf.Clone() as Brep;
                    sup.Translate(0, 0, wp.Prf.SB / 2 + wp.Prf.TA / 2 + distanceFromWebToFlange);

                    EyeUtils.SolidSubtract(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY);

                    /*
                     * Servono per tagliare il solido feature in modo da rimuovere solo il web ma al momento la strategia è di 
                     * sottrarre il solido completo 
                    double _zDown = wp.Prf.CodePrf == "I" ? wp.Prf.SB / 2 - wp.Prf.TA / 2 : 0, // Z coordinate of the web lower planar face
                           _zUp = wp.Prf.CodePrf == "I" ? wp.Prf.SB / 2 + wp.Prf.TA / 2 : wp.Prf.TA;
                    double zDown = _zDown - flange_web_clearance,
                           zUp = _zUp + flange_web_clearance;
                    var upperHorizontalPlane = new Plane(new Point3D(0, 0, zUp), Vector3D.AxisZ);
                    var lowerHorizontalPlane = new Plane(new Point3D(0, 0, zDown), Vector3D.AxisZ);
                    */

                    webSolids.Add(inf);
                    webSolids.Add(sup);

                    if (removeWeb)
                        webSolids.Add(feature);

                }
                else
                   continue;
            }

            return webSolids;
        }
        
        private static bool ComputeCurves(in List<ProgramPoint> macroPoint, string extrusionPlane, in double tolLinear, in double tolAngle,
                                          out List<ICurve> curves, bool IsClosed = false)
        {
            curves = new List<ICurve>();

            ProgramPoint m1 = null, m2 = null;
            Line lPrev = null, lFirst = null;
            bool lastPoint = false;

            for (int idx = 0; idx < macroPoint.Count; idx++)
            {
                m1 = macroPoint[idx];

                if (idx < macroPoint.Count - 1)
                {
                    lastPoint = false;
                    m2 = macroPoint[idx + 1];
                }
                else
                {
                    lastPoint = true;
                    if (IsClosed)
                        m2 = macroPoint[0];
                    else
                        continue;
                }
                if (
                    m1.X.IsEqualTo(m2.X, tolLinear) &&  // Check if the coordinates of the program points 
                    m1.Y.IsEqualTo(m2.Y, tolLinear) &&  // are equals
                    m1.Z.IsEqualTo(m2.Z, tolLinear)
                    )
                {
                    continue;
                }

                // If ArcRadius != 0 and Radius != 0  is given the priority to Radius
                if (m2.ArcRadius == 0 || m1.Radius != 0)
                {
                    Point3D a = new Point3D(m1.X, m1.Y, m1.Z), b = new Point3D(m2.X, m2.Y, m2.Z);

                    Line l = new Line(a, b);
                    curves.Add(l);

                    if (m1.Radius > 0)
                    {
                        if (!AddArc(ref curves, lPrev, l, m1.Radius, tolLinear))
                            return false;
                    }

                    if (lastPoint && macroPoint[0].Radius > 0)
                    {
                        if (!AddArc(ref curves, l, lFirst, macroPoint[0].Radius, tolLinear))
                            return false;
                    }

                    if (idx == 0)
                        lFirst = l;

                    lPrev = l;
                }
                else // Handle ArcRadius
                {
                    Point3D a = new Point3D(m1.X, m1.Y, m1.Z), b = new Point3D(m2.X, m2.Y, m2.Z);

                    AddArc(a, b, extrusionPlane, m2.ArcRadius, tolLinear, out ICurve arc);

                    if (arc is CompositeCurve cc)
                        curves.Add(cc);
                    else
                        curves.Add(arc);

                }
            }

            return true;
        }

        // Non gestisce il mirroring, se necessario va implementato fuori dalla funzione
        public static bool AddCurves(in List<ProgramPoint> macroPoint, string extrusionPlane,
            IWorkPiece wp, in double tolLinear, in double tolAngle, out List<IEyeCurve> eyeCurves, bool IsClosed = false)
        {
            ComputeCurves(macroPoint, string.Empty, tolLinear, tolAngle, out List<ICurve> curves);

            bool isPlaneHorizontal = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L";
            if (!isPlaneHorizontal)
            {
                curves = EyeUtils.ProjectCurvesOnPlane(curves, Plane.XZ);

                // Transform the list of ICurve in a list of IEyeCurve translating them if they belong to the b plane
                eyeCurves = curves.Select(c =>
                {
                    if (extrusionPlane == "B")
                        c.Translate(0, wp.Prf.SA - wp.Prf.TB);

                    return c.ConvertToEyeCurve(extrusionPlane);
                }).ToList();
            }
            else
            {
                if (wp.Prf.CodePrf == "I")
                    curves.ForEach(c => c.Translate(0, 0, wp.Prf.SB / 2 - wp.Prf.TA / 2));
                else if (wp.Prf.CodePrf == "Q" && extrusionPlane == "D")
                    curves.ForEach(c => c.Translate(0, 0, wp.Prf.SB - wp.Prf.TA));

                eyeCurves = curves.Select(c => c.ConvertToEyeCurve(extrusionPlane)).ToList();
            }

            return true;
        }

        public static bool AddContourExtrusionRepeated(in List<ProgramPoint> macroPoint, string extrusionPlane, double extrusionDepth, bool mirrorYZ, bool mirrorXZ, bool mirrorXY,
            int xRepetition, int yRepetition, double xStep, double yStep,
            in EyeWorkPiece wp, in double tolBrep, in double tolLinear, in double tolAngle, ref List<Brep> features)
        {
            List<ProgramPoint> mPoint = new List<ProgramPoint>();
            for (int k = 0; k < macroPoint.Count(); k++)
                mPoint.Add(new ProgramPoint());

            for (int i = 0; i < xRepetition; i++)
            {
                for (int j = 0; j < yRepetition; j++)
                {
                    for (int k = 0; k < mPoint.Count(); k++)
                    {
                        mPoint[k].X = macroPoint[k].X + i * xStep;
                        mPoint[k].Y = macroPoint[k].Y + j * yStep;
                        mPoint[k].Z = macroPoint[k].Z;
                        mPoint[k].Radius = macroPoint[k].Radius;
                    }
                    if (!AddContourExtrusion(mPoint, extrusionPlane, extrusionDepth, mirrorYZ, mirrorXZ, mirrorXY,
                        wp, tolBrep, tolLinear, tolAngle, ref features))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a rectangular contour extrusion and subtracts it to the part
        /// </summary>
        /// <param name="xBottomLeft"></param>
        /// <param name="yBottomLeft"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="extrusionPlane"></param>
        /// <param name="extrusionDepth"></param>
        /// <param name="mirrorYZ"></param>
        /// <param name="side"></param>
        /// <param name="wp"></param>
        /// <param name="part"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static bool AddRectExtrusion(double xBottomLeft, double yBottomLeft, double length, double width,
        double r1, double r2, double r3, double r4,
        string extrusionPlane, double extrusionDepth, bool mirrorYZ, bool mirrorXZ, bool mirrorXY,
        in IWorkPiece wp, in double tolBrep, in double tolLinear, in double tolWebFlange, ref List<Brep> features, double surplus)
        {
            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            macroPoint.Add(new ProgramPoint(xBottomLeft, yBottomLeft, 0, r1));
            macroPoint.Add(new ProgramPoint(xBottomLeft + length, yBottomLeft, 0, r2));
            macroPoint.Add(new ProgramPoint(xBottomLeft + length, yBottomLeft + width, 0, r3));
            macroPoint.Add(new ProgramPoint(xBottomLeft, yBottomLeft + width, 0, r4));

            return AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth + 2 * surplus, mirrorYZ, mirrorXZ, mirrorXY,
                wp, tolBrep, tolLinear, tolWebFlange, ref features, surplus);
        }

        private static IEnumerable<Line> ComputeCuttingVectors(ICurve c1, ICurve c2)
        {
            c1 = (ICurve)c1.Clone();
            c2 = (ICurve)c2.Clone();

            c2.Reverse();

            Surface ruled = Surface.Ruled(c2, c1);

            Line startVector = null;
            int n = 100;

            for (int i = 0; i < n; i++)
            {
                double u = ruled.DomainU.Min + i * ruled.DomainU.Length / n;

                var c = ruled.IsocurveV(u);

                Vector3D direction = new Vector3D(c.StartPoint, c.EndPoint);
                Point3D endPoint = c.StartPoint - direction;
                Line l = new Line(c.StartPoint, endPoint);

                if (ruled.IsClosedU && i == 0)
                    startVector = (Line)l.Clone();

                l.LineWeightMethod = colorMethodType.byEntity;
                l.LineWeight = 2;

                l.ColorMethod = colorMethodType.byEntity;
                l.Color = Color.OrangeRed;

                yield return l;
            }

            // Add startVector at the end of the returned array
            if (startVector != null)
                yield return startVector;
        }

        public static bool AddCircleExtrusion(Point2D centre, double radius, string extrusionPlane, double extrusionDepth,
                                              bool mirrorYZ, bool mirrorXZ, bool mirrorXY,
                                              in IWorkPiece wp, in double tolBrep, in double tolLinear, in double tolAngle, in double tolWebFlange, ref Brep feature,
                                              in double surplus, double verticalAxisAngle = 0, double normalAxisAngle = 0)
        {
            if (radius <= 0)
                return false;

            //
            //  Applica l'offeset Y al  per riportare le coordinate al centro del piano (serve per il mirroring)
            //
            if (!ApplyOffsetYMirroring(wp, extrusionPlane, ref centre))
                return false;

            if(!ApplyTolWebFlange(wp, extrusionPlane, tolWebFlange, tolLinear, ref centre.Y, radius))
                return false;

            // Create the circle object
            Circle circle = new Circle(Plane.XY, centre, radius);

            // Create the list of curves
            List<ICurve> curves = new List<ICurve>
            {
                // Add the circle to the list
                circle
            };

            double offsetX = 0, offsetY = 0, offsetZ = 0;
            Vector3D amount = null;
            GetSolidSubtractOffsetAmount(wp, extrusionPlane, extrusionDepth + 2 * surplus, tolBrep, tolAngle, surplus, ref amount, ref offsetX, ref offsetY, ref offsetZ, verticalAxisAngle, normalAxisAngle);
            EyeUtils.SolidSubtract(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, tolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY);

            return true;
        }

        /// <summary>
        /// Compute a solid extruding a slot
        /// </summary>
        /// <param name="centre">
        /// Centre point of the slot
        /// </param>
        /// <param name="lenght">
        /// Centre to centre distance
        /// </param>
        /// <param name="radius">
        /// Radius of the arcs
        /// </param>
        /// <param name="extrusionPlane">A B C D</param>
        /// <param name="extrusionDepth"></param>
        /// <param name="mirrorYZ"></param>
        /// <param name="mirrorXZ"></param>
        /// <param name="mirrorXY"></param>
        /// <param name="wp"></param>
        /// <param name="tolBrep"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        public static bool AddSlotExtrusion(Point2D centre, double lenght, double radius, string extrusionPlane, double extrusionDepth,
                                              bool mirrorYZ, bool mirrorXZ, bool mirrorXY,
                                              in IWorkPiece wp, in double tolBrep, in double tolAngle, ref Brep feature, in double surplus, double verticalAxisAngle = 0, double normalAxisAngle = 0)
        {

            Point2D temp = centre.Clone() as Point2D;
            //
            //  Applica l'offeset Y al  per riportare le coordinate al centro del piano (serve per il mirroring)
            //
            if (!ApplyOffsetYMirroring(wp, extrusionPlane, ref temp))
                return false;

            // Compute the x,y coordinate of the first centre of the slot
            double x = temp.X - lenght / 2,
                   y = temp.Y;

            // Create the circle object
            CompositeCurve slot = CompositeCurve.CreateSlot(x, y, lenght, radius);

            // Create the list of curves
            List<ICurve> curves = new List<ICurve>();
            // Add the slot curves to the list
            curves.AddRange(slot.CurveList);

            double offsetX = 0, offsetY = 0, offsetZ = 0;
            Vector3D amount = null;
            GetSolidSubtractOffsetAmount(wp, extrusionPlane, extrusionDepth + 2 * surplus, tolBrep, tolAngle, surplus, ref amount, ref offsetX, ref offsetY, ref offsetZ, verticalAxisAngle, normalAxisAngle);
            EyeUtils.SolidSubtract(curves, wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, tolBrep, ref feature, mirrorYZ, mirrorXZ, mirrorXY);

            return true;
        }

        /// <summary>
        /// Rotate the solid in the specified plane around a centre of rotation
        /// </summary>
        /// <param name="radANGLE">
        /// Angle in radians
        /// </param>
        /// <param name="x">
        /// X coordinate of the centre of rotation in the plane
        /// </param>
        /// <param name="y">
        /// Y coordinate of the centre of rotation in the plane
        /// </param>
        /// <param name="plane">A, B, C, D</param>
        /// <param name="solid">Solid to rotate</param> 
        /// <returns></returns>
        public static bool RotateSolid(in double radANGLE, in double x, in double y, in string plane, in IWorkPiece wp, ref Brep solid, bool MirrorInizialeFinale = false, bool MirrorSideASideB = false, bool MirrorAltoBasso = false)
        {
            Point3D rotationCenter;

            bool horizontalPlane = plane == "C" || plane == "D" || plane == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = plane == "A" || plane == "B" && wp.Prf.CodePrf != "L";

            //
            //  Manipolazione del segno dell'angolo:
            //
            //  -   non viene gestito nessun flag di mirroring in ingresso
            //  -   ipotizzo che l'angolo passato si riferisca ad un osservatore che guarda il piano secondo la
            //      normale uscente dal piano e che voglia vedere un angolo in igresso > 0 corrispondente a una
            //      rotazione antioraria
            //
            //  Allo scopo:
            //
            //  -   l'angolo passato non viene modificato se si tratta di un piano orizzontale diverso da D oppure
            //      se si tratta del piano verticale B
            //  -   negli altri casi viene invertito il segno dell'angolo
            //
            if (horizontalPlane)
            {
                rotationCenter = new Point3D(x, y, 0);

                if(MirrorInizialeFinale)
                    rotationCenter.X = wp.Lp - rotationCenter.X;

                if (plane == "D")
                    solid.Rotate(-radANGLE, Vector3D.AxisZ, rotationCenter);
                else
                    solid.Rotate(radANGLE, Vector3D.AxisZ, rotationCenter);
            }
            else
            {
                if (MirrorAltoBasso)
                {
                    double yMiddle = wp.Prf.SB / 2;
                    rotationCenter = new Point3D(x, 0, 2 * yMiddle - y);
                }
                else
                    rotationCenter = new Point3D(x, 0, y);

                if (MirrorInizialeFinale)
                    rotationCenter.X = wp.Lp - rotationCenter.X;

                if (plane == "B" || MirrorSideASideB)
                    solid.Rotate(radANGLE, Vector3D.AxisY, rotationCenter);
                else
                    solid.Rotate(-radANGLE, Vector3D.AxisY, rotationCenter);
            }

            return true;
        }

        /// <summary>
        /// Compute the brep solid needed to obtain an external flange chamfer
        /// </summary>
        /// <param name="start">
        /// 2D point programmed in the flange plane corresponding to the start of the chamfer 
        /// </param>
        /// <param name="end">
        /// 2D point programmed in the flange plane corresponding to the start of the chamfer
        /// </param>
        /// <param name="extrusionPlane"> A B</param>
        /// <param name="radAngle">
        /// Angle in radians of the chamfer
        /// </param>
        /// <param name="depth"></param>
        /// <param name="flip"></param>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static bool AddExternalChamfer(in ProgramPoint start, in ProgramPoint end, in IWorkPiece wp, in string extrusionPlane, in double radAngle, in double depth, in bool mirrorInizialeFinale, in bool mirrorAltoBasso,
                                              in double surplus, in double tolBrep, in double tolLinear, in double tolWebFlange, out Brep solid, in bool flip = false, bool normalToRailCurve = true)
        {
            solid = null;
            double _surplus = surplus;

            double limitAngle = 0.1;
            // If the angle is less than  limit angle degrees do nothing
            if (radAngle < limitAngle)
                return false;

            ProgramPoint s, e;

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                 verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            // I have to flip the triangle on the x direction just if  mirrorInizialeFinale != flip;
            bool _flip = mirrorInizialeFinale != flip, external = true ;

            if (wp.Prf.CodePrf == "Q" && horizontalPlane && mirrorAltoBasso)
            {
                _flip = mirrorAltoBasso;
                external = false ;
            }
                
            if (!ApplySurplusToChamferPoints2D(start, end, extrusionPlane, verticalPlane, wp, tolLinear, surplus, radAngle, depth, mirrorInizialeFinale,
                                             out ProgramPoint start2D, out ProgramPoint end2D))
                return false;

            AddThirdDimensionToProgramPoints(start2D, end2D, wp, extrusionPlane, true, out s, out e);

            return AddChamfer(s, e, external, extrusionPlane, horizontalPlane, tolLinear, tolBrep, wp, _surplus, radAngle, depth, normalToRailCurve, mirrorInizialeFinale, mirrorAltoBasso, _flip, out solid);
        }

        /// <summary>
        /// Compute the brep solid needed to obtain an external flange chamfer
        /// </summary>
        /// <param name="start">
        /// 2D point programmed in the flange plane corresponding to the start of the chamfer 
        /// </param>
        /// <param name="end">
        /// 2D point programmed in the flange plane corresponding to the start of the chamfer
        /// </param>
        /// <param name="extrusionPlane"> A B</param>
        /// <param name="radAngle">
        /// Angle in radians of the chamfer
        /// </param>
        /// <param name="depth"></param>
        /// <param name="flip"></param>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static bool AddInternalChamfer(in ProgramPoint start, in ProgramPoint end, in IWorkPiece wp, in string extrusionPlane, in double radAngle, in double depth, in bool mirrorInizialeFinale, in bool mirrorAltoBasso, in double surplus, in double tolBrep, in double tolLinear, in double tolWebFlange, out Brep solid, in bool flip = false, bool normalToRailCurve = true)
        {
            solid = null;
            double _surplus = surplus;

            double limitAngle = 0.1;
            // If the angle is less than  limit angle degrees do nothing
            if (radAngle < limitAngle)
                return false;

            //
            // Apply the TolWebFlange when needed to avoid eyeshot crash 
            //
            //*************************************************************************************
            List<ProgramPoint> programPoints = new List<ProgramPoint>();
            programPoints.Add(start);
            programPoints.Add(end);

            ApplyTolWebFlange(wp, extrusionPlane, tolWebFlange, tolLinear, ref programPoints);
            //**************************************************************************************

            ProgramPoint s, e;

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                 verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            // I have to flip the triangle on the x direction just if  mirrorInizialeFinale != flip;
            bool _flip = mirrorInizialeFinale != flip;

            if (!ApplySurplusToChamferPoints2D(start, end, extrusionPlane, verticalPlane, wp, tolLinear, surplus, radAngle, depth, mirrorInizialeFinale,
                                             out ProgramPoint start2D, out ProgramPoint end2D))
                return false;

            AddThirdDimensionToProgramPoints(start2D, end2D, wp, extrusionPlane, false, out s, out e);

            return AddChamfer(s, e, false, extrusionPlane, horizontalPlane, tolLinear, tolBrep, wp, _surplus, radAngle, depth, normalToRailCurve, mirrorInizialeFinale, mirrorAltoBasso, _flip, out solid);
        }

        /// <summary>
        /// Compute the brep solid needed to obtain an external circular chamfer
        /// </summary>
        /// <param name="centre"> Centre of the circle</param>
        /// <param name="extrusionPlane"> A B</param>
        /// <param name="radAngle">
        /// Angle in radians of the chamfer
        /// </param>
        /// <param name="depth"></param>
        /// <param name="flip"></param>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static bool AddExternalCircularChamfer(in ProgramPoint centre, in IWorkPiece wp, in string extrusionPlane, in double radAngle, in double depth, in bool mirrorInizialeFinale, in bool mirrorAltoBasso,
                                              in double surplus, in double tolBrep, in double tolLinear, in double tolWebFlange, out Brep solid, in bool flip = false, bool normalToRailCurve = true)
        {
            solid = null;
            double _surplus = surplus;

            double limitAngle = 0.1;
            // If the angle is less than  limit angle degrees do nothing
            if (radAngle < limitAngle)
                return false;

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                 verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            // I have to flip the triangle on the x direction just if  mirrorInizialeFinale != flip;
            bool _flip = mirrorInizialeFinale != flip;


            AddThirdDimensionToProgramPoints(centre, null, wp, extrusionPlane, true, out ProgramPoint centre3D, out ProgramPoint useless);

            return AddCircularChamfer(centre3D, centre.Radius, true, extrusionPlane, horizontalPlane, tolLinear, tolBrep, wp, _surplus, radAngle, depth, normalToRailCurve, mirrorInizialeFinale, mirrorAltoBasso, _flip, out solid);
        }

        /// <summary>
        /// Compute the brep solid needed to obtain an external circular chamfer
        /// </summary>
        /// <param name="start">
        /// 2D point programmed in the flange plane corresponding to the start of the chamfer 
        /// </param>
        /// <param name="end">
        /// 2D point programmed in the flange plane corresponding to the start of the chamfer
        /// </param>
        /// <param name="extrusionPlane"> A B</param>
        /// <param name="radAngle">
        /// Angle in radians of the chamfer
        /// </param>
        /// <param name="depth"></param>
        /// <param name="flip"></param>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static bool AddInternalCircularChamfer(in ProgramPoint centre, in double radius, in IWorkPiece wp, in string extrusionPlane, in double radAngle, in double depth, in bool mirrorInizialeFinale, in bool mirrorAltoBasso,
                                              in double surplus, in double tolBrep, in double tolLinear, in double tolWebFlange, out Brep solid, in bool flip = false, bool normalToRailCurve = true)
        {
            solid = null;
            double _surplus = surplus;

            double limitAngle = 0.1;
            // If the angle is less than  limit angle degrees do nothing
            if (radAngle < limitAngle)
                return false;

            bool horizontalPlane = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L",
                 verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            // I have to flip the triangle on the x direction just if  mirrorInizialeFinale != flip;
            bool _flip = mirrorInizialeFinale != flip;


            AddThirdDimensionToProgramPoints(centre, null, wp, extrusionPlane, true, out ProgramPoint centre3D, out ProgramPoint useless);

            return AddCircularChamfer(centre, radius, true, extrusionPlane, horizontalPlane, tolLinear, tolBrep, wp, _surplus, radAngle, depth, normalToRailCurve, mirrorInizialeFinale, mirrorAltoBasso, _flip, out solid);
        }

        private static bool ApplySurplusToChamferPoints2D(in ProgramPoint start, in ProgramPoint end, in string extrusionPlane, in bool verticalPlane, in IWorkPiece wp, in double tolLinear, in double surplus, in double radAngle, in double depth, in bool mirrorInizialeFinale,
                                                      out ProgramPoint start2D, out ProgramPoint end2D)
        {
            start2D = start.Clone() as ProgramPoint;
            end2D = end.Clone() as ProgramPoint;

            double _surplus = surplus;
            double planeWidth = extrusionPlane == "C" || extrusionPlane == "D" || extrusionPlane == "B" && wp.Prf.CodePrf == "L" ? wp.Prf.SA : wp.Prf.SB;

            // If the chamfer is in the x direction and is not in the horizontal plane do not apply the surplus
            bool isInXDirection = !end.X.IsEqualTo(start.X, tolLinear) && end.Y.IsEqualTo(start.Y, tolLinear);
            //if (!(!isInXDirection && horizontalPlane)) // Da testare nuova condizione nel caso cancellare questa più avanti
            if (!(isInXDirection && verticalPlane))
            {
                bool inclinedLine = !end.X.IsEqualTo(start.X, tolLinear) && !end.Y.IsEqualTo(start.Y, tolLinear);

                // Compute the amount of surplus needed to obtain the chamfer along all the surface. The quantity depth * Math.Tan(radAngle)
                // is the exact amount, we add surplus to avoid eyeshot errors
                double chamferLineSurplus;
                if (inclinedLine)
                    chamferLineSurplus = depth * Math.Tan(radAngle) + surplus;
                else
                    chamferLineSurplus = surplus;


                if (!ApplyChamferSurplus(ref start2D, ref end2D, planeWidth, false, chamferLineSurplus, tolLinear))
                    return false;
            }

            return true;
        }

        public static bool AddChamfer(ProgramPoint s, ProgramPoint e, bool external, string extrusionPlane, bool horizontalPlane, double tolLinear, double tolBrep, IWorkPiece wp,
                                      double _surplus, double radAngle, double depth, bool normalToRailCurve, bool mirrorInizialeFinale, bool mirrorAltoBasso, bool _flip,
                                      out Brep solid)
        {
            solid = null;

            List<ProgramPoint> points = new List<ProgramPoint>() { s, e };
            ComputeCurves(points, extrusionPlane, tolLinear, tolLinear, out List<ICurve> curves);
            ICurve l = curves[0];
            
            if (!IsCurveIntersectingOtherPlane(l, wp, extrusionPlane, tolLinear) && IsCurveInRadiusRegion(l, wp, extrusionPlane, tolLinear))
            {
                if (IsLargerChamferSurplusNeeded(external, horizontalPlane, wp))
                    _surplus = wp.Prf.Radius;
            }

            MirrorCurve(wp, extrusionPlane, mirrorInizialeFinale, mirrorAltoBasso, ref l);

            EyeUtils.Chamfer(l, wp, extrusionPlane, external, _flip, radAngle, depth, _surplus, tolBrep, tolLinear, out solid, normalToRailCurve);

            return solid != null;
        }

        public static bool AddCircularChamfer(ProgramPoint centre, double radius, bool external, string extrusionPlane, bool horizontalPlane, double tolLinear, double tolBrep, IWorkPiece wp,
                                      double _surplus, double radAngle, double depth, bool normalToRailCurve, bool mirrorInizialeFinale, bool mirrorAltoBasso, bool _flip,
                                      out Brep solid)
        {
            solid = null;

            Plane drawingPlane = horizontalPlane ? Plane.XY : Plane.XZ;

            ICurve l = new Circle(drawingPlane, new Point3D(centre.X, centre.Y, centre.Z), radius);

            if (!IsCurveIntersectingOtherPlane(l, wp, extrusionPlane, tolLinear) && IsCurveInRadiusRegion(l, wp, extrusionPlane, tolLinear))
            {
                if (IsLargerChamferSurplusNeeded(external, horizontalPlane, wp))
                    _surplus = wp.Prf.Radius;
            }

            MirrorCurve(wp, extrusionPlane, mirrorInizialeFinale, mirrorAltoBasso, ref l);

            EyeUtils.Chamfer(l, wp, extrusionPlane, external, _flip, radAngle, depth, _surplus, tolBrep, tolLinear, out solid, normalToRailCurve);

            return solid != null;
        }

        private static void AddThirdDimensionToProgramPoints(in ProgramPoint p1, in ProgramPoint p2, in IWorkPiece wp, in string extrusionPlane, in bool external, out ProgramPoint p13D, out ProgramPoint p23D )
        {
            bool verticalPlane = extrusionPlane == "A" || extrusionPlane == "B" && wp.Prf.CodePrf != "L";

            if (verticalPlane)
            {
                double y = external ? 0 : wp.Prf.TB;
                // Transform the two points programmed on the flange plane passed in
                // into 3D
                if (p2 == null)
                {
                    p13D = new ProgramPoint(p1.X, y, p1.Y, p1.Radius, p1.ArcRadius);
                    p23D = null;
                }
                else if (p1.Y > p2.Y)
                {
                    p13D = new ProgramPoint(p2.X, y, p2.Y, p2.Radius, p2.ArcRadius);
                    p23D = new ProgramPoint(p1.X, y, p1.Y, p1.Radius, p1.ArcRadius);

                }
                else
                {
                    p13D = new ProgramPoint(p1.X, y, p1.Y, p1.Radius, p1.ArcRadius);
                    p23D = new ProgramPoint(p2.X, y, p2.Y, p2.Radius, p2.ArcRadius);
                }
            }
            else
            {
                // Transform the two points programmed on the web plane passed in
                // into 3D

                double z = 0;
                if (external)
                {
                    if (wp.Prf.CodePrf == "I")
                        z = wp.Prf.SB / 2 + wp.Prf.TA / 2;
                    else if (wp.Prf.CodePrf == "Q")
                    {
                        if (extrusionPlane == "C")
                            z = wp.Prf.SB;
                        else if (extrusionPlane == "D")
                            z = 0;
                    }
                    else if (wp.Prf.CodePrf == "L")
                        z = wp.Prf.TB;
                    else
                        z = wp.Prf.TA;
                }
                else
                {
                    if (wp.Prf.CodePrf == "I")
                        z = wp.Prf.SB / 2 - wp.Prf.TA / 2;
                    else if (wp.Prf.CodePrf == "Q")
                    {
                        if (extrusionPlane == "C")
                            z = wp.Prf.SB - wp.Prf.TA;
                        else if (extrusionPlane == "D")
                            z = wp.Prf.TA;
                    }
                    else
                        z = 0;
                }

                if (p2 == null)
                {
                    p13D = new ProgramPoint(p1.X, p1.Y, z, p1.Radius, p1.ArcRadius);
                    p23D = null;
                }
                else if (p1.Y > p2.Y)
                {
                    p13D = new ProgramPoint(p2.X, p2.Y, z, p2.Radius, p2.ArcRadius);
                    p23D = new ProgramPoint(p1.X, p1.Y, z, p1.Radius, p1.ArcRadius);
                }
                else
                {
                    p13D = new ProgramPoint(p1.X, p1.Y, z, p1.Radius, p1.ArcRadius);
                    p23D = new ProgramPoint(p2.X, p2.Y, z, p2.Radius, p2.ArcRadius);
                }
            }
        }

        private static bool IsLargerChamferSurplusNeeded(bool externalChamfer, bool horizontalPlane, IWorkPiece wp)
        {
            string codeProfile = wp.Prf.CodePrf;

            if (codeProfile == "F")
                return false;
            else if (codeProfile == "U" || codeProfile == "L")
            {
                if (externalChamfer != horizontalPlane)
                    return false;
                else 
                    return true;
            }
            else if (codeProfile == "I")
            {
                if (externalChamfer && !horizontalPlane)
                    return false;
                else 
                    return true;
            }
            else if (codeProfile == "Q")
            {
                if (!externalChamfer && !horizontalPlane || !externalChamfer && horizontalPlane)
                    return true;
                else 
                    return false;
            }

            return false;
        }

        private static bool MirrorCurve(in IWorkPiece wp, in string side, in bool mirrorInizialeFinale, in bool mirrorAltoBasso, ref ICurve curve)
        {
            bool horizontalPlane = side == "C" || side == "D" || side == "B" && wp.Prf.CodePrf == "L",
                verticalPlane = side == "A" || side == "B" && wp.Prf.CodePrf != "L";

            Point3D origin = Point3D.Origin;
            double tMin = 0;
            curve.ClosestPointTo(origin, out tMin);
            Point3D pMin = curve.PointAt(tMin);

            double xMin = pMin.X, 
                   yMin = pMin.Y,
                   zMin = pMin.Z;

            //
            //  Mirror X -> LP -X
            //
            if (mirrorInizialeFinale)
            {
                Plane mirrorPlane = new Plane(new Point3D(wp.Lp / 2, 0, 0), Vector3D.AxisX);
                Mirror mirror = new Mirror(mirrorPlane);

                if (curve is Line line)
                    line.TransformBy(mirror);
                else if (curve is Arc arc)
                    arc.TransformBy(mirror);
                else if (curve is CompositeCurve cc)
                    cc.TransformBy(mirror);
                else if (curve is Curve c)
                    c.TransformBy(mirror);
            }

            //
            //  Mirror Y -> SA - Y
            //
            if (side == "B")
            {
                if (curve is Line line)
                    line.Translate(0, wp.Prf.SA - 2 * yMin);
                else if (curve is Arc arc)
                    arc.Translate(0, wp.Prf.SA - 2 * yMin);
                else if (curve is CompositeCurve cc)
                    cc.Translate(0, wp.Prf.SA - 2 * yMin);
                else if (curve is Curve c)
                    c.Translate(0, wp.Prf.SA - 2 * yMin);
            }

            if (mirrorAltoBasso)
            {
                // Translate the solid on the bottom side  
                if (side != "c")
                {
                    Plane mirrorPlane = new Plane(new Point3D(0, 0, wp.Prf.SB / 2), Vector3D.AxisZ);
                    Mirror mirror = new Mirror(mirrorPlane);

                    // Mirror the curve and reverse the direction in order to obtain the start point with the lower
                    // z coordinate
                    if (curve is Line line)
                    {
                        line.TransformBy(mirror);
                        line.Reverse();
                    }
                    else if (curve is Arc arc)
                    {
                        arc.TransformBy(mirror);
                        arc.Reverse();
                    }
                    else if (curve is CompositeCurve cc)
                    {
                        cc.TransformBy(mirror);
                        cc.Reverse();
                    }
                    else if (curve is Curve c)
                    {
                        c.TransformBy(mirror);
                        c.Reverse();
                    }
                }
            }
            return true;
        }

        private static bool IsCurveIntersectingOtherPlane(in ICurve curve, in IWorkPiece wp, in string side, in double tolLinear )
         {
            bool horizontalPlane = side == "C" || side == "D" || side == "B" && wp.Prf.CodePrf == "L",
                 verticalPlane = side == "A" || side == "B" && wp.Prf.CodePrf != "L";

            if (wp.Prf.CodePrf == "F")
                return false;

            if (horizontalPlane)
            {
                if (side == "C" || side == "D")
                {
                    // cordinate y dell'inizio ala A e B
                    double yFlangeA = wp.Prf.TB , yFlangeB = wp.Prf.SA - wp.Prf.TB;

                    if (curve.StartPoint.Y.IsLessThan(yFlangeA, tolLinear) || curve.StartPoint.Y.IsGreaterThan(yFlangeB, tolLinear) ||
                        curve.EndPoint.Y.IsLessThan(yFlangeA, tolLinear) || curve.EndPoint.Y.IsGreaterThan(yFlangeB, tolLinear))
                        return true;
                    else
                        return false;

                }
                else if (side == "B" && wp.Prf.CodePrf == "L")
                {
                    double yFlangeA = wp.Prf.TA;

                    if (curve.StartPoint.Y.IsLessThan(yFlangeA, tolLinear) ||
                        curve.EndPoint.Y.IsLessThan(yFlangeA, tolLinear))
                        return true;
                    else 
                        return false;
                }

                return false;
            }
            else
            {
                if (wp.Prf.CodePrf == "I")
                {
                    // cordinate y degli edges superiore e inferiore del web con sistema di riferimento in y = 0
                    double yWebUp = wp.Prf.SB / 2 + wp.Prf.TA / 2, yWebDown = wp.Prf.SB / 2 - wp.Prf.TA / 2;

                    // The curve intersect the web  if the start or end point are above the webUp (superior edge of the web) and the end or start point
                    // are below the webUp or if the the start or end point are below the webDown (inferior edge of the web) and the end or start point
                    // are above the webDown
                    if (curve.StartPoint.Z.IsLessThan(yWebDown, tolLinear) && curve.EndPoint.Z.IsGreaterThan(yWebDown, tolLinear) ||
                        curve.EndPoint.Z.IsLessThan(yWebDown, tolLinear) && curve.StartPoint.Z.IsGreaterThan(yWebDown, tolLinear) ||
                        curve.StartPoint.Z.IsLessThan(yWebUp, tolLinear) && curve.EndPoint.Z.IsGreaterThan(yWebUp, tolLinear) ||
                        curve.EndPoint.Z.IsLessThan(yWebUp, tolLinear) && curve.StartPoint.Z.IsGreaterThan(yWebUp, tolLinear))
                        return true;
                    else
                        return false;
                }
                else if (wp.Prf.CodePrf == "U" || wp.Prf.CodePrf == "L")
                {
                    // cordinate y dell'edge superiore del web con sistema di riferimento in y = 0
                    double yWebUp = wp.Prf.CodePrf != "L" ? wp.Prf.TA : wp.Prf.TB;

                    if (curve.StartPoint.Z.IsLessThan(yWebUp, tolLinear) ||
                        curve.EndPoint.Z.IsLessThan(yWebUp, tolLinear))
                        return true;
                    else 
                        return false;

                }
                else if (wp.Prf.CodePrf == "Q")
                {
                    // cordinate y dell'edge superiore del piano D e dell'edge inferiore del piano c con sistema di riferimento in y = 0
                    double yWebD = wp.Prf.TA, yWebC = wp.Prf.SB - wp.Prf.TA;

                    if (curve.StartPoint.Y.IsGreaterThan(yWebD, tolLinear) || curve.StartPoint.Y.IsLessThan(yWebC, tolLinear) ||
                        curve.EndPoint.Y.IsGreaterThan(yWebD, tolLinear) || curve.EndPoint.Y.IsLessThan(yWebC, tolLinear))
                        return true;
                    else return false;

                }

                return false;
            }
        }

        private static bool IsCurveInRadiusRegion(in ICurve curve, in IWorkPiece wp, in string side, in double tolLinear)
        {
            bool horizontalPlane = side == "C" || side == "D" || side == "B" && wp.Prf.CodePrf == "L",
                 verticalPlane = side == "A" || side == "B" && wp.Prf.CodePrf != "L";

            if (wp.Prf.CodePrf == "F" || wp.Prf.Radius == 0)
                return false;

            if (horizontalPlane)
            {
                if (side == "C" || side == "D")
                {
                    // cordinate y dell'inizio della regione dovuta al raccordo tra web e ala A e B
                    double yRadA = wp.Prf.TB + wp.Prf.Radius, yRadB = wp.Prf.SA - wp.Prf.TB - wp.Prf.Radius;

                    if (curve.StartPoint.Y.IsLessThan(yRadA, tolLinear) || curve.StartPoint.Y.IsGreaterThan(yRadB, tolLinear) ||
                        curve.EndPoint.Y.IsLessThan(yRadA, tolLinear) || curve.EndPoint.Y.IsGreaterThan(yRadB, tolLinear))
                        return true;
                    else
                        return false;

                }
                else if (side == "B" && wp.Prf.CodePrf == "L")
                {
                    double yFlangeA = wp.Prf.TB + wp.Prf.Radius;

                    if (curve.StartPoint.Y.IsLessThan(yFlangeA, tolLinear) ||
                        curve.EndPoint.Y.IsLessThan(yFlangeA, tolLinear))
                        return true;
                    else
                        return false;
                }

                return false;
            }
            else
            {
                if (wp.Prf.CodePrf == "I")
                {
                    // cordinate y degli edges superiore e inferiore del web con sistema di riferimento in y = 0
                    double zWebUp = wp.Prf.SB / 2 + wp.Prf.TA / 2 + wp.Prf.Radius, zWebDown = wp.Prf.SB / 2 - wp.Prf.TA / 2 - wp.Prf.Radius;

                    if (curve.StartPoint.Z.IsLessThan(zWebUp, tolLinear) && curve.StartPoint.Z.IsGreaterThan(zWebDown, tolLinear) ||
                        curve.EndPoint.Z.IsLessThan(zWebUp, tolLinear) && curve.EndPoint.Z.IsGreaterThan(zWebDown, tolLinear))
                        return true;
                    else
                        return false;
                }
                else if (wp.Prf.CodePrf == "U" || wp.Prf.CodePrf == "L")
                {
                    // cordinate y dell'edge superiore del web con sistema di riferimento in y = 0
                    double zWebUp = wp.Prf.TA + wp.Prf.Radius;

                    if (curve.StartPoint.Z.IsLessThan(zWebUp, tolLinear) ||
                        curve.EndPoint.Z.IsLessThan(zWebUp, tolLinear))
                        return true;
                    else
                        return false;

                }
                else if (wp.Prf.CodePrf == "Q")
                {
                    // cordinate y dell'edge superiore del piano D e dell'edge inferiore del piano c con sistema di riferimento in y = 0
                    double zWebD = wp.Prf.TA + wp.Prf.Radius, zWebC = wp.Prf.SB - wp.Prf.TA - wp.Prf.Radius;

                    if (curve.StartPoint.Z.IsGreaterThan(zWebD, tolLinear) || curve.StartPoint.Z.IsLessThan(zWebC, tolLinear) ||
                        curve.EndPoint.Z.IsGreaterThan(zWebD, tolLinear) || curve.EndPoint.Z.IsLessThan(zWebC, tolLinear))
                        return true;
                    else 
                        return false;

                }

                return false;
            }
        }

        //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //
        //  Per ora gestisce solo i cianfrini ali profili I
        //
        //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public static bool AddLineChamfer(double X, double externalAngle, double internalAngle, double landing,
        double internalDepth, double internalBreak, string sVX, string sSide, in IWorkPiece wp, in double surplus, in double tolThickness, in double brepTolerance,
        ref List<Brep> features)
        {
            List<ICurve> curves = new List<ICurve>();

            //  Dati profilo
            double SA = wp.Prf.SA, TA = wp.Prf.TA, SB = wp.Prf.SB, TB = wp.Prf.TB + tolThickness, Radius = wp.Prf.Radius;

            //
            //  Aggiungo alle features da sottrarre eventuali CIANFRINI
            //
            if (externalAngle > 0)
            {
                Line line = new Line(Plane.XZ, X, -1, X, SB + surplus);
                curves.Clear();
                curves.Add(line);
                double depth = TB - landing - internalDepth;
                Brep chamfer = null;
                EyeUtils.Chamfer(externalAngle, depth, sSide + "e" + sVX, wp, curves, brepTolerance, ref chamfer);
                features.Add(chamfer);
            }
            if (internalAngle > 0)
            {
                double height = SB / 2 - TA / 2 - internalBreak;
                Line line = new Line(Plane.XZ, X, -1, X, height);
                curves.Clear();
                curves.Add(line);
                double depth = internalDepth;
                Brep chamfer = null;
                EyeUtils.Chamfer(internalAngle, depth, sSide + "i" + sVX, wp, curves, brepTolerance, ref chamfer);
                features.Add(chamfer);
                height = SB / 2 + TA / 2 + internalBreak;
                line = new Line(Plane.XZ, X, height, X, SB + surplus);
                curves.Clear();
                curves.Add(line);
                EyeUtils.Chamfer(internalAngle, depth, sSide + "i" + sVX, wp, curves, brepTolerance, ref chamfer);
                features.Add(chamfer);
            }

            return true;
        }
        
    }
}
