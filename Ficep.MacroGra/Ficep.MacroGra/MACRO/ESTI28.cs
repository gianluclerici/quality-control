using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI28 : EyeMacro
    {
        public ESTI28(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, offsetY = SB / 2, topY = SB;

            double absS = Math.Abs(ParS);

            double ParX = ParR * Math.Cos(Math.Asin((ParB - TB) / ParR));

            double internalChamferDistanceFromWeb = TA / 2 + (ParE < InnerChamferDisFromWeb ? InnerChamferDisFromWeb : ParE);
            //
            // Estrusione anima FF
            //
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParX, ParB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParB, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, ParB + absS));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);            
            macroPoint.Clear();

            //
            // Estrusione anima FM
            //
            macroPoint.Add(new ProgramPoint(ParA, width, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParR, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParX, width - ParB, 0, 0, -ParR));
            macroPoint.Add(new ProgramPoint(0, width - ParB, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, width - (ParB + absS)));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino anima superiore
            //
            double chamferDepth = ParH;
            
            ProgramPoint startChamfer = new ProgramPoint(0, ParB + absS, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, width - (ParB + absS), 0, 0);
            
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino anima inferiore
            //
            chamferDepth = TA - ParI - ParH;

            startChamfer = new ProgramPoint(0, ParB + absS, 0, 0);
            endChamfer = new ProgramPoint(0, width - (ParB + absS), 0, 0);

            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FF alto
            //

            extrusionPlane = "A";
            chamferDepth = TB - ParD - ParC;

            startChamfer = new ProgramPoint(ParA, offsetY + TA /2 + ParL, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FF basso
            //
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - TA / 2 - ParM, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM alto
            //
            extrusionPlane = "B";

            startChamfer = new ProgramPoint(ParA, offsetY + TA / 2 + ParL, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM basso
            //
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - TA / 2 - ParM, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF alto
            //
            extrusionPlane = "A";
            chamferDepth = ParC;

            startChamfer = new ProgramPoint(ParA, offsetY + internalChamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF basso
            //
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - internalChamferDistanceFromWeb, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM alto
            //
            extrusionPlane = "B";
            startChamfer = new ProgramPoint(ParA, offsetY + internalChamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM basso
            //
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - internalChamferDistanceFromWeb, 0, 0);

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