using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI14 : EyeMacro
    {

        public ESTI14(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, topY = SB, offsetY = SB / 2;

            double chamferDistanceFromWeb = TA / 2 + (ParM < InnerChamferDisFromWeb ? InnerChamferDisFromWeb : ParM);

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));//2
            macroPoint.Add(new ProgramPoint(ParALFA + ParR, TB, 0, 0));//2
            macroPoint.Add(new ProgramPoint(ParALFA, TB + ParR, 0, 0, ParR));//3
            macroPoint.Add(new ProgramPoint(ParBETA, width - TB - ParR, 0, 0));//4
            macroPoint.Add(new ProgramPoint(ParBETA + ParR, width - TB, 0, 0, ParR));//5
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));//6S
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala FF
            //

            extrusionPlane = "A";
            extrusionDepth = TB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParS, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala FM
            //

            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL + ParS, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala FF
            //
            extrusionPlane = "A";
            double chamferDepth = TB - ParC - ParD;
            
            ProgramPoint startChamfer = new ProgramPoint(ParI, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParI + ParS, topY, 0, 0);
            
            if (!ParA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF basso
            //

            chamferDepth = ParD;

            double tanGamma = ParS / SB;        

            startChamfer = new ProgramPoint(ParI, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI + (offsetY - chamferDistanceFromWeb) * tanGamma, offsetY - chamferDistanceFromWeb, 0, 0);
            
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF alto
            //

            startChamfer = new ProgramPoint(ParI + (offsetY + chamferDistanceFromWeb) * tanGamma, offsetY + chamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(ParI + ParS, topY, 0, 0);

            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParH - ParG;

            startChamfer = new ProgramPoint(ParL, 0, 0, 0);
            endChamfer = new ProgramPoint(ParL + ParS, topY, 0, 0);

            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM basso
            //

            chamferDepth = ParH;

            startChamfer = new ProgramPoint(ParL, 0, 0, 0);
            endChamfer = new ProgramPoint(ParL + (offsetY - chamferDistanceFromWeb) * tanGamma, offsetY - chamferDistanceFromWeb, 0, 0);

            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM alto
            //

            startChamfer = new ProgramPoint(ParL + (offsetY + chamferDistanceFromWeb) * tanGamma, offsetY + chamferDistanceFromWeb, 0, 0);
            endChamfer = new ProgramPoint(ParL + ParS, topY, 0, 0);

            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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