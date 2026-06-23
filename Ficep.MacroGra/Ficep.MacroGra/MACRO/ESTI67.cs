using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI67 : EyeMacro
    {
        public ESTI67(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            Double actualR = (ParR.IsEqualTo(15, TolLinear)) ? ParR + TolWebFlange : ParR; 

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - actualR, TB + actualR, 0, 0, actualR));
            macroPoint.Add(new ProgramPoint(ParC, TB + actualR + (ParA - actualR) * Math.Tan(ParO.ToRad()), 0, 0));
            macroPoint.Add(new ProgramPoint(ParJ, width - (TB + actualR + (ParH - actualR) * Math.Tan(ParP.ToRad())), 0, 0));
            macroPoint.Add(new ProgramPoint(ParH - actualR, width - (TB + actualR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParH - actualR, width - TB, 0, 0, actualR));
            macroPoint.Add(new ProgramPoint(ParI, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino ANIMA
            //

            // i calcoli seguenti servono per allungare il cianfrino sull'anima affinche tagli effettivamente tutto il materiale necessario
            
            double webWidthRemaining = width - 2 * (TB + actualR) - (ParH - actualR) * Math.Tan(ParP.ToRad()) - (ParA - actualR) * Math.Tan(ParO.ToRad());
            double tanGamma = (ParC - ParJ) / webWidthRemaining;

            double chamferDepth = TA;
            
            ProgramPoint startChamfer = new ProgramPoint(ParC + (ParA - actualR) * Math.Tan(ParO.ToRad()) * tanGamma, TB + actualR, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParJ - (ParH - actualR) * Math.Tan(ParP.ToRad()) * tanGamma, width - (TB + actualR), 0, 0);
            
            if (ParALFA > 0)
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, Math.Abs(ParALFA.ToRad()), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            else if (ParALFA < 0)
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, Math.Abs(ParALFA.ToRad()), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FF
            //
            extrusionPlane = "A";
            chamferDepth = TB - ParD - ParE;
            
            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParB, topY, 0, 0);
            
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF
            //
            chamferDepth = ParD;

            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParB, topY, 0, 0);

            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParL - ParK;

            startChamfer = new ProgramPoint(ParI, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI, topY, 0, 0);

            if (!ParN.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParN.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM
            //
            chamferDepth = ParK;

            startChamfer = new ProgramPoint(ParI, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI, topY, 0, 0);

            if (!ParM.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParM.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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