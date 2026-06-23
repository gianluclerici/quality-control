using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI143 : EyeMacro
    {
        public ESTI143(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            ////////////////////////////
            //Calcolo angolo Rat Holes
            ////////////////////////////
            double ratHoleAngleFF = 0;
            double ratHoleAngleFM = 0;
            double root = 0;

            //  FF
            double A = ParB, B = ParC - ParR - ParD, C = 2 * ParR - ParB;

            if (Solve2(A, B, C, ref root))
                ratHoleAngleFF = 2 * Math.Atan(Math.Abs(root));

            //  FM
            B = ParJ - ParR - ParK;

            if (Solve2(A, B, C, ref root))
                ratHoleAngleFM = 2 * Math.Atan(Math.Abs(root));

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParP - ParC, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParP - ParC, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParP - ParC + ParD, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParP - ParR + ParR * Math.Sin(ratHoleAngleFF), TB + ParB - ParR - ParR * Math.Cos(ratHoleAngleFF), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParP - ParR, TB + ParB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParP, TB + ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParQ, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParQ - ParR, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParQ - ParR + ParR * Math.Sin(ratHoleAngleFM), width - (TB + ParB - ParR - ParR * Math.Cos(ratHoleAngleFM)), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParI + ParQ - ParJ + ParK, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParQ - ParJ, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParQ - ParJ, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino interno ala FF
            //
            extrusionPlane = "A";
            double chamferDepth = ParF;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA + ParP - ParC, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA + ParP - ParC, topY, 0, 0);

            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno ala FF
            //
            chamferDepth = TB - ParE - ParF;

            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = ParM;

            startChamfer = new ProgramPoint(ParI + ParQ - ParJ, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI + ParQ - ParJ, topY, 0, 0);

            if (!ParO.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParO.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno ala FM
            //
            chamferDepth = TB - ParL - ParM;

            if (!ParN.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParN.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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