using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG15 : EyeMacroLung
    {

        public LUNG15(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, offsetY = SB / 2, topY = SB;

            string extrusionPlane = "C";

            double tanAlfa = Math.Tan(ParALFA.ToRad()), tanP = Math.Tan(ParP.ToRad());
            double offsetFixP, offsetFixAlfa, offsetMobP, offsetMobAlfa;

            double tanBeta = Math.Tan(ParBETA.ToRad()), tanQ = Math.Tan(ParQ.ToRad());

            if (ParALFA.IsEqualTo(0, TolLinear) && ParP.IsEqualTo(0, TolLinear))
            {
                offsetFixP = 0;
                offsetFixAlfa = 0;
                offsetMobP = 0; // REG 24
                offsetMobAlfa = 0;
            }
            else
            {
                double mobM = ParM - 2 * (ParB - ParK) * tanAlfa;
                double mobO = ParO + 2 * (ParB - ParK) * tanP;

                LongCut.CalFix(ParB, ParD, ParK, ParE, ParM, ParO, ParP, ParALFA, out offsetFixP, out offsetFixAlfa);
                LongCut.CalMob(ParB, ParD, ParK, ParE, mobM, mobO, ParP, ParALFA, out offsetMobP, out offsetMobAlfa);
            }

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

            if (ParO + offsetFixP < ParD / 2) //     && nPeriodi > 0
            {
                nPeriodi--;
                lungPrimoTratto = ParE + ParO;
            }            
            if (ParO + offsetFixP > ParE + ParD / 2)
            {
                nPeriodi++;
                lungPrimoTratto = ParO - ParE;
            }
            if (ParM + offsetFixAlfa < ParD / 2)//   && nPeriodi > 0
            {
                nPeriodi--;
                lungUltimoTratto = ParE + ParM;
            }
            if (ParM + offsetFixAlfa > ParE + ParD / 2)
            {
                nPeriodi++;
                lungUltimoTratto = ParM - ParE;
            }

            double fixA = ParA - offsetFixAlfa;
            lungPrimoTratto += offsetFixP;
            lungUltimoTratto += offsetFixAlfa;

            /////////////////////////////////////////////////////////////////
            //            Taglio anima filo fisso.
            /////////////////////////////////////////////////////////////////

            LongCut.G1LUN15I(fixA, ParB, ParC, ParD, ParE, ParF, ParG, ParH, ParJ, ParK, lungUltimoTratto, nPeriodi, lungPrimoTratto, macroPoint, Lp, offsetFixP, offsetFixAlfa); 
            
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            /////////////////////////////////////////////////////////////////
            //            Taglio ala filo fisso.
            /////////////////////////////////////////////////////////////////
            extrusionPlane = "A";
            macroPoint.Add(new ProgramPoint(Lp - (fixA + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto - offsetFixP + offsetY * tanQ), topY));
            macroPoint.Add(new ProgramPoint(Lp - (fixA + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto - offsetFixP - offsetY * tanQ), 0));
            macroPoint.Add(new ProgramPoint(Lp - fixA - offsetFixAlfa + offsetY * tanBeta, 0));
            macroPoint.Add(new ProgramPoint(Lp - fixA - offsetFixAlfa - offsetY * tanBeta, topY));
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

            if (ParO + offsetMobP + 2 * (ParB - ParK) * tanP < ParD / 2)
            {
                nPeriodi --;
                lungPrimoTratto = ParE + ParO;
            }
            if (ParO + offsetMobP + 2 * (ParB - ParK) * tanP > ParE + ParD / 2)
            {
                nPeriodi ++;
                lungPrimoTratto = ParO - ParE;
            }
            if (ParM + offsetMobAlfa - 2 * (ParB - ParK) * tanAlfa < ParD / 2)
            {
                nPeriodi --;
                lungUltimoTratto = ParE + ParM;
            }
            if (ParM + offsetMobAlfa - 2 * (ParB - ParK) * tanAlfa > ParE + ParD / 2)
            {
                nPeriodi ++;
                lungUltimoTratto = ParM - ParE;
            }

            double mobA = ParA - offsetMobAlfa + 2 * (ParB - ParK) * tanAlfa;
            lungUltimoTratto += offsetMobAlfa - 2 * (ParB - ParK) * tanAlfa;
            lungPrimoTratto += offsetMobP + 2 * (ParB - ParK) * tanP;

            /////////////////////////////////////////////////////////////////
            //            Taglio anima filo mobile.
            /////////////////////////////////////////////////////////////////

            LongCut.G2LUN15I(mobA, ParB, ParC, ParD, ParE, ParF, ParG, - ParH, ParJ, ParK, lungUltimoTratto, nPeriodi, lungPrimoTratto, macroPoint, Lp, width, offsetMobP, offsetMobAlfa);

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            /////////////////////////////////////////////////////////////////
            //            Taglio ala filo mobile.
            /////////////////////////////////////////////////////////////////
            extrusionPlane = "B";
            macroPoint.Add(new ProgramPoint(Lp - (mobA + ParE / 2 + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto - offsetMobP + offsetY * tanQ), topY));
            macroPoint.Add(new ProgramPoint(Lp - (mobA + ParE / 2 + lungUltimoTratto + nPeriodi * ParE + lungPrimoTratto - offsetMobP - offsetY * tanQ), 0));
            macroPoint.Add(new ProgramPoint(Lp - (mobA + ParE / 2 + offsetMobAlfa - offsetY * tanBeta), 0));
            macroPoint.Add(new ProgramPoint(Lp - (mobA + ParE / 2 + offsetMobAlfa + offsetY * tanBeta), topY));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);

            return true;
        }



    }
}
