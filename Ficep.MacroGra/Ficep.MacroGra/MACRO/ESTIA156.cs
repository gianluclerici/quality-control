using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA156 : EyeMacro
    {
        public ESTIA156(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "Q";
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
            Brep feature = null;

            double extrusionDepth = SA;
            string extrusionPlane = Side;

            double topY = SB, offsetY = SB / 2, width = SA;

            //  Estrusione cilindrica passante
            double radAlfa = ParALFA.ToRad();
            double extDist = Math.Sqrt(ParR * ParR - offsetY * offsetY);

            Point2D centre = new(-extDist + TolWebFlange, offsetY);
            double radius = ParR + (ParR.IsEqualTo(offsetY, TolLinear) ? TolWebFlange : 0);

            EyeGeometryUtils.AddCircleExtrusion(centre, radius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, !MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus, -radAlfa);

            Features.Add(new EyeFeature(feature));

            //
            // Cianfrino esterno ala Side
            //
            extrusionPlane = "C";
            double chamferDepth = TA - ParB;
            
            ProgramPoint startChamfer = new ProgramPoint(0, MirrorSideASideB ? width : 0);
            ProgramPoint endChamfer = new ProgramPoint(width * Math.Abs(Math.Tan(radAlfa)), MirrorSideASideB ? 0:  width);
            
            if (!ParA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
                //  Ciandrino temporaneo fimnche non ci sono cianfrini su D
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParA.ToRad(), chamferDepth, MirrorInizialeFinale, true, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, true))
                    breps.Add(chamferA);
            }

            //  Estursione su C per eliminare sfridi
            extrusionDepth = SB;
            macroPoint.Add(new ProgramPoint(0, !MirrorSideASideB ? width : 0, 0, 0));
            macroPoint.Add(startChamfer);
            macroPoint.Add(endChamfer);
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}