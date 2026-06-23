using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG30 : EyeMacroLung
    {
        public LUNG30(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "Q";
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

            double topY = SB;
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParA));
            macroPoint.Add(new ProgramPoint(Lp, ParD));
            macroPoint.Add(new ProgramPoint(Lp - ParC, 0));

            EyeGeometryUtils.AddCurves(macroPoint, "C", Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            EyeGeometryUtils.AddCurves(macroPoint, "D", Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);

            macroPoint.Clear();
            curves.Clear();

            if(ParC > 0)
            {
                macroPoint.Add(new ProgramPoint(0, 0));
                macroPoint.Add(new ProgramPoint(0, topY));
                macroPoint.Add(new ProgramPoint(Lp - ParC, topY));
                macroPoint.Add(new ProgramPoint(Lp - ParC, 0));

                EyeGeometryUtils.AddCurves(macroPoint, "A", Wp, TolLinear, TolAngle, out curves); // da aggiungersi inclinazione per matchare il taglio proveniente dalle anime
                ProgrammedCurves.AddRange(curves);
            }

            return true;
        }

    }
}