using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA05 : EyeMacro
    {
        public ESTIA05(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double radALFA = ParALFA.ToRad();
            if (MirrorInizialeFinale)
                radALFA = -radALFA;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double extrusionDepth = SA - TB - Surplus - TolWebFlange, extrusionAngle = radALFA >= 0 ? -radALFA : radALFA;
            double offsetX = -SA * Math.Abs (Math.Tan (radALFA)) - Surplus;
            string extrusionPlane = radALFA >= 0 ? "B" : "A";

            //
            //  Estrusione ala con inclinazione
            //
            if (MirrorSideASideB)
            {
                macroPoint.Add(new ProgramPoint(ParC, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, SB / 2 - ParB, 0, 0)); //Secondo me è + ParB qui
                macroPoint.Add(new ProgramPoint(ParA, SB, 0, 0));
                macroPoint.Add(new ProgramPoint(0 + offsetX, SB, 0, 0));
                macroPoint.Add(new ProgramPoint(0 + offsetX, 0, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParC, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, SB / 2 + ParB, 0, 0)); //e - ParB qui
                macroPoint.Add(new ProgramPoint(ParA, SB, 0, 0));
                macroPoint.Add(new ProgramPoint(0 + offsetX, SB, 0, 0));
                macroPoint.Add(new ProgramPoint(0 + offsetX, 0, 0, 0));
            }

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, extrusionAngle, 0);

            macroPoint.Clear();

            //  Estrusione ala Side
            extrusionPlane = extrusionPlane == "A" ? "B" : "A";
            extrusionDepth = TB;

            double offsetTB = 0;
            double topY = SB;
            double tanBeta = ParC / (ParB + SB / 2); //Beta è l'angolo taglio ala inf.
            double radBeta = Math.Atan(tanBeta);
            double tanGamma = ParA / (SB / 2 - ParB); // Gamma è l'angolo taglio anima sup
            double radGamma = Math.Atan(tanGamma);

            double ParD = (ParC - offsetX) / tanBeta + ParA / tanGamma - SB;

            double interX = ParD / Math.Sin(radBeta + radGamma) * Math.Sin(radBeta) * Math.Sin(radGamma);
            double interY = SB + (interX - ParA) / tanGamma;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC - offsetX + offsetTB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(interX + offsetTB, interY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + offsetTB, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, - extrusionAngle, 0);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}