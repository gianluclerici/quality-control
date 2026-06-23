using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA96 : EyeMacro
    {
        public ESTIA96(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "Q";
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

            double width = SA, topY = SB;

            double extrusionDepth = width / 2;
            string extrusionPlane = "B";

            double radAlfa = ParALFA.ToRad(), tanAlfa = Math.Tan(radAlfa);
            double angleD = Math.Atan((width * tanAlfa + ParB - ParD) / width);

            //
            // Estrusione piano Opposite Side
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -angleD);
            macroPoint.Clear();

            //
            // Estrusione piano Side
            //
            extrusionPlane = Side;

            macroPoint.Add(new ProgramPoint(0, topY - ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(width * tanAlfa, topY - ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(width * tanAlfa + ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, angleD);
            macroPoint.Clear();

            //
            // Estrusione piano C
            //
            extrusionDepth = 2 * topY; //   perchè il solido viene estruso sopra e sotto invede di solo in una direzione
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(- width * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(width * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            macroPoint.Add(new ProgramPoint(-width * tanAlfa, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}