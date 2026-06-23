using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA79 : EyeMacro
    {
        public ESTIA79(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double offsetY = SB / 2, topY = SB, width = SA;
            double radBeta = ParBETA.ToRad();
            double tanBeta = Math.Tan(radBeta);
            double radAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();
            double absAlfa = Math.Abs(radAlfa);
            double absTanAlfa = Math.Tan(absAlfa);
            double offsetX = width * absTanAlfa + (VX == "I" ? (ParC - ParA) : (ParA - ParC)) * tanBeta;
            double xWingFF, xWingFM, xWebFF, xWebFM;

            if (radAlfa > 0)
            {
                xWingFF = width * absTanAlfa + (ParC - ParA) * tanBeta;
                xWingFM = 0;
                xWebFF = Side == "A" ? (width - TB) * absTanAlfa + (ParC - TA / 2) * tanBeta : (ParC + TA / 2) * tanBeta + TB * absTanAlfa;
                xWebFM = Side == "A" ? (ParC - TA / 2) * tanBeta + TB * absTanAlfa : (width - TB) * absTanAlfa + (ParC + TA / 2) * tanBeta;
            }
            else
            {
                xWingFF = 0;
                xWingFM = width * absTanAlfa + (ParA - ParC) * tanBeta;
                xWebFF = Side == "A" ? (ParA - TA / 2) * tanBeta + TB * absTanAlfa : (width - TB) * absTanAlfa + (ParA + TA / 2) * tanBeta;
                xWebFM = Side == "A" ? (width - TB) * absTanAlfa + (ParA - TA / 2) * tanBeta : (ParA + TA / 2) * tanBeta + TB * absTanAlfa;
            }

            //
            // Estrusione ALA SIDE
            //
            double extrusionDepth = TB;
            string extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(- offsetX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(xWingFF + (ParA + offsetY) * tanBeta, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(xWingFF, offsetY + ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(xWingFF + ParB, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(- offsetX, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);

            macroPoint.Clear();

            //
            // Estrusione ALA OPPOSITE SIDE
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(-offsetX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(xWingFM + (ParC + offsetY) * tanBeta, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(xWingFM, offsetY + ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(xWingFM + ParD, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(-offsetX, topY, 0, 0));

            // MIRROR ALTO BASSO USATO AL POSTO DI MIRROR SIDE A SIDE B
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorAltoBasso, MirrorSideASideB, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, - radAlfa);

            macroPoint.Clear();

            //
            // Estrusione Anima
            //
            extrusionDepth = SB;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(-offsetX, TB - TolWebFlange, 0, 0));
            macroPoint.Add(new ProgramPoint(xWebFF, TB - TolWebFlange, 0, 0));
            macroPoint.Add(new ProgramPoint(xWebFM, width - (TB - TolWebFlange), 0, 0));
            macroPoint.Add(new ProgramPoint(-offsetX, width - (TB - TolWebFlange), 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, Side == "A" ? - radBeta : radBeta);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}