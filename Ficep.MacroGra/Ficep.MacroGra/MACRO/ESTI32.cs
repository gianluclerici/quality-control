using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI32 : EyeMacro
    {
        public ESTI32(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParC - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, ParC - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, ParC)); // ArcRadius
            macroPoint.Add(new ProgramPoint(ParA, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, width - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, width - (ParC - 2 * ParR))); // ArcRadius
            macroPoint.Add(new ProgramPoint(ParA, width - (ParC - 2 * ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala FF
            //
            extrusionPlane = "A";
            double chamferDepth = TB;
            
            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, topY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala Side
            //
            extrusionPlane = "B";
            
            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, topY, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            extrusionPlane = "C";
            //
            //  Cilindro FF
            //
            Point2D centre = new Point2D(ParA + ParB, ParC - ParR);
            double holeRadius = ParR;
            Brep feature = null;
            EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
            Features.Add(new EyeFeature(feature));
            //
            //  Cilindro FM
            //
            centre = new Point2D(ParA + ParB, width - (ParC - ParR));
            EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
            Features.Add(new EyeFeature(feature));
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            return true;
        }
    }
}