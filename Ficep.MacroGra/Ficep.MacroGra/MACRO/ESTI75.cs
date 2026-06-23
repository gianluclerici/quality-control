
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System.Collections.Generic;
using System.Linq;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class ESTI75 : EyeMacro
	{

		public ESTI75(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "QR";
        }
        public bool CreateMacroR()
        {
            Circle circle = new Circle(Plane.YZ, Wp.Prf.SA / 2 - Wp.Prf.TA);
			circle.Translate(0, Wp.Prf.SA / 2, Wp.Prf.SA / 2);
			List<ICurve> list = new List<ICurve>();
			Brep chamfer;
		    EyeUtils.ChamferRoundTube(circle, Params.A, Params.ALFA, Wp, out  chamfer, Surplus, TolBrep);

			if (Params.VX == "I")
				Features.Add(new EyeFeature(chamfer));
			else
			{
                chamfer.Translate(Surplus, 0);
                EyeUtils.MirrorBrep(Wp, "C", ref chamfer, true);
                Features.Add(new EyeFeature(chamfer));
            }
			
			return true;
        }
        public override bool CreateMacro()
		{
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            if (CodePrf == "R")
                return CreateMacroR();
            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<Brep> breps = new List<Brep>();

            double topY = SB;

            //  Cianfrini esterni                
            double chamferDepth = TB - ParA;

            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "A", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "B", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "C", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
                //if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "D", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                //    breps.Add(chamferA);
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
	}
}
