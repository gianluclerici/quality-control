using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG12 : EyeMacroLung
    {

        public LUNG12(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            List<IEyeCurve> curves = new List<IEyeCurve>();

            double offsetY = SB / 2;

            double period = ParD;
            int nPeriodi = (int)((Lp - ParE) / period);
            //  Secondo me è più giusto:
            //int nPeriodi = (int)((Lp - ParE - ParC) / period);
            //
            // Recalculate the period?? from file.MAC
            //
            period = (Lp - ParE) / nPeriodi;
            
            for (int counter = 0; counter < nPeriodi; counter ++)
            {
                // Ala A
                macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParA + ParB));
                macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParA));
                macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY + ParA));
                EyeGeometryUtils.AddCurves(macroPoint, "A", Wp, TolLinear, TolAngle, out curves);
                ProgrammedCurves.AddRange(curves);
                curves.Clear();
                macroPoint.Clear();

                // Ala B
                if (ParF > 0)
                {
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParF + ParB));
                    macroPoint.Add(new ProgramPoint(ParC + period * counter, offsetY + ParF));
                    macroPoint.Add(new ProgramPoint(period * (counter + 1), offsetY + ParF));
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
