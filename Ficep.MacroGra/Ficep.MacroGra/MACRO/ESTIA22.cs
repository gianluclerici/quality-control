using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA22 : EyeMacro
    {
        public ESTIA22(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double actualAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();
            double tanAlfa = Math.Tan(actualAlfa);
            double absR = Math.Abs(ParR);
            double offsetY = SB / 2, topY = SB;

            //
            // Estrusione ala FM
            //
            extrusionPlane = Side == "A" ? "B" : "A";
            extrusionDepth = SA - ParB - absR;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (Side == "A" ? offsetY * tanAlfa : -offsetY * tanAlfa), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (Side == "A" ? offsetY * tanAlfa : -offsetY * tanAlfa), topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala FF
            //
            extrusionPlane = Side == "A" ? "A" : "B";
            extrusionDepth = ParB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(topY * Math.Abs(tanAlfa), actualAlfa > 0 ? 0 : topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione anima
            //
            extrusionPlane = "C";
            extrusionDepth = SB;

            macroPoint.Add(new ProgramPoint(0, ParB - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(Math.Abs((offsetY - TA / 2) * tanAlfa), ParB - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(Math.Abs(offsetY * tanAlfa) + ParC, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - absR, ParB, 0, 0));
            if (ParR > 0) macroPoint.Add(new ProgramPoint(ParA, ParB - absR, 0, 0, absR));
            macroPoint.Add(new ProgramPoint(ParA + (ParR > 0 ? 0 : - TA / 2 * tanAlfa), ParB + absR, 0, 0, absR));
            macroPoint.Add(new ProgramPoint(0, ParB + absR, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}