using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class LUNG14 : EyeMacroLung
    {

        public LUNG14(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA;
            string extrusionPlane = "C";

            double periodo = 2 * ParR + ParD;
            int nPeriodi = (int)((Lp - ParA - 3 * ParR - 3 * ParS) / periodo);
            double spazioRimanente = Lp - ParA - 3 * ParR - 3 * ParS - nPeriodi * periodo;

            //Pezzo FF

            macroPoint.Add(new ProgramPoint(spazioRimanente, 0, 0));
            macroPoint.Add(new ProgramPoint(spazioRimanente, ParB, 0));

            if (ParM.IsEqualTo(1, TolLinear))
                macroPoint = LongCut.SemiCircleCycleCut(true, false, nPeriodi, ParD, ParR, ParS, ParS, macroPoint, ParC, ParF, ParG, ParH);
            else
                macroPoint = LongCut.SemiCircleCycleCut(true, false, nPeriodi, ParD, ParR, ParS, ParS, macroPoint);

            macroPoint.Add(new ProgramPoint(macroPoint.Last().X, 0, 0));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            //Pezzo FM
            if (ParA > 0)
                macroPoint.Add(new ProgramPoint(Lp - ParA, width, 0));
            macroPoint.Add(new ProgramPoint(Lp - ParA, width - ParB, 0));

            if (ParL.IsEqualTo(1, TolLinear))
                macroPoint = LongCut.SemiCircleCycleCut(false, false, nPeriodi, ParD, ParR, ParS, ParS, macroPoint, ParC, ParF, ParG, ParH);
            else
                macroPoint = LongCut.SemiCircleCycleCut(false, false, nPeriodi, ParD, ParR, ParS, ParS, macroPoint);

            macroPoint.Add(new ProgramPoint(macroPoint.Last().X, width, 0));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            return true;
        }
    }
}
