using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI21 : EyeMacro
    {

        public ESTI21(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double absR = Math.Abs(ParR), absS = Math.Abs(ParS);

            double lowerChamferDistance = TA / 2 + (InnerChamferDisFromWeb > ParF ? InnerChamferDisFromWeb : ParF);
            double upperChamferDistance = TA / 2 + (InnerChamferDisFromWeb > ParE ? InnerChamferDisFromWeb : ParE);
            //
            // Estrusione anima FF
            //

            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(absR, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + absR, 0, 0, -ParR));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione anima FM
            //

            macroPoint.Add(new ProgramPoint(ParC, width, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, width - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(absS, width - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - ParD - absS, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            double radAlfa = ParALFA.ToRad(), radBeta = ParBETA.ToRad();
            //
            // Cianfrino esterno ala FF
            //

            extrusionPlane = "A";
            double chamferDepth = TB - ParG - ParH;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF basso
            //

            chamferDepth = ParH;
            
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - lowerChamferDistance, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF alto
            //
           
            startChamfer = new ProgramPoint(ParA, offsetY + upperChamferDistance, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParI - ParL;

            startChamfer = new ProgramPoint(ParC, 0, 0, 0);
            endChamfer = new ProgramPoint(ParC, topY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM basso
            //
            extrusionPlane = "B";
            chamferDepth = ParL;
            
            startChamfer = new ProgramPoint(ParC, 0, 0, 0);
            endChamfer = new ProgramPoint(ParC, offsetY - lowerChamferDistance, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM alto
            //

            startChamfer = new ProgramPoint(ParC, offsetY + upperChamferDistance, 0, 0);
            endChamfer = new ProgramPoint(ParC, topY, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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