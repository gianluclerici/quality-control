
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
	public class SCAI10 : EyeMacro
	{

		public SCAI10(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IU";
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

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> programPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            //
            //  ESTRUSIONE ANIMA
            //
            string extrusionPlane = "C";
            double extrusionDepth = SB;

            programPoint.Add(new ProgramPoint(0, 0, 0, 0));
            programPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            programPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            programPoint.Add(new ProgramPoint(ParC, ParB, 0, 0));
            programPoint.Add(new ProgramPoint(ParC, SA - TB, 0, 0));
            programPoint.Add(new ProgramPoint(0, SA - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(programPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

	}
}
