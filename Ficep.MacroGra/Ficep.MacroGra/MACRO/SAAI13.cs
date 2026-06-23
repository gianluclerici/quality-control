using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI13 : EyeMacro
    {
        public SAAI13(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = ParC - ParR >= Radius ? Radius + TolWebFlange : TB;
            string extrusionPlane = Side;

            // actualBmR sta per B meno R e serve ad evitare bug sui cianfrini che intercettano il radiu. lo stesso discorso vale anche per C men R.
            double actualBmR = (ParB - ParR).IsEqualTo(Radius, TolAngle) ? Radius + TolLinear : ParB - ParR - TolWebFlange;
            double actualCmR = (ParC - ParR).IsEqualTo(Radius, TolAngle) ? Radius + TolLinear : ParC - ParR - TolWebFlange;

            double topY = SB, width = SA;
            //
            // Estrusione lato Side
            //
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, topY, 0));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, topY - ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, topY - ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, topY, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Cianfrini esterni Side
            //
            double chamferDepth = TB;

            ProgramPoint startChamfer = new ProgramPoint(ParE - ParA / 2, topY, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParE - ParA / 2, topY - (actualBmR), 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParE + ParA / 2, topY, 0);
            endChamfer = new ProgramPoint(ParE + ParA / 2, topY - (actualBmR), 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Estrusione anima
            //
            extrusionDepth = ParB - ParR >= Radius ? Radius : TA;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrini esterni anima
            //
            chamferDepth = TA;

            //  mirroring manuale cianfrini sull anima
            double chamferStartY = Side == "A" ? 0 - 2 * Surplus : width + 2 * Surplus;
            double chamferEndY = Side == "A" ? actualCmR : width - (actualCmR);

            startChamfer = new ProgramPoint(ParE - ParA / 2, chamferStartY, 0);
            endChamfer = new ProgramPoint(ParE - ParA / 2, chamferEndY, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParE + ParA / 2, chamferStartY, 0);
            endChamfer = new ProgramPoint(ParE + ParA / 2, chamferEndY, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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