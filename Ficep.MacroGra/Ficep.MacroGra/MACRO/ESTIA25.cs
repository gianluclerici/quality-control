using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA25 : EyeMacro
    {
        public ESTIA25(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double actualAlfa = VX == "I" ? ParALFA.ToRad() : - ParALFA.ToRad();
            double tanAlfa = Math.Tan(actualAlfa), absTanAlfa = Math.Abs(tanAlfa);

            double actualBeta = VX == "I" ? ParBETA.ToRad() : -ParBETA.ToRad();
            actualBeta = Side == "B" ? -actualBeta : actualBeta;
            double tanBeta = Math.Tan(actualBeta);

            double gamma = Math.Atan(ParC / (SA - ParB));

            double absR = Math.Abs(ParR);

            double topY = SB, offsetY = SB /2;

            //
            // Estrusione Obliqua ala FM
            //
            extrusionPlane = Side == "A" ? "B" : "A";
            extrusionDepth = SA - ParB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + (Side == "A" ? offsetY * tanAlfa : -offsetY * tanAlfa), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC - (Side == "A" ? offsetY * tanAlfa : -offsetY * tanAlfa), topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, gamma);
            macroPoint.Clear();


            extrusionPlane = "C";
            extrusionDepth = SB;            
            ////
            //// Estrusione Obliqua anima FF
            ////            
            macroPoint.Add(new ProgramPoint(-offsetY * absTanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + TA * tanAlfa / 2, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + TA * tanAlfa / 2 - ParB * tanBeta, ParB, 0, ParR > 0 ? 0 : absR));
            macroPoint.Add(new ProgramPoint(-offsetY * absTanAlfa, ParB, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, actualAlfa);
            macroPoint.Clear();
            //  Rettangolo che rifinisce l'ìintersezione tra il cilindro e il taglio sull'anima inclinato
            if (ParR > 0 && actualBeta < 0 && actualAlfa > 0)
            {
                double xBottomLeft = ParA - (ParB - absR) * tanBeta - TA * tanAlfa;
                double yBottomLeft = ParB - absR;
                double length = -absR * tanBeta + TA * tanAlfa;
                double width = absR / 2;

                macroPoint.Add(new ProgramPoint(0, yBottomLeft));
                macroPoint.Add(new ProgramPoint(xBottomLeft + length, yBottomLeft));
                macroPoint.Add(new ProgramPoint(xBottomLeft + length, yBottomLeft + width));
                macroPoint.Add(new ProgramPoint(0, yBottomLeft + width));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
            }
            //  Cilidro se ParR > 0
            if (ParR > 0)
            {
                Point2D centre = new Point2D(ParA - ParB * tanBeta, ParB);
                double holeRadius = ParR;
                Brep feature = null;
                EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
                Features.Add(new EyeFeature(feature));
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}