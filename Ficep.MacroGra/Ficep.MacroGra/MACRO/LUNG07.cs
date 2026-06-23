using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class LUNG07 : EyeMacroLung
    {

        public LUNG07(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            List<Brep> breps = new List<Brep>();

            double width = SA, halfWidth = SA / 2, topY = SB, offsetY = SB / 2;

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double tanH = Math.Tan(ParH.ToRad()), tanG = Math.Tan(ParG.ToRad()), tanI = Math.Tan(ParI.ToRad()), tanL = Math.Tan(ParL.ToRad());

            bool direction = true;
            //
            //  Taglio anima
            //

            //  webCutStartX = Offset X inizio taglio anima
            double offsetGX = (ParG <= 0) ? - ((halfWidth + ParB / 2) * tanG) : ((halfWidth - ParB / 2) * tanG);
            double offsetHX = Math.Abs(tanH) * (offsetY + (ParH > 0 ? -TA / 2 : TA / 2));            
            double webCutStartX = offsetGX + offsetHX;

            //  webCutEndX = Offset X fine taglio anima
            double offsetIX = (ParI >= 0) ? tanI * (halfWidth - ParB / 2) : - tanI * (halfWidth + ParB / 2);
            double offsetLX = Math.Abs(tanL) * (offsetY + (ParL < 0 ? -TA / 2 : TA / 2));            
            double webCutEndX  = offsetIX + offsetLX;

            //
            //  Tratto iniziale
            //
            macroPoint.Add(new ProgramPoint(webCutStartX, halfWidth + ParB / 2));
            macroPoint.Add(new ProgramPoint(ParF, halfWidth + ParB / 2));

            //
            //  Taglio ciclico lungo l'anima
            //
            double periodo = 2 * (ParA + ParC);
            int nPeriodi = (int)Math.Floor((Lp - webCutEndX - ParF) / periodo);

            macroPoint = LongCut.TrapezoidalCycleCut(direction, true, nPeriodi, ParA, ParB, ParC, macroPoint);

            double spazioRimanente = Lp - nPeriodi * periodo - ParF;
            //
            //  Ultimo tratto.
            //
            macroPoint = LongCut.TrapezoidalLastCut(direction, true, spazioRimanente, webCutEndX, ParA, ParB, ParC, macroPoint);

            if (direction) macroPoint[macroPoint.Count - 1].X = Lp - macroPoint[macroPoint.Count - 1].X; //Correzione perchè non riesco ad accedere a Lp da dentro la funzione

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            curves.Clear();
            macroPoint.Clear();


            //
            //  Estrusione iniziale
            //
            double wingStartXFF = (ParG <= 0) ? 0 : width * Math.Abs(tanG);
            wingStartXFF += Math.Abs(tanH) * offsetY;

            double wingStartXFM = (ParG >= 0) ? 0 : width * Math.Abs(tanG);
            wingStartXFM += Math.Abs(tanH) * offsetY;

            macroPoint.Add(new ProgramPoint(0 - topY * Math.Abs(tanH), 0));
            macroPoint.Add(new ProgramPoint(wingStartXFF - tanH * TA / 2, 0));
            macroPoint.Add(new ProgramPoint(wingStartXFM - tanH * TA / 2, width));
            macroPoint.Add(new ProgramPoint(0 - topY * Math.Abs(tanH), width));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, - ParH.ToRad());

            macroPoint.Clear();

            //
            //  Estrusione finale
            //
            wingStartXFF = (ParI >= 0) ? Lp : Lp - width * Math.Abs(tanI);
            wingStartXFF -= Math.Abs(tanL) * offsetY;

            wingStartXFM = (ParI <= 0) ? Lp : Lp - width * Math.Abs(tanI);
            wingStartXFM -= Math.Abs(tanL) * offsetY;

            macroPoint.Add(new ProgramPoint(Lp + topY * Math.Abs(tanL), 0));
            macroPoint.Add(new ProgramPoint(wingStartXFF - tanL * TA / 2, 0));
            macroPoint.Add(new ProgramPoint(wingStartXFM - tanL * TA / 2, width));
            macroPoint.Add(new ProgramPoint(Lp + topY * Math.Abs(tanL), width));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -ParL.ToRad());

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
            return true;
        }

    }
}
