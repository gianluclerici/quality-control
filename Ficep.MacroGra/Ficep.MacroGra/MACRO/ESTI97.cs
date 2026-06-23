using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI97 : EyeMacro
    {
        public ESTI97(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;

            double radAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad(), absAlfa = Math.Abs(radAlfa), tanAlfa = Math.Tan(radAlfa), absTanAlfa = Math.Tan(absAlfa);
            double radBeta = VX == "I" ? ParBETA.ToRad() : -ParBETA.ToRad(), tanBeta = Math.Tan(radBeta), absTanBeta = Math.Abs(tanBeta);

            string extrusionPlane = radAlfa >= 0 ? "A" : "B";

            double topY = SB, offsetY = SB / 2, width = SA;

            double offsetX = offsetY * absTanBeta;
            //
            // Estrusione obliqua 
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(extrusionDepth * absTanAlfa + (radBeta > 0 ? 2 * offsetX : 0), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(extrusionDepth * absTanAlfa + (radBeta < 0 ? 2 * offsetX : 0), topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, absAlfa);

            macroPoint.Clear();

            //
            // Estrusione anima 
            //
            extrusionPlane = "C";
            extrusionDepth = SB;

            offsetX = radAlfa >= 0 ? offsetX + width * absTanAlfa : offsetX;
            offsetX -= TA / 2 * tanBeta;

            macroPoint.Add(new ProgramPoint(-TA * absTanBeta + offsetX - ParA * tanAlfa, ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX - ParA * tanAlfa + ParB, ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX - (ParA + ParC) * tanAlfa + ParB, ParD + ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(-TA * absTanBeta + offsetX - (ParA + ParC) * tanAlfa, ParA + ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, - radBeta);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}