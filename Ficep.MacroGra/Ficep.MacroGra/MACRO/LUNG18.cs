using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG18 : EyeMacroLung
    {
        public LUNG18(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            string extrusionPlane = "C";

            //
            // Taglio anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParG, ParD - ParL, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParG + ParH, ParD - ParL + ParI, 0, 0));
            macroPoint.Add(new ProgramPoint(Lp - ParA, ParC, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(Lp, 0));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////
            return true;
        }

    }
}