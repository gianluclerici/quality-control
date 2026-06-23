using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI140 : EyeMacro
    {
        public ESTI140(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double ParREG_01 = (2 * ParS > ParH) ? ParS : ParH / 2;

            //
            // Estrusione anima
            //

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB + ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, width - (TB + ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParR, width - TB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();
            //
            // Cianfrino superiore anima
            //
            double chamferDepth = ParM;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA, TB + ParR / 2, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, width - (TB + ParR / 2), 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino inferiore anima
            //
            chamferDepth = TA - ParM - ParN;
            
            startChamfer = new ProgramPoint(ParA, TB + ParR / 2, 0, 0);
            endChamfer = new ProgramPoint(ParA, width - (TB + ParR / 2), 0, 0);
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            //  Estrusione ala A
            //

            extrusionDepth = TB;
            extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParF, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParG, 0, 0));

            if (ParH > 0)
            {
                macroPoint.Add(new ProgramPoint(ParB, offsetY - ParH / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParB, offsetY - ParH / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParS, offsetY - ParH / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - ParH / 2 + ParS, 0, 0, ParS));///Probably needs fixing
                macroPoint.Add(new ProgramPoint(ParA, offsetY + ParH / 2 - ParS, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParS, offsetY + ParH / 2, 0, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParB, offsetY + ParH / 2, 0, 0));
            }

            macroPoint.Add(new ProgramPoint(ParB, topY - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParD, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Cianfrino esterno ala A
            //
            chamferDepth = TB - ParP;
            
            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParB, topY, 0, 0);
            
            if (!ParO.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParO.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            //  Estrusione ala B
            //

            extrusionDepth = TB;
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParK, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, ParL, 0, 0));

            if (ParH > 0)
            {
                macroPoint.Add(new ProgramPoint(ParC, offsetY - ParH / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParC, offsetY - ParH / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParS, offsetY - ParH / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - ParH / 2 + ParS, 0, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + ParH / 2 - ParS, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParS, offsetY + ParH / 2, 0, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParC, offsetY + ParH / 2, 0, 0));
            }

            macroPoint.Add(new ProgramPoint(ParC, topY - ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParI, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala B
            //
            
            startChamfer = new ProgramPoint(ParC, 0, 0, 0);
            endChamfer = new ProgramPoint(ParC, topY, 0, 0);
            
            if (!ParQ.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParQ.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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