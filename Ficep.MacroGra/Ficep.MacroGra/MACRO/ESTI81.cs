using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI81 : EyeMacro
    {

        public ESTI81(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            ////////////////////////////
            //Calcolo angolo Rat Holes
            ////////////////////////////
            double ratHoleAngleFF = 0;
            double ratHoleAngleFM = 0;
            double root = 0;

            //  FF
            double A = ParB, B = ParC - ParD - ParR, C = 2 * ParR - ParB;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFF = 2 * Math.Atan(Math.Abs(root));

            //  FM
            B = ParJ - ParR - ParK;
            if (Solve2(A, B, C, ref root))
                ratHoleAngleFM = 2 * Math.Atan(Math.Abs(root));

            double radBeta = VX == "I" ? ParBETA.ToRad() : - ParBETA.ToRad();
            double tanBeta = Math.Tan(radBeta);

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double width = SA, topY = SB, offsetY = SB / 2;

            double offsetX = offsetY * tanBeta;
            double offsetChamfer = TA / 2 * tanBeta;
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC + ParD, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR + ParR * Math.Sin(ratHoleAngleFF), TB + ParB - ParR - ParR * Math.Cos(ratHoleAngleFF), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR, TB + ParB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParP, TB + ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParQ, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParR, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParR + ParR * Math.Sin(ratHoleAngleFM), width - (TB + ParB - ParR - ParR * Math.Cos(ratHoleAngleFM)), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParI - ParJ + ParK, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Cianfrino ANIMA
            //

            // i calcoli seguenti servono per allungare il cianfrino sull'anima affinche tagli effettivamente tutto il materiale necessario

            double webWidthRemaining = width - 2 * (TB + ParB);
            double tanGamma = (ParP - ParQ) / webWidthRemaining;

            double chamferDepth = TA - ParS;

            ProgramPoint startChamfer = new ProgramPoint(ParP + ParR * tanGamma, TB + ParB - ParR, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParQ - ParR * tanGamma, width - (TB + ParB - ParR), 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, Math.Abs(ParALFA.ToRad()), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }


            //
            // Estrusione ala FF
            //
            extrusionDepth = TB;
            extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC + offsetX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC - offsetX, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Cianfrino esterno ala FF
            //

            chamferDepth = TB - ParE - ParF;

            startChamfer = new ProgramPoint(ParA - ParC + offsetX, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA - ParC - offsetX, topY, 0, 0);

            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF
            //

            chamferDepth = ParF;

            if (ParD > ParF * Math.Tan(ParH.ToRad()))
            {
                startChamfer = new ProgramPoint(ParA - ParC + offsetX, 0, 0, 0);
                endChamfer = new ProgramPoint(ParA - ParC - offsetX, topY, 0, 0);

                if (!ParH.IsEqualTo(0, TolAngle))
                {

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            else
            {
                double internalChamferDistanceFromWeb = TA / 2 + InnerChamferDisFromWeb;

                startChamfer = new ProgramPoint(ParA - ParC + offsetX, 0, 0, 0);
                endChamfer = new ProgramPoint(ParA - ParC + offsetChamfer, offsetY - internalChamferDistanceFromWeb, 0, 0);

                if (!ParH.IsEqualTo(0, TolAngle))
                {

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                startChamfer = new ProgramPoint(ParA - ParC - offsetChamfer, offsetY + internalChamferDistanceFromWeb, 0, 0);
                endChamfer = new ProgramPoint(ParA - ParC - offsetX, topY, 0, 0);

                if (!ParH.IsEqualTo(0, TolAngle))
                {

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }

            //
            // Estrusione ala FM
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParJ + offsetX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParJ - offsetX, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala FM
            //
            chamferDepth = TB - ParM - ParL;

            startChamfer = new ProgramPoint(ParI - ParJ + offsetX, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI - ParJ - offsetX, topY, 0, 0);

            if (!ParN.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParN.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM
            //
            chamferDepth = ParM;

            if (ParK > ParM * Math.Tan(ParO.ToRad()))
            {
                startChamfer = new ProgramPoint(ParI - ParJ + offsetX, 0, 0, 0);
                endChamfer = new ProgramPoint(ParI - ParJ - offsetX, topY, 0, 0);

                if (!ParO.IsEqualTo(0, TolAngle))
                {

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParO.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            else
            {
                double internalChamferDistanceFromWeb = TA / 2 + InnerChamferDisFromWeb;

                startChamfer = new ProgramPoint(ParI - ParJ + offsetX, 0, 0, 0);
                endChamfer = new ProgramPoint(ParI - ParJ + offsetChamfer, offsetY - internalChamferDistanceFromWeb, 0, 0);

                if (!ParO.IsEqualTo(0, TolAngle))
                {

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParO.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                startChamfer = new ProgramPoint(ParI - ParJ - offsetChamfer, offsetY + internalChamferDistanceFromWeb, 0, 0);
                endChamfer = new ProgramPoint(ParI - ParJ - offsetX, topY, 0, 0);

                if (!ParO.IsEqualTo(0, TolAngle))
                {

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParO.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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