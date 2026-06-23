using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA144 : EyeMacro
    {
        public ESTIA144(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //Forzo il mirroring del chianfrino sull'anima
            double webChamferStart = MirrorSideASideB ? width - (TB + TolWebFlange) : TB + TolWebFlange;
            double webChamferEnd = MirrorSideASideB ? (ParC > TB ? ParC : TB + TolWebFlange) : (width - (ParC > TB ? ParC : TB + TolWebFlange));
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width - ParC, 0, 0));

            if (ParR > 0)
            {
                if (ParC < TB)
                {
                    macroPoint.Add(new ProgramPoint(ParA + ParB - ParR, width - ParC, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA + ParB - ParR, width - TB, 0, 0));
                }
                else
                {
                    double sqrtValue = Math.Sqrt(ParR * ParR - (ParC - TB) * (ParC - TB));
                    macroPoint.Add(new ProgramPoint(ParA + ParB - sqrtValue, width - ParC, 0, 0));
                }
                macroPoint.Add(new ProgramPoint(ParA + ParB, width - TB - ParR, 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParA + ParB + ParR, width - TB, 0, 0, ParR));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParA + ParB, width - ParC, 0, 0));
            }

            macroPoint.Add(new ProgramPoint(ParA + ParB, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Cianfrino superiore anima
            //
            double chamferDepth = ParL;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA, webChamferStart, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, webChamferEnd, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Cianfrino inferiore anima
            //
            chamferDepth = TA - ParL - ParM;
            
            startChamfer = new ProgramPoint(ParA, webChamferStart, 0, 0);
            endChamfer = new ProgramPoint(ParA, webChamferEnd, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala Side
            //
            chamferDepth = TB - ParF - ParG;
            extrusionPlane = Side;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParD.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParD.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side
            //
            chamferDepth = ParG;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            ////Se lo vogliamo spezzato con InnerChamferDistance...:
            //
            //startChamfer = new ProgramPoint(0, 0, 0, 0);
            //endChamfer = new ProgramPoint(0, offsetY - InnerChamferDisFromWeb, 0, 0);
            //
            //if (!ParE.IsEqualTo(0, TolAngle))
            //{
            //    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
            //        breps.Add(chamferA);
            //}
            //startChamfer = new ProgramPoint(0, topY, 0, 0);
            //endChamfer = new ProgramPoint(0, offsetY + InnerChamferDisFromWeb, 0, 0);
            //
            //if (!ParE.IsEqualTo(0, TolAngle))
            //{
            //    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
            //        breps.Add(chamferA);
            //}
            //
            // Cianfrino esterno ala Opposite Side
            //
            chamferDepth = TB - ParJ - ParK;
            extrusionPlane = Side == "A" ? "B" : "A";

            startChamfer = new ProgramPoint(ParA + ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParB, topY, 0, 0);

            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side
            //
            chamferDepth = ParK;

            startChamfer = new ProgramPoint(ParA + ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParB, topY, 0, 0);

            if (!ParI.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParI.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            ////Se lo vogliamo spezzato con InnerChamferDistance...:
            //
            //startChamfer = new ProgramPoint(ParA + ParB, 0, 0, 0);
            //endChamfer = new ProgramPoint(ParA + ParB, offsetY - InnerChamferDisFromWeb, 0, 0);
            //
            //if (!ParI.IsEqualTo(0, TolAngle))
            //{
            //    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParI.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
            //        breps.Add(chamferA);
            //}
            //startChamfer = new ProgramPoint(ParA + ParB, topY, 0, 0);
            //endChamfer = new ProgramPoint(ParA + ParB, offsetY + InnerChamferDisFromWeb, 0, 0);
            //
            //if (!ParI.IsEqualTo(0, TolAngle))
            //{
            //    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParI.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
            //        breps.Add(chamferA);
            //}


            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}