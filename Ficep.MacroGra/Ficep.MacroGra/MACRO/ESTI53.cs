using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI53 : EyeMacro
    {
        public ESTI53(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IQ";
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

            //La validate dovra fallire se (A e/o B) > 0 e BETA != 0 
            if (Validate() != ErrMacro.No_err)
                return false;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double chamferDepth, chamferAngle;

            double extrusionDepth = SA;

            double topY = SB, offsetY = SB / 2, width = SA;

            double radAlfa = VX == "I" ? ParALFA.ToRad() : - ParALFA.ToRad(), absAlfa = Math.Abs(radAlfa), absTanAlfa = Math.Tan(absAlfa);

            double absTanBeta = Math.Abs(Math.Tan(ParBETA.ToRad()));

            string extrusionPlane = radAlfa >= 0 ? "A" : "B";

            ProgramPoint startChamfer, endChamfer;

            double actualA = CodePrf == "I" && ParA > 0 && ParB > 0 ? ParA : 0,
                   actualB = CodePrf == "I" && ParA > 0 && ParB > 0 ? ParB : 0;
            //
            // Estrusione con doppia Inclinazione Alfa Beta
            //
            double initBetaEffect = (VX == "I" && ParBETA > 0) || (VX != "I" && ParBETA < 0) ? absTanBeta : 0;
            double endBetaEffect = (VX == "F" && ParBETA > 0) || (VX != "F" && ParBETA < 0) ? absTanBeta : 0;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(width * absTanAlfa + topY * initBetaEffect + actualA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(width * absTanAlfa + (topY - actualB) * initBetaEffect, actualB, 0, 0));
            macroPoint.Add(new ProgramPoint(width * absTanAlfa + (topY - actualB) * endBetaEffect, topY - actualB, 0, 0));
            macroPoint.Add(new ProgramPoint(width * absTanAlfa + topY * endBetaEffect + actualA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, absAlfa);
            macroPoint.Clear();

            //
            //  Cianfrini
            //

            //  Piano A
            chamferDepth = CodePrf == "I" ? TB - ParD : TB - ParB;
            chamferAngle = CodePrf == "I" ? ParC : ParA;

            if (!chamferAngle.IsEqualTo(0, TolAngle) && chamferAngle >= ParALFA)
            {
                chamferAngle = chamferAngle.ToRad();

                startChamfer = new ProgramPoint((width - chamferDepth) * absTanAlfa + topY * initBetaEffect + actualA, 0, 0, 0);
                if (CodePrf == "I" && ParA > 0 && ParB > 0)
                {
                    endChamfer = new ProgramPoint((width - chamferDepth) * absTanAlfa + (topY - actualB) * initBetaEffect, actualB, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(endChamfer, startChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB, false, false))
                        breps.Add(chamferB);

                    startChamfer = new ProgramPoint((width - chamferDepth) * absTanAlfa + (topY - actualB) * endBetaEffect, topY - actualB, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferB, false, false))
                        breps.Add(chamferB);
                }

                endChamfer = new ProgramPoint((width - chamferDepth) * absTanAlfa + topY * endBetaEffect + actualA, topY, 0, 0);

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }

            //  Piano B
            chamferDepth = TB - ParD;
            chamferAngle = ParC;
            extrusionPlane = extrusionPlane == "A" ? "B" : "A";

            if (!chamferAngle.IsEqualTo(0, TolAngle) && chamferAngle >= -ParALFA)
            {
                chamferAngle = chamferAngle.ToRad();

                startChamfer = new ProgramPoint(chamferDepth * absTanAlfa + topY * initBetaEffect + actualA, 0, 0, 0);
                if (CodePrf == "I" && ParA > 0 && ParB > 0)
                {
                    endChamfer = new ProgramPoint(chamferDepth * absTanAlfa + (topY - actualB) * initBetaEffect, actualB, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(endChamfer, startChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, 2 * Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB, false, false))
                        breps.Add(chamferB);

                    startChamfer = new ProgramPoint(chamferDepth * absTanAlfa + (topY - actualB) * endBetaEffect, topY - actualB, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, 2 * Surplus, TolBrep, TolLinear, TolWebFlange, out chamferB, false, false))
                        breps.Add(chamferB);
                }

                endChamfer = new ProgramPoint(chamferDepth * absTanAlfa + topY * endBetaEffect + actualA, topY, 0, 0);

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }

            //  Piano C e D solo per Q
            if (CodePrf == "Q")
            {
                //  Cianfrino C
                chamferDepth = TB - ParG;
                chamferAngle = ParF;
                extrusionPlane = "C";

                double chamferBetaEffect = absTanBeta * ((VX == "F" && ParBETA > 0) || (VX != "F" && ParBETA < 0) ? (topY - chamferDepth): chamferDepth);

                if (!chamferAngle.IsEqualTo(0, TolAngle) && chamferAngle >= - ParBETA)
                {
                    chamferAngle = chamferAngle.ToRad();

                    startChamfer = new ProgramPoint((radAlfa >= 0 ? 0 : width * absTanAlfa) + chamferBetaEffect, width, 0, 0);
                    endChamfer = new ProgramPoint((radAlfa >= 0 ? width * absTanAlfa : 0) + chamferBetaEffect, 0, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                ////  Cianfrino D momentaneamente commentato ed anche sicuramente sbagliato
                //chamferDepth = TB - ParI;
                //chamferAngle = ParH;
                //extrusionPlane = "D";
                //if (!chamferAngle.IsEqualTo(0, TolAngle))
                //{
                //    chamferAngle = chamferAngle.ToRad();
                //
                //    startChamfer = new ProgramPoint(chamferDepth * absTanAlfa + (VX == "I" ? (ParBETA > 0 ? topY * absTanBeta : 0) : (ParBETA < 0 ? topY * absTanBeta : 0)), topY, 0, 0);
                //    endChamfer = new ProgramPoint((topY - chamferDepth) * absTanAlfa + (VX == "I" ? (ParBETA > 0 ? topY * absTanBeta : 0) : (ParBETA < 0 ? topY * absTanBeta : 0)) + actualA, topY, 0, 0);
                //
                //    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                //        breps.Add(chamferA);
                //}
            }
            //
            //  Estrusioni circolari anima
            //
            if (CodePrf == "I")
            {
                extrusionPlane = "C";
                extrusionDepth = SB;
                macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
                macroPoint.Add(new ProgramPoint((radAlfa > 0 ? (width - TB) * absTanAlfa : 0) + offsetY * absTanBeta + ParR, TB, 0, 0));
                macroPoint.Add(new ProgramPoint((radAlfa > 0 ? (width - TB) * absTanAlfa : 0) + offsetY * absTanBeta - ParR * Math.Sin(radAlfa), TB + ParR * Math.Cos(radAlfa), 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(0, TB + ParR * Math.Cos(radAlfa), 0, 0));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
                macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
                macroPoint.Add(new ProgramPoint((radAlfa < 0 ? (width - TB) * absTanAlfa : 0) + offsetY * absTanBeta + ParR, width - TB, 0, 0));
                macroPoint.Add(new ProgramPoint((radAlfa < 0 ? (width - TB) * absTanAlfa : 0) + offsetY * absTanBeta + ParR * Math.Sin(radAlfa), width - (TB + ParR * Math.Cos(radAlfa)), 0, 0, -ParR));
                macroPoint.Add(new ProgramPoint(0, width - (TB + ParR * Math.Cos(radAlfa)), 0, 0));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}