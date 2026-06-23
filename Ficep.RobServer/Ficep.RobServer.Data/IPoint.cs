using Ficep.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Data
{
    public interface IPoint
    {
        double X { get; set; }
        double Y { get; set; }
        double Z { get; set; }

        public bool AreCoordinatesEqual(IPoint other, double tol = 0.01)
        {
            return X.IsEqualTo(other.X, tol) && Y.IsEqualTo(other.Y, tol) && Z.IsEqualTo(other.Z, tol);
        }
    }

}
