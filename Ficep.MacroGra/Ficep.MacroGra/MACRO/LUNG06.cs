using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class LUNG06 : EyeMacroLung
    {

        public LUNG06(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            List<Brep> breps = new List<Brep>();

            double width = SA, topY = SB;

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double tanAlfa = Math.Tan(ParALFA.ToRad());
            double REG_21 = Math.Atan((ParA - TB) / (Lp - (width + ParA - TB) * tanAlfa));
            double REG_22 = Math.Sqrt(Math.Abs(ParS * ParS - (ParB - ParS) * (ParB - ParS)));

            //Estrusioni anima (fatte cosi perchè pegaso on prevede alfa negativo?)
            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(new ProgramPoint(width * tanAlfa, width));
            macroPoint.Add(new ProgramPoint(0, width));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            macroPoint.Add(new ProgramPoint(Lp, width));
            macroPoint.Add(new ProgramPoint(Lp - width * tanAlfa, 0));
            macroPoint.Add(new ProgramPoint(Lp, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //  Primo pezzo
            macroPoint.Add(new ProgramPoint(ParA * tanAlfa - ParR * Math.Sin(ParALFA.ToRad()), ParA - ParR * Math.Cos(ParALFA.ToRad())));
            macroPoint.Add(new ProgramPoint(ParA * tanAlfa + ParR * Math.Cos(REG_21), ParA - ParR * Math.Sin(REG_21), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(Lp - (width - TB) * tanAlfa - (ParB + ParC - TB) / Math.Tan(REG_21), ParB + ParC));
            macroPoint.Add(new ProgramPoint(Lp - (width - TB) * tanAlfa - (ParB + ParC - TB) / Math.Tan(REG_21) + REG_22, ParC, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(Lp - (width - ParC) * tanAlfa, ParC));

            EyeGeometryUtils.AddCurves(macroPoint, "C", Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            curves.Clear();
            macroPoint.Clear();

            //  Secondo pezzo
            macroPoint.Add(new ProgramPoint(Lp - (ParA * tanAlfa - ParR * Math.Sin(ParALFA.ToRad())), width - (ParA - ParR * Math.Cos(ParALFA.ToRad())), 0, 0));
            macroPoint.Add(new ProgramPoint(Lp - (ParA * tanAlfa + ParR * Math.Cos(REG_21)), width - (ParA - ParR * Math.Sin(REG_21)), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint((width - TB) * tanAlfa + (ParB + ParC - TB) / Math.Tan(REG_21), width - (ParB + ParC)));
            macroPoint.Add(new ProgramPoint((width - TB) * tanAlfa + (ParB + ParC - TB) / Math.Tan(REG_21) - REG_22, width - ParC, 0, 0, ParS)) ;
            macroPoint.Add(new ProgramPoint((width - ParC) * tanAlfa, width - ParC));

            EyeGeometryUtils.AddCurves(macroPoint, "C", Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
            return true;
        }

    }
}
