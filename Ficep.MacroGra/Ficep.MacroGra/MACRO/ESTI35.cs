using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI35 : EyeMacro
    {
        public ESTI35(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth;
            string extrusionPlane;

            //
            // Estrusione anima
            //
            extrusionPlane = "C";
            extrusionDepth = SB;

            double width = SA, topY = SB;

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB + ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - ParR, TB + 2 * ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParB, TB + 2 * ParR, 0, 0));

            macroPoint.Add(new ProgramPoint(ParD, width - (TB + 2 * ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParC - ParR, width - (TB + 2 * ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, width - (TB + ParR), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC - ParR, width - TB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
                        
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala FF
            //
            extrusionPlane = "A";
            double chamferDepth = TB - ParF - ParG;

            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, topY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF UNICO
            //
            chamferDepth = ParG;

            double offsetY = SB / 2;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);
            
            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParI - ParJ;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM UNICO
            //
            chamferDepth = ParJ;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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