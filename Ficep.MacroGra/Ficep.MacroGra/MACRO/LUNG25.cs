using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG25 : EyeMacroLung
    {
        public LUNG25(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();

            double width = SA;
            string extrusionPlane = "C";

            //  Linea FF
            macroPoint.Add(new ProgramPoint(0, ParA - ParE));
            macroPoint.Add(new ProgramPoint(ParD, ParA - ParE, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD, ParA));
            macroPoint.Add(new ProgramPoint(Lp - ParF, ParA));
            macroPoint.Add(new ProgramPoint(Lp - ParF, ParA - ParG, 0, ParR));
            macroPoint.Add(new ProgramPoint(Lp, ParA - ParG));
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();
            //  Linea FM
            macroPoint.Add(new ProgramPoint(0, width - (ParA - ParE)));
            macroPoint.Add(new ProgramPoint(ParD, width - (ParA - ParE), 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD, width - ParA));
            macroPoint.Add(new ProgramPoint(Lp - ParF, width - ParA));
            macroPoint.Add(new ProgramPoint(Lp - ParF, width - (ParA - ParG), 0, ParR));
            macroPoint.Add(new ProgramPoint(Lp, width - (ParA - ParG)));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);

            ProgrammedCurves.AddRange(curves);

            return true;
        }
    }
}