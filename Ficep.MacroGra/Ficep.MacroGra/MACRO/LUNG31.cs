using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG31 : EyeMacroLung
    {
        public LUNG31(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //  Filo Fisso
            double periodo = 2 * ParR + ParD;
            double trattoInizialeFF = periodo - ParA;
            int nPeriodi = (int)((Lp - trattoInizialeFF) / periodo);
            double trattoFinaleFF = Lp - trattoInizialeFF - periodo * nPeriodi;
            //  Tratto iniziale
            if (ParA <= ParD)
            {
                macroPoint.Add(new ProgramPoint(0, ParB));
                macroPoint.Add(new ProgramPoint(ParD - ParA, ParB));
            }
            else
                macroPoint.Add(new ProgramPoint(0, ParB - Math.Sqrt(ParR * ParR - (ParA - ParD - ParR) * (ParA - ParD - ParR))));
            
            macroPoint.Add(new ProgramPoint(ParD - ParA + 2 * ParR, ParB, 0, 0, ParR));
            nPeriodi--;
            //  Tratto ciclico
            macroPoint = LongCut.SemiCircleCycleCut(true, false, nPeriodi, ParD, ParR, ParD, 0, macroPoint);
            
            //  Tratto finale
            if(trattoFinaleFF <= ParD)
                macroPoint.Add(new ProgramPoint(Lp, ParB));
            else
            {
                macroPoint.Add(new ProgramPoint(Lp - trattoFinaleFF + ParD, ParB));
                macroPoint.Add(new ProgramPoint(Lp, ParB - Math.Sqrt(ParR * ParR - (trattoFinaleFF - ParD - ParR) * (trattoFinaleFF - ParD - ParR)), 0, 0, ParR));
            }
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            // Filo Mobile
            if (ParB >= SA / 2) //   - 3 * FAT_CONV
            {
                double trattoFinaleFM = trattoFinaleFF - ParD / 2 + ParR;
                nPeriodi = (int)((Lp - trattoFinaleFM) / periodo);
                double trattoInizialeFM = Lp - trattoFinaleFM - nPeriodi * periodo;
                nPeriodi--;
                //  Tratto Finale
                if (trattoFinaleFM >= 2 * ParR)
                {
                    if (trattoFinaleFM >= periodo)
                    {
                        macroPoint.Add(new ProgramPoint(Lp, SA - ParB + Math.Sqrt(ParR * ParR - Math.Pow(trattoFinaleFM - (3 * ParR + ParD), 2)), 0));
                        macroPoint.Add(new ProgramPoint(Lp - trattoFinaleFM + 2 * ParR + ParD, SA - ParB, 0, 0, ParR));
                    }
                    else
                        macroPoint.Add(new ProgramPoint(Lp, SA - ParB, 0));
                    macroPoint.Add(new ProgramPoint(Lp - trattoFinaleFM + 2 * ParR, SA - ParB, 0));
                    macroPoint.Add(new ProgramPoint(Lp - trattoFinaleFM + ParR, SA - ParB + ParR, 0, 0, ParR));
                }
                else
                    macroPoint.Add(new ProgramPoint(Lp, SA - ParB + Math.Sqrt(ParR * ParR - Math.Pow(trattoFinaleFM - ParR, 2)), 0));
                macroPoint.Add(new ProgramPoint(Lp - trattoFinaleFM, SA - ParB, 0, 0, ParR));

                //  Tratto Ciclico
                macroPoint = LongCut.SemiCircleCycleCut(false, false, nPeriodi, ParD, ParR, ParD, 0, macroPoint);

                //  Tratto Iniziale
                if (trattoInizialeFM < ParD)
                    macroPoint.Add(new ProgramPoint(0, SA - ParB, 0));
                else
                {
                    macroPoint.Add(new ProgramPoint(trattoInizialeFM - ParD, SA - ParB, 0));
                    macroPoint.Add(new ProgramPoint(0, SA - (ParB - Math.Sqrt(ParR * ParR - Math.Pow(trattoInizialeFM - ParD - ParR, 2))), 0, 0, ParR));
                }
            }
            else
            {
                //  Tratto Finale
                if (trattoFinaleFF < ParD)
                    macroPoint.Add(new ProgramPoint(Lp, SA - ParB, 0));
                else
                {
                    macroPoint.Add(new ProgramPoint(Lp, SA - (ParB - Math.Sqrt(ParR * ParR - Math.Pow(trattoFinaleFF - ParD - ParR, 2))), 0));
                    macroPoint.Add(new ProgramPoint(Lp - trattoFinaleFF + ParD, SA - ParB, 0, 0, ParR));
                }

                //  Tratto Ciclico
                macroPoint = LongCut.SemiCircleCycleCut(false, false, nPeriodi, ParD, ParR, ParD, 0, macroPoint);

                //  Tratto Iniziale
                macroPoint.Add(new ProgramPoint(ParD - ParA + 2 * ParR, SA - ParB, 0));
                if (ParA <= ParD)
                {
                    macroPoint.Add(new ProgramPoint(ParD - ParA + ParR, SA - (ParB - ParR), 0, 0, ParR));
                    macroPoint.Add(new ProgramPoint(ParD - ParA, SA - ParB, 0, 0, ParR));
                    macroPoint.Add(new ProgramPoint(0, SA - ParB, 0));
                }
                else
                    macroPoint.Add(new ProgramPoint(0, SA - (ParB - Math.Sqrt(ParR * ParR - Math.Pow(ParA - ParD - ParR, 2))), 0, 0, ParR));
            }
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            return true;
        }
    }
}