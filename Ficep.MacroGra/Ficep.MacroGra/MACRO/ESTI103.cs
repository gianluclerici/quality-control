using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI103 : EyeMacro
    {
        public ESTI103(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA;
            double topY = SB, offsetY = SB / 2;
            double REG_01 = 0, REG_02 = 0;
            if (ParR > 0)
            {
                REG_01 = Math.Sqrt(ParR * ParR - Math.Pow(ParALFA - ParI, 2));
                REG_02 = Math.Sqrt(ParR * ParR - Math.Pow(ParBETA - ParL, 2));
            }
            //  Dati per cianfrini interni
            double tanJN = (ParN / ParJ), tanJK = (ParK / (topY - ParJ));

            double lowerChamferX = ParJ < offsetY - ParM ? 0 : (ParJ - (offsetY - ParM)) * tanJN;
            double lowerChamferY = ParJ < offsetY - ParM ? ParJ : (offsetY - ParM);
            double middleChamferX1 = ParJ < offsetY - ParM ? 0 : (ParJ - (offsetY + ParM)) * tanJN;
            double middleChamferY1 = ParJ < offsetY - ParM ? ParJ : (offsetY + ParM);
            double middleChamferX2 = ParJ < offsetY - ParM ? (offsetY - ParM - ParJ) * tanJK : 0;
            double middleChamferY2 = ParJ < offsetY - ParM ? (offsetY - ParM) : ParJ;
            double upperChamferX = ParJ < offsetY - ParM ? (offsetY + ParM - ParJ) * tanJK : ParJ > offsetY + ParM ? 0 : (offsetY + ParM - ParJ) * tanJK;
            double upperChamferY = ParJ < offsetY - ParM ? (offsetY + ParM) : ParJ > offsetY + ParM ? ParJ : offsetY + ParM;

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParR, TB, 0, 0));
            if (ParI >= ParALFA)
            {
                macroPoint.Add(new ProgramPoint(ParI, TB + ParR, 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParALFA, TB + ParR, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParALFA, TB + REG_01, 0, 0, ParR));
            }
            if (ParL >= ParBETA)
            {
                macroPoint.Add(new ProgramPoint(ParBETA, width - TB - ParR, 0, 0));
                macroPoint.Add(new ProgramPoint(ParL, width - TB - ParR, 0, 0));
                macroPoint.Add(new ProgramPoint(ParL + ParR, width - TB, 0, 0, ParR));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParBETA, width - TB - REG_02, 0, 0));
                macroPoint.Add(new ProgramPoint(ParL + ParR, width - TB, 0, 0, ParR));
            }
            macroPoint.Add(new ProgramPoint(ParL, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala FF
            //
            extrusionDepth = TB + Surplus;
            extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParN, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParK, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Cianfrino esterno ala FF
            //
            double chamferDepth = TB - ParC - ParD;
            
            ProgramPoint startChamfer = new ProgramPoint(ParI + ParN, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParI, ParJ, 0, 0);
            
            if (!ParA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            startChamfer = new ProgramPoint(ParI, ParJ, 0, 0);
            endChamfer = new ProgramPoint(ParI + ParK, topY, 0, 0);
            
            if (!ParA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Cianfrino interno ala FF
            //
            chamferDepth = ParD;
            
            startChamfer = new ProgramPoint(ParI + ParN, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI + lowerChamferX, lowerChamferY, 0, 0);
            
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            if(ParJ < offsetY - ParM || ParJ > offsetY + ParM)
            {
                startChamfer = new ProgramPoint(ParI + middleChamferX1, middleChamferY1, 0, 0);
                endChamfer = new ProgramPoint(ParI + middleChamferX2, middleChamferY2, 0, 0);
            
                if (!ParB.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            
            
            startChamfer = new ProgramPoint(ParI + upperChamferX, upperChamferY, 0, 0);
            endChamfer = new ProgramPoint(ParI + ParK, topY, 0, 0);
            
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Estrusione ala FM
            //
            extrusionPlane = "B";
            
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL + ParN, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL, ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL + ParK, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            //
            // Cianfrino esterno ala FM
            //
            chamferDepth = TB - ParH - ParG;
            
            startChamfer = new ProgramPoint(ParL + ParN, 0, 0, 0);
            endChamfer = new ProgramPoint(ParL, ParJ, 0, 0);
            
            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            startChamfer = new ProgramPoint(ParL, ParJ, 0, 0);
            endChamfer = new ProgramPoint(ParL + ParK, topY, 0, 0);
            
            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Cianfrino interno ala FM
            //
            chamferDepth = ParH;
            
            startChamfer = new ProgramPoint(ParL + ParN, 0, 0, 0);
            endChamfer = new ProgramPoint(ParL + lowerChamferX, lowerChamferY, 0, 0);
            
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            if (ParJ < offsetY - ParM || ParJ > offsetY + ParM)
            {
                startChamfer = new ProgramPoint(ParL + middleChamferX1, middleChamferY1, 0, 0);
                endChamfer = new ProgramPoint(ParL + middleChamferX2, middleChamferY2, 0, 0);
            
                if (!ParF.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            
            startChamfer = new ProgramPoint(ParL + upperChamferX, upperChamferY, 0, 0);
            endChamfer = new ProgramPoint(ParL + ParK, topY, 0, 0);
            
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}