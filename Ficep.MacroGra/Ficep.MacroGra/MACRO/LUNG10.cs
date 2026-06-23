using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class LUNG10 : EyeMacroLung
    {

        public LUNG10(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
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

            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            macroPoint.Add(new ProgramPoint(0, TB));
            macroPoint.Add(new ProgramPoint(ParA, TB));
            macroPoint.Add(new ProgramPoint(ParB + ParA, SA / 2));
            macroPoint.Add(new ProgramPoint(ParA, SA - TB));
            macroPoint.Add(new ProgramPoint(0, SA - TB));

            List<Brep> breps = new List<Brep>();
            EyeGeometryUtils.AddContourExtrusion(macroPoint, "C", SB, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            macroPoint.Clear();
            macroPoint.Add(new ProgramPoint(ParA + ParB, SA / 2));
            macroPoint.Add(new ProgramPoint(Lp, SA / 2));

            EyeGeometryUtils.AddCurves(macroPoint, "C", Wp, TolLinear, TolAngle, out List<IEyeCurve> eyeCurves); 

            ProgrammedCurves.AddRange(eyeCurves);

            return true;
        }

    }
}
