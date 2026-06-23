using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI76 : EyeMacro
    {
        public SCAI76(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = TB;
            string extrusionPlane = Side;

            double topY = SB;

            double bevelAngle = ParALFA.ToRad();
            double bevelOffset = (TB - ParE) * Math.Tan(bevelAngle);
            double acAngle = Math.Atan(ParC / ParA), adAngle = Math.Atan(ParD / ParA);

            double realAngleWithtrue = Math.Atan(5.195/8.998).ToDeg();
            double realAngleWithfalse = Math.Atan(4.155 / 8.997).ToDeg();

            ////
            //// Estrusione anima
            ////
            macroPoint.Add(new ProgramPoint(0, topY - ParB - ParC - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY - ParB - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY - ParB, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);


            ////
            //// Estrusione anima 2
            ////


            //macroPoint.Add(new ProgramPoint(0, topY - ParB - ParC, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParA, topY - ParB - ParC, 0, 0));
            //macroPoint.Add(new ProgramPoint(0, topY - ParB - ParC - ParD, 0, 0));
            ////
            //EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, Math.PI / 4, Math.PI / 4);

            //
            // Cianfrini esterni
            //
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                double chamferDepth = TB - ParE;
            
                ProgramPoint startChamfer = new ProgramPoint(ParA, topY - ParB - ParC, 0, 0);
                ProgramPoint endChamfer = new ProgramPoint(0, topY - ParB, 0, 0);            
            
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            
                endChamfer = new ProgramPoint(0, topY - ParB - ParC - ParD, 0, 0);
            
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, false, false))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side
            //
            //extrusionPlane = Side;
            //double chamferDepth = TB;
            //
            //ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            //ProgramPoint endChamfer = new ProgramPoint(ParA, SB, 0, 0);
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