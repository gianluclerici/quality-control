using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG22 : EyeMacroLung
    {
        public LUNG22(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUQ";
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

            double offsetY;

            double period = ParB;
            int nPeriodi = (int)((Lp - ParC) / period);

            //  Ricalcolo il periodo
            period = (Lp - ParC) / nPeriodi;

            double actualD = CodePrf == "U" ? -ParD : ParD;

            for (int counter = 0; counter < nPeriodi; counter++)
            {
                offsetY = CodePrf == "U" ? ParA / 2 : SB / 2;
                // Ala A ALTA
                if (CodePrf != "I")
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParA / 2 - actualD));
                else
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParA / 2 + ParF));
                macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParA / 2));
                macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY + ParA / 2));
                if (CodePrf != "I")
                    macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY + ParA / 2 - actualD));
                EyeGeometryUtils.AddCurves(macroPoint, "A", Wp, TolLinear, TolAngle, out curves);
                ProgrammedCurves.AddRange(curves);
                curves.Clear();
                macroPoint.Clear();

                // Ala A BASSA
                if (CodePrf != "U")
                {
                    if (CodePrf != "I")
                        macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY - (ParA / 2 - actualD)));
                    else
                        macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY - (ParA / 2 + ParF)));
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY - ParA / 2));
                    macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY - ParA / 2));
                    if (CodePrf != "I")
                        macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY - (ParA / 2 - actualD)));
                    else
                        macroPoint.Add(new ProgramPoint(period * (counter + 1), ParD));
                    EyeGeometryUtils.AddCurves(macroPoint, "A", Wp, TolLinear, TolAngle, out curves);
                    ProgrammedCurves.AddRange(curves);
                    curves.Clear();
                    macroPoint.Clear();
                }

                offsetY = CodePrf == "U" ? ParE / 2 : SB / 2;
                // Ala B ALTA
                if (CodePrf != "I")
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParE / 2 - actualD));
                else
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParE / 2 + ParF));
                macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParE / 2));
                macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY + ParE / 2));
                if (CodePrf != "I")
                    macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY + ParE / 2 - actualD));
                EyeGeometryUtils.AddCurves(macroPoint, "B", Wp, TolLinear, TolAngle, out curves);
                ProgrammedCurves.AddRange(curves);
                curves.Clear();
                macroPoint.Clear();

                // Ala B BASSA
                if (CodePrf != "U")
                {
                    if (CodePrf != "I")
                        macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY - (ParE / 2 - actualD)));
                    else
                        macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY - (ParE / 2 + ParF)));
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY - ParE / 2));
                    macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY - ParE / 2));
                    if (CodePrf != "I")
                        macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY - (ParE / 2 - actualD)));
                    else
                        macroPoint.Add(new ProgramPoint(period * (counter + 1), ParD));
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