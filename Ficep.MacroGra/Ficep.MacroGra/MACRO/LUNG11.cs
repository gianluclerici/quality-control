using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG11 : EyeMacroLung
    {

        public LUNG11(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double halfWidth = SA / 2;

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            bool direction = true;

            double periodo = 2 * (ParA + ParC);
            int nPeriodi = (int)Math.Floor((Lp - ParF - ParG - (ParH == 1 ? ParC : 0)) / periodo);
            double spazioRimanente = Lp - nPeriodi * periodo - ParF; 

            //
            //  Tratto iniziale
            //
            macroPoint.Add(new ProgramPoint(0, halfWidth + ParB / 2));
            macroPoint.Add(new ProgramPoint(ParF, halfWidth + ParB / 2));

            //
            //  Taglio ciclico lungo l'anima
            //
            
            macroPoint = LongCut.TrapezoidalCycleCut(direction, true, nPeriodi, ParA, ParB, ParC, macroPoint);
            
            //
            //  Ultimo tratto.
            //

            if (ParH == 1)
            {
                macroPoint.Add(new ProgramPoint(Lp - spazioRimanente + ParC, halfWidth - ParB / 2));
                macroPoint.Add(new ProgramPoint(Lp, halfWidth - ParB / 2));
            }
            else if (ParH == 0)
            {     
                if (spazioRimanente - ParG > ParA + 2 * ParC) 
                {
                    macroPoint = LongCut.TrapezoidalLastCut(direction, true, spazioRimanente, ParG, ParA, ParB, ParC, macroPoint);
                    if (direction) macroPoint[macroPoint.Count - 1].X = Lp - macroPoint[macroPoint.Count - 1].X; //Correzione perchè non riesco ad accedere a Lp da dentro la funzione
                }
                macroPoint.Add(new ProgramPoint(Lp, halfWidth + ParB / 2));
            }            
            
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            return true;
        }

    }
}
