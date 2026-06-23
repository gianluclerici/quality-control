using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA85 : EyeMacro
    {
        public ESTIA85(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, topY = SB, offsetY = SB / 2;

            double wingY = Side == "A" ? topY - ParO : ParO;

            //ANGOLO RATHOLE VICINO ALA FF
            double root = 0;
            double ratHoleAngleFF02 = 0;
            double A = ParS + ParR, B = ParA - ParC - ParD - ParR, C = ParR - ParS;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFF02 = 2 * Math.Atan(Math.Abs(root));
            //ANGOLO RATHOLE VICINO CENTRO ANIMA FF
            double ratHoleAngleFF03 = 0;
            A = ParB - ParS + ParR; B = ParA - ParC - ParR; C = ParR - ParB + ParS;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFF03 = 2 * Math.Atan(Math.Abs(root));
            //ANGOLO RATHOLE VICINO ALA FM
            double ratHoleAngleFM02 = 0;
            A = ParK + ParR; B = ParI - ParC - ParL - ParR; C = ParR - ParK;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFM02 = 2 * Math.Atan(Math.Abs(root));
            //ANGOLO RATHOLE VICINO CENTRO ANIMA FM
            double ratHoleAngleFM03 = 0;
            A = ParJ - ParK + ParR; B = ParI - ParC - ParR; C = ParR - ParJ + ParK;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFM03 = 2 * Math.Atan(Math.Abs(root));

            //	radPO e' l'angolo di inclinazione sull'ala superiore.
            double radPO = Math.Atan(ParP / ParO);

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParD, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR + ParR * Math.Sin(ratHoleAngleFF02), TB + ParS - ParR * Math.Cos(ratHoleAngleFF02), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB + ParS, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - ParR + ParR * Math.Sin(ratHoleAngleFF03), TB + ParS + ParR * Math.Cos(ratHoleAngleFF03), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, TB + ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, width - (TB + ParJ), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParR + ParR * Math.Sin(ratHoleAngleFM03), width - (TB + ParK + ParR * Math.Cos(ratHoleAngleFM03)), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, width - (TB + ParK), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParI - ParR + ParR * Math.Sin(ratHoleAngleFM02), width - (TB + ParK - ParR * Math.Cos(ratHoleAngleFM02)), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC + ParL, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala FF
            //
            extrusionDepth = TB;
            extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParQ, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, wingY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParP, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, wingY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Cianfrino esterno ala FF
            //
            double chamferDepth = TB - ParE - ParF;
            
            ProgramPoint startChamfer = new ProgramPoint(ParQ, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, wingY, 0, 0);
            
            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            startChamfer = new ProgramPoint(0, wingY, 0, 0);
            endChamfer = new ProgramPoint(ParP, topY, 0, 0);

            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF
            //
            chamferDepth = ParF;

            startChamfer = new ProgramPoint(ParQ, 0, 0, 0);
            endChamfer = new ProgramPoint(0, wingY, 0, 0);

            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }

            startChamfer = new ProgramPoint(0, wingY, 0, 0);
            endChamfer = new ProgramPoint(ParP, topY, 0, 0);

            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }

            //
            // Estrusione ala FM
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParQ, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, wingY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParP, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, wingY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);            
            //
            // Cianfrino esterno ala FM
            //
            chamferDepth = TB - ParM - ParN;

            startChamfer = new ProgramPoint(ParQ, 0, 0, 0);
            endChamfer = new ProgramPoint(0, wingY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            startChamfer = new ProgramPoint(0, wingY, 0, 0);
            endChamfer = new ProgramPoint(ParP, topY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM
            //
            chamferDepth = ParM;

            startChamfer = new ProgramPoint(ParQ, 0, 0, 0);
            endChamfer = new ProgramPoint(0, wingY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }

            startChamfer = new ProgramPoint(0, wingY, 0, 0);
            endChamfer = new ProgramPoint(ParP, topY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, false, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
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