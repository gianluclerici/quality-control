using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI39 : EyeMacro
    {
        public ESTI39(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, topY = SB;

            double internalChamferDistanceFromWeb = TA / 2 + (ParD < InnerChamferDisFromWeb ? InnerChamferDisFromWeb : ParD);

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB + ParK, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParR, TB + ParK, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, TB + ParK + ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, width - (TB + ParK + ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParR, width - (TB + ParK), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, width - (TB + ParK), 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino SUP ANIMA
            //
            double chamferDepth = ParG;

            ProgramPoint startChamfer = new ProgramPoint(ParC, TB + ParK + ParR, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParC, width - (TB + ParK + ParR / 2), 0, 0);

            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino INF ANIMA
            //
            chamferDepth = TB - ParH - ParB;
            startChamfer = new ProgramPoint(ParC, TB + ParK + ParR, 0, 0);
            endChamfer = new ProgramPoint(ParC, width - (TB + ParK + ParR / 2), 0, 0);

            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FF
            //
            extrusionPlane = "A";
            chamferDepth = TB - ParA - ParB;
            
            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF basso
            //
            chamferDepth = ParB;

            double offsetY = SB / 2;
            
            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, offsetY - internalChamferDistanceFromWeb, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF alto
            //
            startChamfer = new ProgramPoint(0, offsetY + internalChamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParS - ParM;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParL.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParL.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF basso
            //
            chamferDepth = ParB;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, offsetY - internalChamferDistanceFromWeb, 0, 0);

            if (!ParI.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParI.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF alto
            //
            startChamfer = new ProgramPoint(0, offsetY + internalChamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParI.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParI.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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