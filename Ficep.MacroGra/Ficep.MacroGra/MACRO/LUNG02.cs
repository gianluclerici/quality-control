using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG02 : EyeMacroLung
    {
        public LUNG02(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
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


            // Devo richiedere ParD < ParA / 2 ??????

            string extrusionPlane = CodePrf == "L" ? "B" : "C";

            double halfWidth = CodePrf == "L" ? SB / 2 : SA / 2;

            //
            //  Tratto iniziale
            double startX = 0; //   Ignoro stich iniziale (altrimenti startX = ParD)
            macroPoint.Add(new ProgramPoint(Lp - startX, halfWidth - ParB / 2));//1
            macroPoint.Add(new ProgramPoint(Lp - ParA / 2, halfWidth - ParB / 2));//2

            //
            //  Taglio ciclico lungo l'anima
            //
            double periodo = 2 * (ParA + ParC);
            int nPeriodi = (int)Math.Floor((Lp - ParA / 2) / periodo);
            double spazioRimanente = Lp - nPeriodi * periodo - ParA / 2;            
            double endX = 0;// Ignoro stich finale ( altrimenti endX = ParD)

            macroPoint = LongCut.TrapezoidalCycleCut(false, false, nPeriodi, ParA, ParB, ParC, macroPoint);

            //
            //  Ultimo tratto.
            //
            macroPoint = LongCut.TrapezoidalLastCut(false, false, spazioRimanente, endX, ParA, ParB, ParC, macroPoint);

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);

            return true;
        }

    }
}