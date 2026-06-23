using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI106 : EyeMacro
    {
        public ESTI106(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fiswidth
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

            double width = SA, topY = SB, offsetY = SB / 2;

            double safeR = ParR == 15 ? ParR + TolWebFlange : ParR;

            double radBCG = 0, radACG = 0, radEFH = 0, radDFH = 0, xFF = 0, xFM = 0, radAlfa = 0;
            //  Angolo ala FF basso
            if (ParC > 0)
            {
                radBCG = Math.Atan((ParB - ParG) / ParC);
            }
            //  Angolo ala FF alto
            if (ParC < topY)
            {
                radACG = Math.Atan((ParA - ParG) / (topY - ParC));
            }
            //  Angolo ala FM basso
            if (ParF > 0)
            {
                radEFH = Math.Atan((ParE - ParH) / ParF);
            }
            //  Angolo ala FM alto
            if (ParF < topY)
            {
                radDFH = Math.Atan((ParD - ParH) / (topY - ParF));
            }
            //  Coordinata X FF
            if (ParC > offsetY + TA / 2)
            {
                xFF = ParG + (ParC - offsetY) * Math.Tan(radBCG);
            }
            else
            {
                xFF = ParG + (offsetY - ParC) * Math.Tan(radACG);
            }
            //  Coordinata X FM
            if (ParF > offsetY + TA / 2)
            {
                xFM = ParH + (ParF - offsetY) * Math.Tan(radEFH);
            }
            else
            {
                xFM = ParH + (offsetY- ParF) * Math.Tan(radDFH);
            }
            //  Angolo di taglio anima
            radAlfa = Math.Atan((xFF - xFM) / width);

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(xFF - TB * Math.Tan(radAlfa), TB, 0, 0));
            macroPoint.Add(new ProgramPoint(xFF - TB * Math.Tan(radAlfa) + ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(xFF - TB * Math.Tan(radAlfa) - ParR * Math.Sin(radAlfa), TB + ParR * Math.Cos(radAlfa), 0, 0, safeR));
            macroPoint.Add(new ProgramPoint(xFM + (TB * Math.Tan(radAlfa) + ParR * Math.Sin(radAlfa)), width - (TB + ParR * Math.Cos(radAlfa)), 0, 0));
            macroPoint.Add(new ProgramPoint(xFM + TB * Math.Tan(radAlfa) + ParR, width - TB, 0, 0, safeR));
            macroPoint.Add(new ProgramPoint(xFM + TB * Math.Tan(radAlfa), width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            macroPoint.Clear();

            //
            // Cianfrino anima SUP
            //
            double chamferDepth = TA / 2;
            
            ProgramPoint startChamfer = new ProgramPoint(xFF - TB * Math.Tan(radAlfa) - ParR * Math.Sin(radAlfa), TB + ParR * Math.Cos(radAlfa) - ParR / 2, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(xFM + (TB * Math.Tan(radAlfa) + ParR * Math.Sin(radAlfa)), width - (TB + ParR * Math.Cos(radAlfa) - ParR / 2), 0, 0);

            double radO = ParO.ToRad();
            double chamferAngle = (ParC > offsetY + TA / 2) ? radO : radACG > radO ? radACG : radO;


            if (!chamferAngle.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino anima INF
            //
            startChamfer = new ProgramPoint(xFF - TB * Math.Tan(radAlfa) - ParR * Math.Sin(radAlfa), TB + ParR * Math.Cos(radAlfa) - ParR / 2, 0, 0);
            endChamfer = new ProgramPoint(xFM + (TB * Math.Tan(radAlfa) + ParR * Math.Sin(radAlfa)), width - (TB + ParR * Math.Cos(radAlfa) - ParR / 2), 0, 0);

            double radP = ParP.ToRad();

            chamferAngle = (ParC < offsetY + TA / 2) ? radP : radBCG > radP ? radBCG : radP;
            if (!chamferAngle.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            //  Estrusione ala FF
            //
            extrusionDepth = TB + ParR / 4;
            extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);

            macroPoint.Clear();

            //
            // Cianfrino esterno ala FF
            //
            chamferDepth = TB / 2 - ParK / 2;
            chamferAngle = ParI.ToRad();

            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParG, ParC, 0, 0);
                        
            if (!chamferAngle.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParG, ParC, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!chamferAngle.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF
            //
            chamferAngle = ParJ.ToRad();
            double chamferOffset = (TB / 2 + ParK / 2) * Math.Tan(radAlfa);
            if (radAlfa < chamferAngle)//  Avrei messo anche un check per spezzare il cianfrino ed evitare la parte di anima if( ( B / 2 - ParK / 2 ) * tan(parJ) < parR) ma molto probabilmente è superfluo
            {
                startChamfer = new ProgramPoint(ParB - chamferOffset, 0, 0, 0);
                endChamfer = new ProgramPoint(ParG - chamferOffset, ParC, 0, 0);

                if (!chamferAngle.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                        breps.Add(chamferA);
                }
                startChamfer = new ProgramPoint(ParG - chamferOffset, ParC, 0, 0);
                endChamfer = new ProgramPoint(ParA - chamferOffset, topY, 0, 0);

                if (!chamferAngle.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                        breps.Add(chamferA);
                }
            }


            //
            //  Estrusione ala FM
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParH, ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus,- radAlfa);
            //
            // Cianfrino esterno ala FM
            //
            chamferDepth = TB / 2 - ParN / 2;
            chamferAngle = ParM.ToRad();
            if (radAlfa < chamferAngle)
            {
                startChamfer = new ProgramPoint(ParE, 0, 0, 0);
                endChamfer = new ProgramPoint(ParH, ParF, 0, 0);

                if (!chamferAngle.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                startChamfer = new ProgramPoint(ParH, ParF, 0, 0);
                endChamfer = new ProgramPoint(ParD, topY, 0, 0);

                if (!chamferAngle.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }

            //
            // Cianfrino interno ala FM
            //
            chamferAngle = ParL.ToRad();
            chamferOffset = (TB / 2 + ParN / 2) * Math.Tan(radAlfa);

            startChamfer = new ProgramPoint(ParE + chamferOffset, 0, 0, 0);
            endChamfer = new ProgramPoint(ParH + chamferOffset, ParF, 0, 0);
            
            if (!chamferAngle.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParH + chamferOffset, ParF, 0, 0);
            endChamfer = new ProgramPoint(ParD + chamferOffset, topY, 0, 0);
            
            if (!chamferAngle.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, chamferAngle, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }
            ///////////////////////////////
            //      CODA: fiswidth
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}