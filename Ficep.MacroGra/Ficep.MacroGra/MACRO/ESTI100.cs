using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI100 : EyeMacro
    {
        public ESTI100(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA / 2;
            string extrusionPlane = "A";

            double width = SA, topY = SB;

            double angleC = Math.Atan((ParD + ParH - ParA - ParG) / width);
            double angleD = Math.Atan((ParF + ParH - ParC - ParG) / width);
            //
            // Estrusioni da lato Side
            //  BASSA
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParG, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG, topY - ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY - ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -angleD);
            macroPoint.Clear();

            //  ALTA
            macroPoint.Add(new ProgramPoint(0, topY - ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG, topY - ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParG, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -angleC);
            macroPoint.Clear();

            //
            // Estrusione da lato Opposite Side
            //
            extrusionPlane = "B";
            //  BASSA
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF + ParH, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParH, topY - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY - ParE, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, angleD);
            macroPoint.Clear();

            //  ALTA
            macroPoint.Add(new ProgramPoint(0, topY - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(ParH, topY - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + ParH, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, angleC);

            //
            // Cianfrino esterno ala Side
            //
            //extrusionPlane = Side;
            //double chamferDepth = TB;
            //
            //ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            //ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            //
            //if (!ParALFA.IsEqualTo(0, TolAngle))
            //{
            //    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
            //        breps.Add(chamferA);
            //}

            //
            // Cianfrino interno ala Side
            //
            //extrusionPlane = Side;
            //double chamferDepth = TB;
            //
            //ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            //ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            //
            //if (!ParBETA.IsEqualTo(0, TolAngle))
            //{
            //    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
            //        breps.Add(chamferA);
            //}
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}