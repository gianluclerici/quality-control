using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA99 : EyeMacro
    {
        public ESTIA99(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double radAlfa = ParALFA.ToRad(), tanAlfa = Math.Tan(radAlfa), absTanAlfa = Math.Abs(tanAlfa);

            double webAngleFM = (ParG < topY / 2 - TA / 2) ? Math.Atan((ParI - ParH) / (topY - ParG)) : - Math.Atan((ParF - ParH) / ParG);
            
            //
            // Estrusione anima
            //
            double extrusionDepth = SB;
            string extrusionPlane = "C";
            double webIntersectX = (ParH + (topY / 2 + TA / 2 - ParG) * Math.Tan(webAngleFM)) - TB * tanAlfa;
            macroPoint.Add(new ProgramPoint(-offsetY * absTanAlfa, SA - ParJ, 0));
            macroPoint.Add(new ProgramPoint(webIntersectX, SA - ParJ, 0));
            macroPoint.Add(new ProgramPoint(webIntersectX, width - TB - Surplus / 2, 0));
            macroPoint.Add(new ProgramPoint(-offsetY * absTanAlfa, width - TB - Surplus / 2, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, webAngleFM);
            macroPoint.Clear();
            //
            // Estrusione ala Side
            //
            extrusionDepth = width - ParJ;
            extrusionPlane = Side;
            macroPoint.Add(new ProgramPoint(-width * absTanAlfa, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParD, topY - ParE, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, topY, 0));
            macroPoint.Add(new ProgramPoint(-width * absTanAlfa, topY, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -radAlfa);
            macroPoint.Clear();
            //
            // Estrusione ala Opposite Side
            //
            extrusionDepth = TB;
            extrusionPlane = Side == "A" ? "B" : "A";
            macroPoint.Add(new ProgramPoint(0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParH, ParG, 0));
            macroPoint.Add(new ProgramPoint(ParI, topY, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);
            macroPoint.Clear();
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}