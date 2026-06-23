
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class EST94 : EyeMacro
	{

		public EST94(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "R";
        }

        public override bool CreateMacro()
		{
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            double radALFA = ParALFA.ToRad();
            double radALFAabs = Math.Abs(radALFA);
            // Compute the lenght of the feature and the temporary cylinder
            double extrusionLenghtFeature = 2 * Params.R * Math.Tan(radALFAabs) + Wp.Prf.SA / Math.Cos(radALFA) + Surplus;
            double extrusionLenghtTemp = 2 * Params.R / Math.Cos(radALFA) + Wp.Prf.SA * Math.Tan(radALFAabs) + Surplus;

            // Create the cylinder and translate it at the right position
            Region circle = Region.CreateCircle(Plane.XZ, Params.R);
            Brep feature = circle.ExtrudeAsBrep(extrusionLenghtFeature, 0, TolBrep);
            feature.Translate(0, extrusionLenghtFeature / 2);
            feature.Rotate(radALFA, Vector3D.AxisZ);
            // Translate the mid point of the feature at the centre of the tube 
            feature.Translate(0, Wp.Prf.SA / 2);

            // Create the temporary cylinder, needed just to compute the intersections loops
            // Parametro tolleranza messo a 0.01
            Brep temp = CreateCylinder(Wp.Prf.SA / 2, extrusionLenghtTemp);

            feature.Translate(extrusionLenghtTemp / 2, 0, Wp.Prf.SA / 2);

            // Compute the minimum and maxium points of the intersection curve 
            ICurve[] curves;
            Brep.IntersectionLoops(temp, feature, out curves);
            List<Point3D> points = new List<Point3D>();
            Point3D min, max;
            curves = curves.Where(c => c.IsClosed && (c is CompositeCurve || c is Curve)).ToArray();
            foreach (var curve in curves) 
            {
                curve.GetTightBBox(out min, out max);
                points.Add(min);
                points.Add(max);
            }

            // Get the ordered list of points with respect to the X coordinate of the bounding box for each curve in ascending order
            // the list will have always 4 points since the curves have to be 2
            points = points.OrderBy(p => p.X).ToList();
                
            if (Params.VX == "F")
            {
                // Get the second maximum point in the list and translate back the feature of that value
                min = points[1];
                feature.Translate((Wp.Lp - min.X) - TolLinear, 0);
            }
            else
            {
                // Get the second maximum point in the list and translate back the feature of that value
                max = points[2];
                feature.Translate(-max.X + TolLinear, 0);
            }

            Features.Add(new EyeFeature(feature));
            return true;
        }

        private Brep CreateCylinder(double r , double lenght)
        {
            Region circle = Region.CreateCircle(Plane.YZ, r);
            // Parametro tolleranza messo a 0.01
            Brep feature = circle.ExtrudeAsBrep(lenght, 0, TolBrep);
            feature.Translate(0, r, r);
            return feature;
        }
    }
}
