
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
	public class ESTI46 : EyeMacro
	{

		public ESTI46(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        public override bool CreateMacro()
		{
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            Brep feature = null;

            List<ICurve> curves = new List<ICurve>();
            Line line = new Line(-1, -1, -1, Wp.Prf.SA + 1);
            curves.Add(line);
            line = new Line(-1, Wp.Prf.SA + 1, Params.D, Wp.Prf.SA + 1);
            curves.Add(line);
            line = new Line(Params.D, Wp.Prf.SA + 1, Params.C, Params.F);
            curves.Add(line);
            line = new Line(Params.C, Params.F, Params.B, Params.E);
            curves.Add(line);
            line = new Line(Params.B, Params.E, Params.A, -1);
            curves.Add(line);
            line = new Line(Params.A, -1, -1, -1);
            curves.Add(line);
            Vector3D amount = new Vector3D(0, 0, Wp.Prf.SB);
            bool mirrorYZ = Params.VX == "I" ? false : true;

            /////////////////////////////////////////////////////////
            //  Sostituire con EyeGeometryUtils.AddContourExtrusion
            //
            double offsetX = 0, offsetY = 0, offsetZ = 0;
            double extrusionDepth = Wp.Prf.SB;
            EyeGeometryUtils.GetSolidSubtractOffsetAmount(Wp, "C", extrusionDepth, TolBrep, TolAngle, Surplus, ref amount, ref offsetX, ref offsetY, ref offsetZ);
            EyeUtils.SolidSubtract(curves, Wp, "C", amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, mirrorYZ);
            //////////////////////////////////////////////////

            Features.Add(new EyeFeature(feature));

            return true;
        }

	}
}
