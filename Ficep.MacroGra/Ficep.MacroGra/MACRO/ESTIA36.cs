using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA36 : EyeMacro
    {
        public ESTIA36(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, topY = SB;

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParB, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, width - (2 * ParS + TB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParD - ParS, width - (2 * ParS + TB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParD - ParS, width - TB, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala Side
            //
            extrusionPlane = Side;
            extrusionDepth = TB;

            double actualBeta = VX == "I" ? ParBETA.ToRad() : - ParBETA.ToRad();
            actualBeta = Side == "A" ? actualBeta : - actualBeta;
            double absBeta = Math.Abs(actualBeta);
            double actualAlfa = VX == "I" ? ParALFA.ToRad() : - ParALFA.ToRad();
            double tanAlfa = Math.Tan(actualAlfa);
            double absTanAlfa = Math.Abs(tanAlfa);

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + (actualAlfa >= 0 ? 0 : SB * tanAlfa), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB - (actualAlfa >= 0 ? SB * tanAlfa : 0), topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            macroPoint.Clear();

            //
            // Cianfrino ala Side
            //
            double chamferDepth = TB;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA + ParB + (actualAlfa >= 0 ? 0 : SB * tanAlfa), 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA + ParB - (actualAlfa >= 0 ? SB * tanAlfa : 0), topY, 0, 0);

            if (actualBeta >= 0)
            {
                if (!ParBETA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            else
            {
                if (!ParBETA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }

            //
            // Estrusione ala Opposite Side
            //
            extrusionPlane = Side == "A" ? "B" : "A";
            extrusionDepth = TB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(SB * absTanAlfa, Side == "A" ? (actualAlfa >= 0 ? 0 : topY) : (actualAlfa >= 0 ? topY : 0), 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino ala Opposite Side
            //
            endChamfer = new ProgramPoint(actualAlfa >= 0 ? 0 : SB * absTanAlfa, topY, 0, 0);
            startChamfer = new ProgramPoint(actualAlfa >= 0 ? SB * absTanAlfa : 0, 0, 0, 0);
            
            if (actualBeta >= 0)
            {
                if (!ParBETA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            else
            {
                if (!ParBETA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}