using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI110 : EyeMacro
    {
        public ESTI110(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;
            string extrusionPlane = "A";

            double offsetY = SB / 2, topY = SB;

            double supXChamfer = InnerChamferDisFromWeb * ParA / offsetY, infXChamfer = InnerChamferDisFromWeb * ParB / offsetY; 

            //
            // Estrusione ALA
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, offsetY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(0, offsetY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();
            //
            // Cianfrino esterno ala A
            //
            double chamferDepth = TB - ParC - ParD;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA, topY, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, offsetY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(0, offsetY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala A
            //
            chamferDepth = ParD;

            startChamfer = new ProgramPoint(ParA, topY, 0, 0);
            endChamfer = new ProgramPoint(supXChamfer, offsetY + InnerChamferDisFromWeb, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(infXChamfer, offsetY - InnerChamferDisFromWeb, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }
        
            //
            // Cianfrino esterno ala B
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParC - ParD;

            startChamfer = new ProgramPoint(ParA, topY, 0, 0);
            endChamfer = new ProgramPoint(0, offsetY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(0, offsetY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala B
            //
            chamferDepth = ParD;

            startChamfer = new ProgramPoint(ParA, topY, 0, 0);
            endChamfer = new ProgramPoint(supXChamfer, offsetY + InnerChamferDisFromWeb, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(infXChamfer, offsetY - InnerChamferDisFromWeb, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
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