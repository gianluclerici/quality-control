using Ficep.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Data
{
    public class ProgramPoint : IPoint, IEquatable<ProgramPoint>, ICloneable
    {
        /// <summary>
        /// Point x coordinate
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Point y coordinate
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// Point z coordinate
        /// </summary>
        public double Z { get; set; }
        /// <summary>
        /// Rounding radius for corners
        /// </summary>
        public double Radius { get; set; }
        /// <summary>
        /// Arc radius. It needs to be specified just for the final point of the arc. if the value is greater than zero  
        /// the arc is counterclockwise else it is clockwise
        /// </summary>
        public double ArcRadius { get; set; }

        public ProgramPoint(double x, double y, double z = 0, double r = 0, double arcRadius = 0)
        {
            X = x;
            Y = y;
            Z = z;
            Radius = r;
            ArcRadius = arcRadius;
        }

        public ProgramPoint()
        {
            X = 0;
            Y = 0;
            Z = 0;
            Radius = 0;
        }

        public bool AreCoordinatesEqual(ProgramPoint other, double tol = 0.01)
        {
            return ((IPoint)this).AreCoordinatesEqual(other, tol);
        }
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (this.GetType() != obj.GetType())
                return false;
            return Equals((ProgramPoint)obj);
        }

        public bool Equals(ProgramPoint ProgramPoint, double tol)
        {
            if (ProgramPoint is null)
                return false;
            if (ReferenceEquals(this, ProgramPoint))
                return true;
            return X.IsEqualTo(ProgramPoint.X, tol) && Y.IsEqualTo(ProgramPoint.Y, tol) && Z.IsEqualTo(ProgramPoint.Z, tol) &&
                   Radius.IsEqualTo(ProgramPoint.Radius, tol);
        }

        public bool Equals(ProgramPoint ProgramPoint)
        {
            double tol = 0.01;
            if (ProgramPoint is null)
                return false;
            if (ReferenceEquals(this, ProgramPoint))
                return true;
            return X.IsEqualTo(ProgramPoint.X, tol) && Y.IsEqualTo(ProgramPoint.Y, tol) && Z.IsEqualTo(ProgramPoint.Z, tol) &&
                   Radius.IsEqualTo(ProgramPoint.Radius, tol);
        }

        // Override GetHashCode per mantenere la coerenza con Equals
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                hash = hash * 23 + Z.GetHashCode();
                hash = hash * 23 + Radius.GetHashCode();
                return hash;
            }
        }

        public object Clone()
        {
            return new ProgramPoint(X, Y, Z, Radius, ArcRadius);
        }
        public static bool operator ==(ProgramPoint left, ProgramPoint right) => left.Equals(right);
        public static bool operator !=(ProgramPoint left, ProgramPoint right) => !left.Equals(right);
    }
}
