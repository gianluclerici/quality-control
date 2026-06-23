using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI157 : EyeMacro
    {
        public ESTI157(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = SB, offsetY = SB / 2, width = SA;
            double intChamferDist = TA / 2 + InnerChamferDisFromWeb;
            ////////////////////////////
            //Calcolo angolo Rat Holes
            ////////////////////////////
            double ratHoleAngleFF = 0;
            double ratHoleAngleFM = 0;
            double root = 0;

            //  FF
            double A = ParB - ParS, B = ParC - ParR - ParD, C = 2 * ParR - ParB;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFF = 2 * Math.Atan(Math.Abs(root));

            //  FM
            B = ParJ - ParR - ParK;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFM = 2 * Math.Atan(Math.Abs(root));


            //double ParREG05 = (PZORG == 0 || PZORG == 3) ? ParBETA : -ParBETA;
            double radBeta = ParBETA.ToRad(), tanBeta = Math.Tan(radBeta);
            
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC + ParS * Math.Tan(ParH.ToRad()), TB + ParS, 0, 0));// inventato
            macroPoint.Add(new ProgramPoint(ParA - ParC + ParD, TB + ParS, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR + ParR * Math.Sin(ratHoleAngleFF), TB + ParB - ParR - ParR * Math.Cos(ratHoleAngleFF), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR, TB + ParB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParL, TB + ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParQ, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParR, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParR + ParR * Math.Sin(ratHoleAngleFM), width - (TB + ParB - ParR - ParR * Math.Cos(ratHoleAngleFM)), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParI - ParJ + ParK, width - TB - ParS, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParJ + ParS * Math.Tan(ParH.ToRad()), width - TB - ParS, 0, 0));// inventato
            macroPoint.Add(new ProgramPoint(ParI - ParJ, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Cianfrino superiore anima
            //
            double chamferDepth = ParN;

            ProgramPoint startChamfer = new ProgramPoint(ParL, TB + ParB, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParQ, width - (TB + ParB), 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino inferiore anima
            //
            chamferDepth = TA - ParN - ParO;

            startChamfer = new ProgramPoint(ParL, TB + ParB, 0, 0);
            endChamfer = new ProgramPoint(ParQ, width - (TB + ParB), 0, 0);

            if (!ParM.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParM.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }


            //
            // Estrusione ala FF
            //

            extrusionPlane = "A";
            extrusionDepth = TB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC + topY * tanBeta / 2, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC - topY * tanBeta / 2, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Cianfrino esterno ala FF
            //
            chamferDepth = TB - ParF - ParE;
            
            startChamfer = new ProgramPoint(ParA - ParC + offsetY * tanBeta, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA - ParC - offsetY * tanBeta, topY, 0, 0);
            
            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF
            //
            chamferDepth = ParF;
            
            startChamfer = new ProgramPoint(ParA - ParC + offsetY * tanBeta, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA - ParC + intChamferDist * tanBeta, offsetY - intChamferDist, 0, 0);
            
            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParA - ParC - offsetY * tanBeta, topY, 0, 0);
            endChamfer = new ProgramPoint(ParA - ParC - intChamferDist * tanBeta, offsetY + intChamferDist, 0, 0);

            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Estrusione ala FM
            //

            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParJ + topY * tanBeta / 2, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParJ - topY * tanBeta / 2, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala FM
            //
            chamferDepth = TB - ParF - ParE;
            
            startChamfer = new ProgramPoint(ParI - ParJ + offsetY * tanBeta, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI - ParJ - offsetY * tanBeta, topY, 0, 0);
            
            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Cianfrino interno ala FM
            //
            chamferDepth = ParF;
            
            startChamfer = new ProgramPoint(ParI - ParJ + offsetY * tanBeta, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI - ParJ + intChamferDist * tanBeta, offsetY - intChamferDist, 0, 0);
            
            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            startChamfer = new ProgramPoint(ParI - ParJ - intChamferDist * tanBeta, offsetY + intChamferDist, 0, 0);
            endChamfer = new ProgramPoint(ParI - ParJ - offsetY * tanBeta, topY, 0, 0);

            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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