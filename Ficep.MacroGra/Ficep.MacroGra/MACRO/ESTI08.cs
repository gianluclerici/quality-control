
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class ESTI08 : EyeMacro
	{

		public ESTI08(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQR";
        }

        public bool CreateMacroR()
		{
            List<Brep> breps;

			double totalLenght = Params.A,
				   width = Params.B,
				   innerRadius = Wp.Prf.SA / 2 - Wp.Prf.TA,
				   outerRadius = Wp.Prf.SA / 2;

            if (Params.VX == "I")
                EyeUtils.CreateRadialAperture(width, totalLenght, 90, innerRadius, outerRadius, TolBrep, true, out breps, 0, 0, Surplus, false, false, true);
			else
			{
                EyeUtils.CreateRadialAperture(width, totalLenght, 90, innerRadius, outerRadius, TolBrep, true, out breps, 0, 0, Surplus, false, false, true);
				for (int i = 0; i < breps.Count; i++) 
				{
					Brep brep = breps[i];
					brep.Translate(Wp.Lp - totalLenght + Surplus, 0);
				}
            }
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
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
                return CreateMacroR ();

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double topY = CodePrf == "L" ? SA : SB,
                   offsetY = CodePrf == "I" || CodePrf == "Q" ? SB / 2 : 0,
                   width = CodePrf == "L" ? SB : SA;
            offsetY += CodePrf == "Q" ? ParC : 0;
            string extrusionPlane = "A";
            double extrusionDepth = width;

            //
            //  ESTRUSIONE unica da ala FF
            //
            macroPoint.Add(new ProgramPoint(0, offsetY + ParB / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParB / 2, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, offsetY - ParB / 2, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, offsetY - ParB / 2, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            if ( CodePrf == "L")
            {
                //  Estrusione triangolare su piano B
                macroPoint.Clear();
                extrusionPlane = "B";
                extrusionDepth = topY;

                macroPoint.Add(new ProgramPoint(0, width - ParD));
                macroPoint.Add(new ProgramPoint(ParA + TolWebFlange, width - ParD));
                macroPoint.Add(new ProgramPoint(ParA + ParC, width));
                macroPoint.Add(new ProgramPoint(0, width));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }
            if (CodePrf == "Q" && !ParALFA.IsEqualTo(0, TolAngle))
            {                
                //  Cianfrini esterni                
                double chamferDepth = TB;

                ProgramPoint startChamfer = new ProgramPoint(0, offsetY + ParB / 2, 0, 0);
                ProgramPoint endChamfer = new ProgramPoint(ParA, offsetY + ParB / 2, 0, 0);

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "A", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "B", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, true))
                    breps.Add(chamferA);
                startChamfer = new ProgramPoint(ParA, offsetY - ParB / 2, 0, 0);
                endChamfer = new ProgramPoint(0, offsetY - ParB / 2, 0, 0);

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "A", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, true))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "B", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, true))
                    breps.Add(chamferA);
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}
