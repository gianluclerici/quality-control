using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG13 : EyeMacroLung
    {

        public LUNG13(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            string extrusionPlane = "C";
            double width = SA;

            List<ProgramPoint> macroPoint = new List<ProgramPoint>();

            //Taglio basso
            macroPoint.Add(new ProgramPoint(0, ParA));
            macroPoint.Add(new ProgramPoint(Lp, ParB));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            //Taglio alto
            macroPoint.Add(new ProgramPoint(0, width - ParB));
            macroPoint.Add(new ProgramPoint(Lp, width - ParA));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);

            return true;
        }

    }
}
