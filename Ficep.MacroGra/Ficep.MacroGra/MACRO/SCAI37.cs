using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI37 : EyeMacro
    {

        public SCAI37(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima secondo SCAI37.MAC
            //

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            //Da SCAI37 -> secondo la mia interpretazione della bitmap reg_01 sempre = ParR
            double reg_01;
            if (ParR > 0 ) reg_01 = Math.Sqrt(ParR * ParR - (ParB - TB) * (ParB - TB));
            else reg_01 = 0;


            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));//1
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));//2
            macroPoint.Add(new ProgramPoint(ParA - ParR, TB, 0, 0));//3
            macroPoint.Add(new ProgramPoint(ParA, TB + ParR, 0, 0, -ParR));//4
            macroPoint.Add(new ProgramPoint(ParA + reg_01, ParB, 0, 0, -ParR));//5
            macroPoint.Add(new ProgramPoint(ParA + ParC - reg_01, ParB, 0, 0));//6
            macroPoint.Add(new ProgramPoint(ParA + ParC, TB + ParR, 0, 0, -ParR));//7
            macroPoint.Add(new ProgramPoint(ParA + ParC + ParR, TB, 0, 0, -ParR));//8
            macroPoint.Add(new ProgramPoint(ParA + ParC, TB, 0, 0));//9
            macroPoint.Add(new ProgramPoint(ParA + ParC, 0, 0, 0));//10


            
             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala Side 1
            //
           
            double topY = SB, offsetY = SB / 2, halfSoul = TA / 2;
           
            extrusionPlane = Side;
            double chamferDepth = TB - ParD - ParE;
            
            //basso
            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, offsetY - halfSoul - ParM, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
            }
            //alto
            startChamfer = new ProgramPoint(ParA, offsetY + halfSoul + ParL, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);
           
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno ala Side 2
            //
            //basso
            startChamfer = new ProgramPoint(ParA + ParC, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParC, offsetY - halfSoul - ParM, 0, 0);
           
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //alto
            startChamfer = new ProgramPoint(ParA + ParC, offsetY + halfSoul + ParL, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParC, topY, 0, 0);
           
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side 1
            //
            //basso
            chamferDepth = ParD;
           
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - halfSoul - ParF, 0, 0);
           
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
            }
            //alto
            startChamfer = new ProgramPoint(ParA, offsetY + halfSoul + ParF, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);
           
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side 2
            //
            //basso
           
            startChamfer = new ProgramPoint(ParA + ParC, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParC, offsetY - halfSoul - ParF, 0, 0);
           
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //alto
            startChamfer = new ProgramPoint(ParA + ParC, offsetY + halfSoul + ParF, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParC, topY, 0, 0);
           
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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