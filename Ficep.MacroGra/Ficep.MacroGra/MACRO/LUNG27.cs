using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG27 : EyeMacroLung
    {
        public LUNG27(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IQ";
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
            List<IEyeCurve> curves;

            double offsetY = SB / 2;

            double period = ParD;
            int nPeriodi = (int)((Lp - ParE - ParF + ParC) / period);

            //  Ricalcolo il periodo
            period = (Lp - ParE - ParF + ParC) / nPeriodi;

            for (int counter = 0; counter < nPeriodi; counter++)
            {
                if (ParG.IsEqualTo(1, TolLinear))
                {
                    // Ala A ALTA
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY + ParA + ParB));
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY + ParA));
                    macroPoint.Add(new ProgramPoint(ParF + period * (counter + 1) - ParC, offsetY + ParA));
                    EyeGeometryUtils.AddCurves(macroPoint, "A", Wp, TolLinear, TolAngle, out curves);
                    ProgrammedCurves.AddRange(curves);
                    curves.Clear();
                    macroPoint.Clear();
                    // Ala B ALTA
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY + ParA + ParB));
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY + ParA));
                    macroPoint.Add(new ProgramPoint(ParF + period * (counter + 1) - ParC, offsetY + ParA));
                    EyeGeometryUtils.AddCurves(macroPoint, "B", Wp, TolLinear, TolAngle, out curves);
                    ProgrammedCurves.AddRange(curves);
                    curves.Clear();
                    macroPoint.Clear();
                }
                if (ParH.IsEqualTo(1, TolLinear))
                {
                    // Ala A BASSA
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY - (ParA + ParB)));
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY - ParA));
                    macroPoint.Add(new ProgramPoint(ParF + period * (counter + 1) - ParC, offsetY - ParA));
                    EyeGeometryUtils.AddCurves(macroPoint, "A", Wp, TolLinear, TolAngle, out curves);
                    ProgrammedCurves.AddRange(curves);
                    curves.Clear();
                    macroPoint.Clear();
                    // Ala B BASSA
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY - (ParA + ParB)));
                    macroPoint.Add(new ProgramPoint(ParF + period * counter, offsetY - ParA));
                    macroPoint.Add(new ProgramPoint(ParF + period * (counter + 1) - ParC, offsetY - ParA));
                    EyeGeometryUtils.AddCurves(macroPoint, "B", Wp, TolLinear, TolAngle, out curves);
                    ProgrammedCurves.AddRange(curves);
                    curves.Clear();
                    macroPoint.Clear();
                }
            }
            return true;
        }

    }
}