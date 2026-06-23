using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA42 : EyeMacro
    {

        public ESTIA42(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IU";
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
            List<Brep> breps = new List<Brep>();

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double width = SA, offsetY = SB / 2, topY = SB;

            double tanAlfa = ParB / ParC; // Alfa è l'inclinazione di partenza dal Filo Fisso del taglio sull'anima
            double radAlfa = Math.Atan(tanAlfa);

            double ParDY = ParC - (ParA - ParB - ParD) * tanAlfa;
            double ParFY = ParDY + (ParD - ParF) / tanAlfa;

            double offsetFromParEX = (SA - ParFY - (ParE - ParF) * tanAlfa) * Math.Sin(radAlfa) * Math.Cos(radAlfa);// I nomi non sono bellissimi 
            double offsetFromParEY = ParFY + (ParE - ParF) * tanAlfa + offsetFromParEX * tanAlfa; // valore Y dell'angolo arrotondato vicino Filo Mobile

            double distanceFromWeb = TA / 2 + (ParI < InnerChamferDisFromWeb ? InnerChamferDisFromWeb : ParI);
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParB, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD, ParDY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, ParFY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + offsetFromParEX, offsetFromParEY, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            if (CodePrf == "I")
            {
                macroPoint.Clear();

                //
                // Estrusione ala Side bassa
                //
                extrusionDepth = TB;
                extrusionPlane = Side;

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParH, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - distanceFromWeb, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY - distanceFromWeb, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);
                macroPoint.Clear();

                //
                // Estrusione ala Side alta
                //
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParG, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + distanceFromWeb, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY + distanceFromWeb, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);
                macroPoint.Clear();

                //
                // Estrusione ala Opposite Side bassa
                //
                extrusionPlane = Side == "A" ? "B" : "A";

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE + ParH, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE, offsetY - distanceFromWeb, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY - distanceFromWeb, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -radAlfa);
                macroPoint.Clear();

                //
                // Estrusione ala Opposite Side alta
                //
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE + ParG, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE, offsetY + distanceFromWeb, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY + distanceFromWeb, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -radAlfa);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}