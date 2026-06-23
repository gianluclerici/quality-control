using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA88 : EyeMacro
    {
        public ESTIA88(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            List<Brep> breps = new List<Brep>();

            double topY = SB, offsetY = SB / 2, width = SA;

            double absR = Math.Abs(ParR);

            double radAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();
            radAlfa = Side == "A" ? radAlfa : -radAlfa;

            double absTanAlfa = Math.Abs(Math.Tan(radAlfa));

            double radBeta = ParBETA.ToRad();

            double webOffsetFM = ParF.IsEqualTo(0, TolLinear) ? TA * absTanAlfa / 2 : 0;

            // webOffsetFF e' la quota X taglio anima filo fisso.
            // webAngle e' l'angolo incl. torcia nel taglio anima filo fisso.
            double webOffsetFF = 0, webAngle = 0;
            if (ParD > offsetY - TA / 2)
            {
                webOffsetFF = ParE * (ParD - offsetY - TA / 2) / ParD;
                webAngle = ParF.IsEqualTo(0, TolLinear) ? Math.Atan(ParE / ParD) : 0;
            }
            else
            {
                webOffsetFF = ParC * (offsetY + TA / 2 - ParD) / (topY - ParD);
                webAngle = ParF.IsEqualTo(0, TolLinear) ? - Math.Atan(ParC / (topY - ParD)) : 0;
            }
            //
            // Estrusione ala Side
            //
            //double extrusionDepth = ParF.IsEqualTo(0, TolAngle) ? SA - ParB : TB;
            double extrusionDepth = TB;
            string extrusionPlane = Side;
            macroPoint.Add(new ProgramPoint(0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            macroPoint.Add(new ProgramPoint(ParC, topY, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0));
            macroPoint.Add(new ProgramPoint(0, ParD, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione anima
            //
            extrusionDepth = SB;
            extrusionPlane = "C";
            //  Lato Opposite Side
            macroPoint.Add(new ProgramPoint(-offsetY * absTanAlfa, width - TB, 0, 0));
            if (radAlfa < 0)
            {
                macroPoint.Add(new ProgramPoint(ParA + (Side == "A" ? webOffsetFM : -webOffsetFM), width - TB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + (Side == "A" ? webOffsetFM : -webOffsetFM), width - ParB + absR, 0, 0));
                if ( ParR > 0 )
                {
                    macroPoint.Add(new ProgramPoint(ParA + (Side == "A" ? webOffsetFM : -webOffsetFM) + absR, width - ParB, 0, 0, -ParR));
                }

                macroPoint.Add(new ProgramPoint(ParA + (Side == "A" ? webOffsetFM : -webOffsetFM) - absR, width - ParB, 0, 0, -absR));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParA - (Side == "A" ? webOffsetFM : -webOffsetFM), width - TB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - (Side == "A" ? webOffsetFM : -webOffsetFM), width - ParB + absR, 0, 0));
                if (ParR > 0)
                {
                    macroPoint.Add(new ProgramPoint(ParA - (Side == "A" ? webOffsetFM : -webOffsetFM) + absR, width - ParB, 0, 0, -ParR));
                }

                macroPoint.Add(new ProgramPoint(ParA - (Side == "A" ? webOffsetFM : -webOffsetFM) - absR, width - ParB, 0, 0, -absR));
            }
            macroPoint.Add(new ProgramPoint(-offsetY * absTanAlfa, width - ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, ParF.IsEqualTo(0, TolLinear) ? (Side == "A" ? -radAlfa : radAlfa) : 0);
            macroPoint.Clear();

            //  Lato Side
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            if ( ParS > 0)
            {
                macroPoint.Add(new ProgramPoint(webOffsetFF + ParS, TB, 0, 0));
                macroPoint.Add(new ProgramPoint(webOffsetFF, TB + ParS, 0, 0, ParS));
                macroPoint.Add(new ProgramPoint(webOffsetFF, width - ParB - ParS, 0, 0));
                macroPoint.Add(new ProgramPoint(webOffsetFF + ParS, width - ParB, 0, 0, ParS));
            }
            else
            {

                macroPoint.Add(new ProgramPoint(webOffsetFF, TB, 0, 0));
                macroPoint.Add(new ProgramPoint(webOffsetFF, width - ParB, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0, width - ParB, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, ParF.IsEqualTo(0, TolLinear) ? -webAngle : 0);
            macroPoint.Clear();
            //
            // Estrusione ala Opposite Side
            //
            extrusionDepth = TB;
            extrusionPlane = Side == "A" ? "B" : "A";
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            if (radAlfa < 0)
            {
                macroPoint.Add(new ProgramPoint(ParA - topY * absTanAlfa / 2 + TB * Math.Tan(radBeta), 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + topY * absTanAlfa / 2 + TB * Math.Tan(radBeta), topY, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParA + topY * absTanAlfa / 2 + TB * Math.Tan(radBeta), 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - topY * absTanAlfa / 2 + TB * Math.Tan(radBeta), topY, 0));
            }
            macroPoint.Add(new ProgramPoint(0, topY, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radBeta);
            macroPoint.Clear();

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}