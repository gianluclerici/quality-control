using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI12 : EyeMacro
    {

        public ESTI12(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            string extrusionPlane;

            double internalChamferDistanceFromWeb = TA / 2 + (ParM < InnerChamferDisFromWeb ? InnerChamferDisFromWeb : ParM);

            //
            // Cianfrino esterno ala FF
            //

            double topY = SB;

            extrusionPlane = "A";
            double chamferDepth = TB - ParC - ParD;
            
            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, topY, 0, 0);
            
            if (!ParA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF basso
            //

            chamferDepth = ParD;
            double offsetY = SB / 2;
            
            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, offsetY - internalChamferDistanceFromWeb, 0, 0);
            
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF alto
            //

            chamferDepth = ParD;

            startChamfer = new ProgramPoint(0, offsetY + internalChamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM
            //

            extrusionPlane = "B";
            chamferDepth = TB - ParG - ParH;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM basso
            //

            chamferDepth = ParH;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, offsetY - internalChamferDistanceFromWeb, 0, 0);

            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM alto
            //

            chamferDepth = ParH;

            startChamfer = new ProgramPoint(0, offsetY + internalChamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);

            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino superiore anima
            //

            extrusionPlane = "C";
            chamferDepth = ParL;

            startChamfer = new ProgramPoint(0, TB + ParI, 0, 0);
            endChamfer = new ProgramPoint(0, SA - TB - ParI, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino inferiore anima
            //

            chamferDepth = TA - ParL - ParS;

            startChamfer = new ProgramPoint(0, TB + ParI, 0, 0);
            endChamfer = new ProgramPoint(0, SA - TB - ParI, 0, 0);

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