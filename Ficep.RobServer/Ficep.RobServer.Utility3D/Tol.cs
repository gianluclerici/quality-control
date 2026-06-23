using Ficep.RobServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Utility3D
{
    public class Tol : ITol
    {

        public double Linear { get; private set; }
        public double Angle { get; private set; }

        
        /// <summary>
        /// Tolerance used in brep construction
        /// </summary>
        public double Brep { get; private set; }
        /// <summary>
        /// Minimum distance between for extrusions close to Flange/Web
        /// </summary>
        public double WebFlange { get; private set; }

        /// <summary>
        /// Set default values of tolerances
        /// </summary>
        public Tol()
        {
            Linear = 0.1;
            Angle = 0.01;
            Brep = 0.001;
            WebFlange = 0.01;
        }

        public Tol(double distance, double angle, double brep, double thickness)
        {
            Linear = distance;
            Angle = angle;
            Brep = brep;
            WebFlange = thickness;
        }
    }

    public class EyeParam
    {

        /// <summary>
        /// Extrusion extension beyond the profile boundaries
        /// </summary>
        public double Surplus { get; private set; }
        /// <summary>
        /// Inner chamfers distance from web when splitted in 2 cuts
        /// </summary>
        public double InnerChamferDisFromWeb { get; private set; }
        /// <summary>
        /// Class containing the tolerances
        /// </summary>
        public Tol Tol { get; private set; }

        /// <summary>
        /// Set the default values of the parameters
        /// </summary>
        public EyeParam()
        {
            Tol = new Tol();
            Surplus = 1;
            InnerChamferDisFromWeb = 2;
        }

        public EyeParam(double distance, double angle, double brep, double thickness, double surplus, double innerChamferDisFromWeb)
        {
            Tol = new Tol(distance, angle, brep, thickness);
            Surplus = surplus;
            InnerChamferDisFromWeb = innerChamferDisFromWeb;
        }
    }
}
