using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG04 : EyeMacroLung
    {

        public LUNG04(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            macroPoint.Add(new ProgramPoint(ParA, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParD));
            macroPoint.Add(new ProgramPoint(Lp - ParB, SA - ParC));
            macroPoint.Add(new ProgramPoint(Lp - ParB, SA));

            EyeGeometryUtils.AddCurves(macroPoint, "C", Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);

            ProgrammedCurves.AddRange(curves);

            curves.Clear();
            macroPoint.Clear();
            macroPoint.Add(new ProgramPoint(ParA, SB - ParF));
            macroPoint.Add(new ProgramPoint(ParA, 0));

            EyeGeometryUtils.AddCurves(macroPoint, "A", Wp, TolLinear, TolAngle, out curves);

            ProgrammedCurves.AddRange(curves);

            curves.Clear();
            macroPoint.Clear();
            macroPoint.Add(new ProgramPoint(Lp - ParB, SB - ParF));
            macroPoint.Add(new ProgramPoint(Lp - ParB, 0));

            EyeGeometryUtils.AddCurves(macroPoint, "B", Wp, TolLinear, TolAngle, out curves);

            ProgrammedCurves.AddRange(curves);

            return true;
        }

    }
}
