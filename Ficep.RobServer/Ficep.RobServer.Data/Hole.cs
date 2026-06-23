using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Data
{
    public interface Hole2D
    {
        /// <summary>
        /// Diameter
        /// </summary>
        double D { get; set; }
        /// <summary>
        /// Center x coordinate
        /// </summary>
        double Xc { get; set; }
        /// <summary>
        /// Center y coordiante
        /// </summary>
        double Yc { get; set; }
        /// <summary>
        /// Plane where the hole is defined
        /// </summary>
        string Plane { get; set; }
    }
}
