using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA98 : EyeMacro
    {
        public ESTIA98(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA - ParG;
            string extrusionPlane = Side;

            double topY = SB, offsetY = SB / 2, width = SA;

            double radAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad(), tanAlfa = Math.Tan(radAlfa), absTanAlfa = Math.Abs(tanAlfa);

            double tanBeta = (ParF - ParE) / SB, tanGamma = (ParD > 0 ? ((ParB - ParC) / (topY - ParD)) : ((ParB - ParA ) / topY));
            double webIntersectX = ParD > 0 ? (ParC + (offsetY + TA / 2 - ParD) * tanGamma - (SA - ParG) * tanAlfa) : (ParA + (offsetY + TA / 2 - ParD) * tanGamma - (SA - ParG) * tanAlfa);

            //
            // Estrusione ala Side
            //
            macroPoint.Add(new ProgramPoint(-(SA - ParG) * absTanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(-(SA - ParG) * absTanAlfa, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);
            macroPoint.Clear();

            //
            // Estrusione ala Opposite Side
            //
            extrusionDepth = TB;
            extrusionPlane = Side == "A" ? "B" : "A";

            macroPoint.Add(new ProgramPoint(-TB * absTanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(-TB * absTanAlfa, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -radAlfa);
            macroPoint.Clear();

            //
            // Estrusione anima
            //
            extrusionDepth = SB;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, width - ParG - Surplus, 0, 0));
            macroPoint.Add(new ProgramPoint(webIntersectX, width - ParG + Surplus, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + offsetY * tanBeta + TB * absTanAlfa, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}