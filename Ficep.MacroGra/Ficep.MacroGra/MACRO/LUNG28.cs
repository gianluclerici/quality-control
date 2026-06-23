using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG28 : EyeMacroLung
    {
        public LUNG28(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            string extrusionPlane = "C";

            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            macroPoint.Add(new ProgramPoint(ParD, ParA));
            macroPoint.Add(new ProgramPoint(Lp - ParE, ParF));

            if (!(CodePrf == "Q" && ParH.IsEqualTo(1, TolLinear)))
            {
                EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
                ProgrammedCurves.AddRange(curves);
            }
            if (CodePrf == "Q" && (ParH.IsEqualTo(1, TolLinear) || ParH.IsEqualTo(2, TolLinear)))
            {
                EyeGeometryUtils.AddCurves(macroPoint, "D", Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
                ProgrammedCurves.AddRange(curves);
            }

            return true;
        }
    }
}