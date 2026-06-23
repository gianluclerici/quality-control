using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI18 : EyeMacro
    {
        public ESTI18(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth;
            string extrusionPlane;

            //
            // Estrusione anima
            //
            extrusionPlane = "C";
            extrusionDepth = SB;

            double width = SA;
            double radAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();
            double tanAlfa = Math.Tan(radAlfa);
            double tanGamma = (ParE - ParB) / (width - ParC - ParF);

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - TB * tanAlfa, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC * tanAlfa, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParG, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParH * tanGamma, ParC + ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE - ParL * tanGamma, width - ParF - ParL, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParI, width - ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + ParF * tanAlfa, width - ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + TB * tanAlfa, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala A
            //
            extrusionPlane = "A";
            extrusionDepth = TB;

            double Beta = VX == "I" ? ParBETA : -ParBETA;
            double tanBeta = Math.Tan(Beta.ToRad());
            double offsetY = SB / 2, topY = SB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + offsetY * tanBeta, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - offsetY * tanBeta, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);
            macroPoint.Clear();

            //
            // Estrusione ala B
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + offsetY * tanBeta, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD - offsetY * tanBeta, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, - radAlfa);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}