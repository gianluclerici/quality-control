
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class ESTI09 : EyeMacro
	{
		public ESTI09(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "ILUQR";
        }
        public bool CreateMacroR()
        {
            List<Brep> breps;

            double totalLenght = Params.A + Params.R,
                   width = Params.C,
                   innerRadius = Wp.Prf.SA / 2 - Wp.Prf.TA,
                   outerRadius = Wp.Prf.SA / 2;

            if (Params.VX == "I")
            {
                if (Params.D == 0)
                    EyeUtils.CreateRadialAperture(width, totalLenght, 0, innerRadius, outerRadius, TolBrep, false, out breps, Params.R, 0, Surplus, false, true); //  Estensione dell'estrusione oltre i contorni profilo, false, true);
                else if (Params.D == 1)
                    EyeUtils.CreateRadialAperture(width, totalLenght, 180, innerRadius, outerRadius, TolBrep, false, out breps, Params.R, 0, Surplus, false, true);
                else
                    EyeUtils.CreateRadialAperture(width, totalLenght, 0, innerRadius, outerRadius, TolBrep, true, out breps, Params.R, 0, Surplus, false, true);
            }
            else
            {
                if (Params.D == 0)
                    EyeUtils.CreateRadialAperture(width, totalLenght, 0, innerRadius, outerRadius, TolBrep, false, out breps, Params.R, 0, Surplus, true, false);
                else if (Params.D == 1)
                    EyeUtils.CreateRadialAperture(width, totalLenght, 180, innerRadius, outerRadius, TolBrep, false, out breps, Params.R, 0, Surplus, true, false);
                else
                    EyeUtils.CreateRadialAperture(width, totalLenght, 0, innerRadius, outerRadius, TolBrep, true, out breps, Params.R, 0, Surplus, true, false);

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
                return CreateMacroR();

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double topY = CodePrf == "L" ? SA : SB;
            //
            //  ESTRUSIONE ANIMA
            //
            string extrusionPlane = CodePrf == "Q" && ParD.IsEqualTo(1, TolLinear) ? "D" : CodePrf == "L" ? "B" : "C";
            double extrusionDepth = CodePrf == "Q" && (ParD.IsEqualTo(0, TolLinear) || ParD.IsEqualTo(1, TolLinear)) ? TB : topY;

            macroPoint.Add(new ProgramPoint(0, ParB + ParC / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParR, ParB + ParC / 2, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParR, ParB - ParC / 2, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParB - ParC / 2, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
            
            return true;
        }
	}
}
