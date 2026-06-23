using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG09 : EyeMacroLung
    {

        public LUNG09(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            string extrusionPlane = "C";
            double width = SA;
            
            double periodo = 2 * ParR + ParD;
            int nPeriodi = (int)((Lp - ParA - 3 * ParR - 3 * ParC) / (2 * ParR + ParD));

            // Taglio basso
            macroPoint.Add(new ProgramPoint(ParA, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB));

            macroPoint = LongCut.SemiCircleCycleCut(true, false, nPeriodi, ParD, ParR, ParC, ParC, macroPoint);

            macroPoint.Add(new ProgramPoint(ParA + periodo * nPeriodi + 2 * (ParC + ParR), 0));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            // Taglio alto
            macroPoint.Add(new ProgramPoint(ParA + ParC + ParR, width));
            macroPoint.Add(new ProgramPoint(ParA + ParC + ParR, width - ParB));


            macroPoint = LongCut.SemiCircleCycleCut(true, true, nPeriodi, ParD, ParR, ParC, ParC, macroPoint);
            macroPoint.Add(new ProgramPoint(ParA + periodo * nPeriodi + 3 * (ParC + ParR), width));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            return true;
        }
    }
}
