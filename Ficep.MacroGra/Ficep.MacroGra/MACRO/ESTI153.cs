using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI153 : EyeMacro
    {
        public ESTI153(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            double ratHoleAngle = 0;
            double root = 0;

            //  FF
            double A = ParB, B = ParA - ParR - ParD, C = 2 * ParR - ParB;

            if (Solve2(A, B, C, ref root))
                ratHoleAngle = 2 * Math.Atan(Math.Abs(root));

            double intChamferDist = TA / 2 + InnerChamferDisFromWeb;
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, TB + ParQ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParD, TB + ParQ, 0, 0));// qUESTO PUNTO LO HA SCELTO IO DIFFERENDO DAL .MAC
            macroPoint.Add(new ProgramPoint(ParE + ParA - ParR + ParR * Math.Sin(ratHoleAngle), TB + ParB - ParR - ParR * Math.Cos(ratHoleAngle), 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA - ParR, TB + ParB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, TB + ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA - ParR, width - (TB + ParB), 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA - ParR + ParR * Math.Sin(ratHoleAngle), width - (TB + ParB - ParR - ParR * Math.Cos(ratHoleAngle)), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC + ParD, width - (TB + ParQ), 0, 0));//uGUALE QUESTO
            macroPoint.Add(new ProgramPoint(ParE, width - (TB + ParQ), 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino superiore anima
            //
            double chamferDepth = ParN;
            
            ProgramPoint startChamfer = new ProgramPoint(ParC, TB + ParB, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParC, width - (TB + ParB), 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino inferiore anima
            //
            chamferDepth = TA - ParN - ParO;

            startChamfer = new ProgramPoint(ParC, TB + ParB, 0, 0);
            endChamfer = new ProgramPoint(ParC, width - (TB + ParB), 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno ala FF
            //
            extrusionPlane = "A";
            chamferDepth = TB - ParH - ParI;

            startChamfer = new ProgramPoint(ParE, 0, 0, 0);
            endChamfer = new ProgramPoint(ParE, topY, 0, 0);

            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala FF
            //
            chamferDepth = ParI;

            startChamfer = new ProgramPoint(ParE, 0, 0, 0);
            endChamfer = new ProgramPoint(ParE, offsetY - intChamferDist, 0, 0);

            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            startChamfer = new ProgramPoint(ParE, topY, 0, 0);
            endChamfer = new ProgramPoint(ParE, offsetY + intChamferDist, 0, 0);

            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParL - ParM;

            startChamfer = new ProgramPoint(ParE, 0, 0, 0);
            endChamfer = new ProgramPoint(ParE, topY, 0, 0);

            if (!ParJ.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParJ.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM
            //
            chamferDepth = ParM;

            startChamfer = new ProgramPoint(ParE, 0, 0, 0);
            endChamfer = new ProgramPoint(ParE, offsetY - intChamferDist, 0, 0);

            if (!ParK.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParK.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            startChamfer = new ProgramPoint(ParE, topY, 0, 0);
            endChamfer = new ProgramPoint(ParE, offsetY + intChamferDist, 0, 0);

            if (!ParK.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParK.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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