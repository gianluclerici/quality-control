using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG16 : EyeMacroLung
    {

        public LUNG16(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, topY = SB;

            string extrusionPlane = "C";

            //
            //  Pezzo 1  (Filo Fisso)
            //
            //   Ho invertito lungPrimoTratto con lungUltimoTratto per evitare confusione.
            //   Ora partendo da (0,0) e andando lungo X (sia sul filo fisso che sul filo mobile)
            //   Il primo pezzo che incontro è lungPrimoTratto che dipende da O e P.
            //   Poi c'è la parte ciclica
            //   Poi c'è lungUltimoTratto che dipende da M e ALFA

            int nPeriodi = (int)ParN;
            double lungPrimoTratto = ParO;
            double lungUltimoTratto = ParM;

            if (ParO < ParD / 2) //     && nPeriodi > 0
            {
                nPeriodi--;
                lungPrimoTratto = ParE + ParO;
            }
            if (ParO > ParE + ParD / 2)
            {
                nPeriodi++;
                lungPrimoTratto = ParO - ParE;
            }
            if (ParM < ParD / 2)//   && nPeriodi > 0
            {
                nPeriodi--;
                lungUltimoTratto = ParE + ParM;
            }
            if (ParM > ParE + ParD / 2)
            {
                nPeriodi++;
                lungUltimoTratto = ParM - ParE;
            }


            /////////////////////////////////////////////////////////////////
            //            Taglio anima filo fisso.
            /////////////////////////////////////////////////////////////////

            LongCut.G1LUN15I(ParA, ParB, ParC, ParD, ParE, ParF, ParG, ParH, ParJ, ParK, lungUltimoTratto, nPeriodi, lungPrimoTratto, macroPoint, Lp);

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            /////////////////////////////////////////////////////////////////
            //            Taglio ala filo fisso.
            /////////////////////////////////////////////////////////////////
            extrusionPlane = "A";
            macroPoint.Add(new ProgramPoint(Lp - (ParA + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto), topY));
            macroPoint.Add(new ProgramPoint(Lp - (ParA + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto), 0));
            macroPoint.Add(new ProgramPoint(Lp - ParA, 0));
            macroPoint.Add(new ProgramPoint(Lp - ParA, topY));
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            //
            //  Pezzo 2  (Filo Mobile)
            //
            extrusionPlane = "C";
            nPeriodi = (int)ParN;
            lungPrimoTratto = ParO;
            lungUltimoTratto = ParM;
            
            if (ParO - ParE / 2 < ParD / 2)
            {
                nPeriodi--;
                lungPrimoTratto = ParE + ParO;
            }
            if (ParO - ParE / 2 > ParE + ParD / 2)
            {
                nPeriodi++;
                lungPrimoTratto = ParO - ParE;
            }
            if (ParM + ParE / 2 < ParD / 2)
            {
                nPeriodi--;
                lungUltimoTratto = ParE + ParM;
            }
            if (ParM + ParE / 2 > ParE + ParD / 2)
            {
                nPeriodi++;
                lungUltimoTratto = ParM - ParE;
            }

            /////////////////////////////////////////////////////////////////
            //            Taglio anima filo mobile.
            /////////////////////////////////////////////////////////////////

            LongCut.G2LUN15I(ParA - ParE / 2, ParB, ParC, ParD, ParE, ParF, ParG, -ParH, ParJ, ParK, lungUltimoTratto + ParE / 2, nPeriodi, lungPrimoTratto - ParE / 2, macroPoint, Lp, width);
            
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();
            
            /////////////////////////////////////////////////////////////////
            //            Taglio ala filo mobile.
            /////////////////////////////////////////////////////////////////
            extrusionPlane = "B";
            macroPoint.Add(new ProgramPoint(Lp - (ParA + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto), topY));
            macroPoint.Add(new ProgramPoint(Lp - (ParA + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto), 0));
            macroPoint.Add(new ProgramPoint(Lp - ParA, 0));
            macroPoint.Add(new ProgramPoint(Lp - ParA, topY));
            
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            
            return true;
        }
    }
}
