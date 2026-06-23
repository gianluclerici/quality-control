using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA86 : EyeMacro
    {
        public ESTIA86(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;
            string extrusionPlane = "A";

            double tanBeta = Math.Tan(ParBETA.ToRad());

            ProgramPoint startChamfer = new ProgramPoint(0, extrusionDepth, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(extrusionDepth * tanBeta, 0, 0, 0);

            //
            // Estrusione passante dal piano FF
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(startChamfer);
            macroPoint.Add(endChamfer);
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorSideASideB, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrini esterni 
            //
            double chamferDepth = TB - ParA;
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                //  Piani A e B
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "A", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorSideASideB, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
                //
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "B", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorSideASideB, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
                
                //  Piani C e D                
                startChamfer = new ProgramPoint(chamferDepth * tanBeta, extrusionDepth, 0, 0);
                endChamfer = new ProgramPoint(chamferDepth * tanBeta, 0, 0, 0);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "C", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorSideASideB, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);

                startChamfer = new ProgramPoint((extrusionDepth - chamferDepth) * tanBeta, extrusionDepth, 0, 0);
                endChamfer = new ProgramPoint((extrusionDepth - chamferDepth) * tanBeta, 0, 0, 0);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "D", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorSideASideB, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
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