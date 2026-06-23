
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class INTA22 : EyeMacro
	{

		public INTA22(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double radALFA = Params.ALFA.ToRad();
            // Lenght of the cylinder to be subtracted
            double lenght;
            double x,
                   y,
                   z = Wp.Prf.SA / 2;

            if (radALFA < 0)
            {
                lenght = Wp.Prf.SA / Math.Cos(radALFA) + Surplus - 2 * Params.R * Math.Tan(radALFA);
                x = Params.A - lenght * Math.Sin(radALFA);
                y = (lenght - Surplus / 2) * Math.Cos(radALFA) + Params.R * Math.Sin(radALFA);
            }
            else
            {
                lenght = Wp.Prf.SA / Math.Cos(radALFA) + Surplus + 2 * Params.R * Math.Tan(radALFA);
                x = Params.A - lenght * Math.Sin(radALFA);
                y = (lenght - Surplus / 2) * Math.Cos(radALFA) - Params.R * Math.Sin(radALFA);
            }

            // cylinder creation
            Region region = Region.CreateCircle(Plane.XZ, Params.R);
            Brep feature = region.ExtrudeAsBrep(lenght, 0, TolBrep);
            
            feature.Rotate(radALFA, Vector3D.AxisZ);

            feature.Translate(x, y, z);
            
            Plane plane = Plane.XZ;
            plane.Translate(0, Wp.Prf.SA / 2);
            feature.CutBy(plane, true);

            Features.Add(new EyeFeature(feature));

            return true;
        }

    }
}
