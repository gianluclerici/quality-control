using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI149 : EyeMacro
    {
        public ESTI149(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double tanH = Math.Tan(ParH.ToRad()), tanI = Math.Tan(ParI.ToRad());

            double webChamferSurplus = ParB.IsEqualTo(0, TolLinear) && ParC.IsEqualTo(0, TolLinear) && ParD.IsEqualTo(0, TolLinear) && ParR.IsEqualTo(0, TolLinear) || TB + ParD < ParE  ? 0 : Surplus;
            //
            // Estrusione anima
            // in due solidi, compresa di cianfrini sulle ali

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC + (TB - ParG - ParJ) * tanH, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC, TB - ParG - ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC, TB - ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC + ParJ * tanI, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, TB + ParD, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, TB + ParD, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParE + ParF, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC + (TB - ParG - ParJ) * tanH, width, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC, width - (TB - ParG - ParJ), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC, width - (TB - ParJ), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC + ParJ * tanI, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, width - (TB + ParD), 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, width - (TB + ParD), 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, width - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - (ParE + ParF), 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino superiore anima FF
            //
            double chamferDepth = TA - ParK - ParL;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA + webChamferSurplus, ParE, 0, 0); 
            ProgramPoint endChamfer = new ProgramPoint(0, ParE + ParF, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino inferiore anima FF
            //
            chamferDepth = ParL;

            startChamfer = new ProgramPoint(ParA + webChamferSurplus, ParE, 0, 0);
            endChamfer = new ProgramPoint(0, ParE + ParF, 0, 0);
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino superiore anima FM
            //
            chamferDepth = TA - ParK - ParL;

            startChamfer = new ProgramPoint(ParA + webChamferSurplus, width - ParE, 0, 0);
            endChamfer = new ProgramPoint(0, width - (ParE + ParF), 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino inferiore anima FM
            //
            chamferDepth = ParL;

            startChamfer = new ProgramPoint(ParA + webChamferSurplus, width - ParE, 0, 0);
            endChamfer = new ProgramPoint(0, width - (ParE + ParF), 0, 0);
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