using Ficep.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.AnyCut.Mathematics
{
    //  Implementazione double della classe Vector3 (float)
    public struct Vector3D
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3D Normalize()
        {
            double length = Length;
            return FMath.Equal(length, 0, 0.01) ? new Vector3D(0, 0, 0) : new Vector3D(X / length, Y / length, Z / length);
        }

        public static Vector3D Zero ()
        {
            return new Vector3D(0, 0, 0);
        }

        public static Vector3D One()
        {
            return new Vector3D(1.0, 1.0, 1.0);
        }

        public static Vector3D Normalize(Vector3D? v)
        {
            if (!v.HasValue)
                return Vector3D.Zero();

            double length = v.Value.Length;
            if (FMath.Equal(length, 0, 0.01))
                return Vector3D.Zero();

            return new Vector3D(v.Value.X / length, v.Value.Y / length, v.Value.Z / length);
        }
        public static Vector3D Normalize(Vector3D v)
        {
            return new Vector3D(v.X, v.Y, v.Z).Normalize();
        }

        //
        //  Prodotto scalare tra due vettori normalizzati
        //  Il prodotto scalare viene limitato tra -1 e 1
        //  per evitare problemi di precisione numerica nell'utilizzo
        //  della funzione Math.Acos() che accetta valori tra -1 e 1
        //
        public static double DotNormalized(Vector3D a, Vector3D b)
        {
            a.Normalize();
            b.Normalize();
            double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            if (dot > 1.0)
                dot = 1.0;
            else if (dot < -1.0)
                dot = -1.0;

            return dot;
        }

        public static Vector3D Cross(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public static Vector3D Lerp(Vector3D value1, Vector3D value2, double amount)
        {
            return (value1 * (1 - amount)) + (value2 * amount);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(Vector3D v, double scalar) => new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        public static Vector3D operator *(double scalar, Vector3D v) => new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        public static Vector3D operator /(Vector3D v, double scalar) => new Vector3D(v.X / scalar, v.Y / scalar, v.Z / scalar);
        public static Vector3D operator -(Vector3D v)
        {
            return new Vector3D(-v.X, -v.Y, -v.Z);
        }
    }

    public class FMathExt2
    {
        // Dati due punti di un percorso con raggio del percorso con segno, calcola le tangenti iniziali e finali 
        // del percorso non normalizzate
        // La direzione della tangente dipende dal senso del percorso, senza saperlo non è possibile calcolarla
        public static bool ComputePathTangents(Point start, Point end, double radius, out Vector3D startTangent, out Vector3D endTangent)
        {
            startTangent = Vector3D.Zero();
            endTangent = Vector3D.Zero();

            if (start == null || end == null)
                return false;

            bool isArc = !radius.IsEqualTo(0, 0.001);

            if (isArc)
            {
                if(!ComputeArcTangents(start, end, radius, out startTangent, out endTangent))
                    return false;
            }
            else
            {
                startTangent = endTangent = new Vector3D((end.X - start.X), (end.Y - start.Y), (end.Z - start.Z));// endPosition - startPosition;
            }

            return true;
        }

        // Dati 2 punti di un arco di cerchio e il raggio con segno, calcola centro e vettore sc, ec che sono i vettori che 
        // congiungono centro c e punto s, centro c e punto e
        // arco antiorario se raggio > 0
        public static bool ComputeCentre(Point s, Point e, double radius, out Point? c, out Vector3D? cs, out Vector3D? ce)
        {
            c = null;
            cs = null;
            ce = null;

            if (!PossibleCenters(s, e, radius, out Point[] centers))
                return false;

            if (centers.Length == 2)
            {
                Point c1 = centers[0];

                Vector3D cs1 = new Vector3D(s.X - c1.X, s.Y - c1.Y, 0),
                        ce1 = new Vector3D(e.X - c1.X, e.Y - c1.Y, 0);

                bool arc1CCW = Vector3D.Cross(cs1, ce1).Z > 0;

                Point c2 = centers[1];

                Vector3D cs2 = new Vector3D(s.X - c2.X, s.Y - c2.Y, 0),
                        ce2 = new Vector3D(e.X - c2.X, e.Y - c2.Y, 0);

                bool arc2CCW = Vector3D.Cross(cs2, ce2).Z > 0;

                if (radius > 0)
                {
                    if (arc1CCW)
                    { 
                        c = c1;
                        cs = cs1;
                        ce = ce1;
                    }
                    else if (arc2CCW)
                    {
                        c = c2;
                        cs = cs2;
                        ce = ce2;
                    }
                    else
                        return false;
                }
                else if (radius < 0)
                {
                    if (!arc1CCW)
                    {
                        c = c1;
                        ce = ce1;
                        cs = cs1;
                    }
                    else if (!arc2CCW)
                    {
                        c = c2;
                        cs = cs2;
                        ce = ce2;
                    }
                    else
                        return false;
                }
            }
            else if (centers.Length == 1)
            {
                c = centers[0];
                cs = new Vector3D(s.X - c.X, s.Y - c.Y, 0);
                ce = new Vector3D(e.X - c.X, e.Y - c.Y, 0);
            }
            else
                return false;

            return true;
        }

        // Dati 2 punti di un arco di cerchio e il raggio con segno, calcolo la tangente sul punto iniziale e finale
        // arco antiorario se raggio > 0
        public static bool ComputeArcTangents(Point start, Point end, double radius, out Vector3D startTangent, out Vector3D endTangent)
        {
            startTangent = Vector3D.Zero();
            endTangent = Vector3D.Zero();
            bool ccw = radius > 0;

            if (!ComputeCentre(start, end, radius, out Point center, out Vector3D? cs, out Vector3D? ce))
                return false;
            
            ComputeArcTangents(start, center, end, ccw, out startTangent, out endTangent);

            return true;
        }

        // Dati inizio centro e fine di un arco calcola le tangenti di inizio e fine
        public static void ComputeArcTangents(Point start, Point center, Point end, bool ccw, out Vector3D startTangent, out Vector3D endTangent)
        {
            //

            // Costruisco i vettori che collegano start e end point al punto centro
            //

            startTangent = new Vector3D(start.X - center.X, start.Y - center.Y, 0);
            endTangent = new Vector3D(end.X - center.X, end.Y - center.Y, 0);

            //
            // Li ruoto di 90 gradi e ottengo le tangenti
            //

            double angle = ccw ? Math.PI / 2 : -Math.PI / 2;

            ComputeRotationAroundZ(startTangent.X, startTangent.Y, angle, out double xRotated, out double yRotated);
            startTangent.X = xRotated;
            startTangent.Y = yRotated;

            ComputeRotationAroundZ(endTangent.X, endTangent.Y, angle, out xRotated, out yRotated);
            endTangent.X = xRotated;
            endTangent.Y = yRotated;
        }

        // Date le coordinate xy di due punti appartenenti ad un arco di cerchio, il raggio
        // calcola i due centri di circonferenza passanti per quei due punti
        // ref: https://rosettacode.org/wiki/Circles_of_given_radius_through_two_points#C#
        public static bool PossibleCenters(Point p, Point q, double radius, out Point[] centers)
        {
            centers = null;

            radius = Math.Abs(radius);

            if (radius == 0)
            {
                if (p == q)
                    centers = new[] { p };
                else
                    return false; // No circles
            }
            if (p == q)
                return false; // Infinite number of circles

            double tol = 0.01;
            double sqDistance = (p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y);
            double sqDiameter = 4 * radius * radius;
            if (sqDistance.IsGreaterThan(sqDiameter, tol))
                return false; // Points are too far apart

            Point midPoint = new Point((p.X + q.X) / 2, (p.Y + q.Y) / 2, 0);
            if (sqDistance.IsEqualTo(sqDiameter, tol))
            {
                centers = new[] { midPoint };
                return true;
            }

            double d = Math.Sqrt(radius * radius - sqDistance / 4);
            double distance = Math.Sqrt(sqDistance);
            double ox = d * (q.X - p.X) / distance, oy = d * (q.Y - p.Y) / distance;
            centers = new[]
            {
                new Point(midPoint.X - oy, midPoint.Y + ox, 0),
                new Point(midPoint.X + oy, midPoint.Y - ox, 0)
            };

            return true;
        }

        // Funzione implementa l'algoritmo SLERP per interpolare tra due vettori v1 e v2
        // in base al parametro t (tra 0 e 1).
        public static Vector3D Slerp(Vector3D v1, Vector3D v2, double t)
        {
            // Esegue l'interpolazione lineare sferica (SLERP) tra due vettori.

            double dot = Vector3D.DotNormalized(v1, v2);
            double theta = FmathExt.Acos(dot);

            if (Math.Sin(theta) == 0)
            {
                return v1;
            }

            Vector3D interpolation = (Math.Sin((1 - t) * theta) / Math.Sin(theta)) * v1 + (Math.Sin(t * theta) / Math.Sin(theta)) * v2;

            return interpolation;
        }

        // Calcola l'angolo del punto di coordinate (x,y). L'angolo restituito è l'angolo compreso tra
        // l'asse x e una linea contenente l'origine (0,0) e un punto di coordinate (x,y)
        public static double ComputeLineAngle(double x, double y)
        {
            double angZ = 0;

            if (FMath.Equal(x, 0) && FMath.Equal(y, 0))
                angZ = 0;
            else
                angZ = Math.Atan2(y, x).ToDeg();

            if (angZ.IsLessThan(0, 0.01))
                angZ += 360;

            return angZ;
        }

        // Dati x,y,angle calcola la rotazione su z delle coordinate rispetto ad un centro xc,yc
        public static void ComputeRotationAroundZ(double x, double y, double radAngle, out double xRotated, out double yRotated, double xc = 0, double yc = 0)
        {
            double cosA = Math.Cos(radAngle),
                   sinA = Math.Sin(radAngle);

            // 1. Translate the point so that the center of rotation is at the origin (0, 0)
            double translatedX = x - xc;
            double translatedY = y - yc;

            // 2. Perform the rotation around the origin
            double rotatedX = translatedX * cosA - translatedY * sinA;
            double rotatedY = translatedX * sinA + translatedY * cosA;

            // 3. Translate the rotated point back to the original coordinate system
            xRotated = rotatedX + xc;
            yRotated = rotatedY + yc;
        }

        public static bool AreVectorsOnSameCuttingPlane(Vector3D pos1, Vector3D pos2, Vector3D vec1, Vector3D vec2, double tolerance)
        {
            // Compute the normal to the plane defined by pose1 and pose2 vectors
            Vector3D normal = Vector3D.Normalize(Vector3D.Cross(vec1, vec2));

            // Check if pos2 lies on the plane defined by pos1 and normal
            double distance = Vector3D.DotNormalized(normal, pos2 - pos1);

            return Math.Abs(distance) < tolerance;
        }

        // Calcola le componenti x,y,z del vettore dati i due angoli che lo definiscono:
        // bevelAngle, angolo tra l'asse Z ed il vettore 
        // angZ, angolo tra l'asse x e la proiezione del vettore nel piano XY
        public static bool ComputeVectorComponents(double bevelAngle, double angZ, ref double vx, ref double vy, ref double vz)
        {
            double radBevelAngle = bevelAngle.ToRad(), 
                   radAngZ = angZ.ToRad();

            if (radBevelAngle >= Math.PI / 2)
                return false;

            // Calcolo componenti dell'orientamento 
            vx = Math.Cos(radAngZ) * Math.Sin(radBevelAngle);
            vy = Math.Sin(radAngZ) * Math.Sin(radBevelAngle);
            vz = Math.Cos(radBevelAngle);

            return true;
        }
    }
}
