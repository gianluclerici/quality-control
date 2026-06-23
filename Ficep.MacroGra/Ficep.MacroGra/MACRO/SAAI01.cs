
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
	public class SAAI01 : EyeMacro
	{

		public SAAI01(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
        }

        public override bool CreateMacro()
		{
            ///////////////////////////////
            //      TESTA: fissa
            ///////////////////////////////
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            ///////////////////////////////
            //      CORPO: variabile
            ///////////////////////////////

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();
            double extrusionDepth = TB + Radius;
            string extrusionPlane = Side;

            double topY = 0, offsetY = 0;

            if (CodePrf == "I")
            {
                topY = SB;
                offsetY = SB / 2;
            }
            else if (CodePrf == "L")
                topY = MirrorSideASideB ? SB : SA;
            else
                topY = SB;

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB + offsetY, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE, ParD - (ParB - ParD) * ParE / ParA + offsetY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD - ParC + offsetY, 0, 0));

            //
            //  mirrorYZ    INIZIALE / FINALE
            //  mirrorXZ    RIBALTAMENTO ALA FISSA/MOBILE SOLO PER PIANO C
            //  mirrorXY    ALTO / BASSO
            //
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

	}
}
