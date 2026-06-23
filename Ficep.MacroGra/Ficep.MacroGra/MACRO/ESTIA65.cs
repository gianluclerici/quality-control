using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA65 : EyeMacro
    {
        public ESTIA65(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double radAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();
            //radAlfa = Side == "A" ? radAlfa : -radAlfa;
            double tanAlfa = Math.Tan(radAlfa);
            double absTanAlfa = Math.Abs(tanAlfa);
            double radBeta = VX == "I" ? ParBETA.ToRad() : -ParBETA.ToRad();
            double absBeta = Math.Abs(ParBETA.ToRad());

            double offsetY = SB / 2, topY = SB;
            double offsetX = offsetY * absTanAlfa - TA / 2 * tanAlfa;

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + offsetX, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + offsetX, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(0, SA - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX + ParD, SA - ParE, 0, ParS));
            macroPoint.Add(new ProgramPoint(offsetX + ParD, SA - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, SA - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ALA SIDE
            //
            extrusionDepth = TB;
            extrusionPlane = Side;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX + ParB + (offsetY + TA / 2) * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX + ParB - (offsetY - TA / 2) * tanAlfa, topY, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Cianfrino SIDE
            //
            double chamferDepth = TB;

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (radBeta > 0)
                {
                    ProgramPoint startChamfer = new ProgramPoint(offsetX + ParB + (offsetY + TA / 2) * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(offsetX + ParB - (offsetY - TA / 2) * tanAlfa, topY, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                else
                {
                    ProgramPoint startChamfer = new ProgramPoint(offsetX + ParB + (offsetY + TA / 2) * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(offsetX + ParB - (offsetY - TA / 2) * tanAlfa, topY, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB))
                        breps.Add(chamferB);
                }
            }

            //
            // Estrusione ALA OPPOSITE SIDE
            //
            extrusionDepth = SA - ParC;
            extrusionPlane = Side == "A" ? "B" : "A";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(SB * absTanAlfa, Side == "A" ? (radAlfa >= 0 ? 0 : topY) : (radAlfa >= 0 ? topY : 0), 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino OPPOSITE SIDE
            //
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (radBeta <= 0)
                {
                    ProgramPoint startChamfer = new ProgramPoint(offsetX + (offsetY + TA / 2) * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(offsetX - (offsetY - TA / 2) * tanAlfa, topY, 0);
            
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                else
                {
                    ProgramPoint startChamfer = new ProgramPoint(offsetX + (offsetY + TA / 2) * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(offsetX - (offsetY - TA / 2) * tanAlfa, topY, 0);
            
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB))
                        breps.Add(chamferB);
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

