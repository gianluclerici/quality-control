using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI24 : EyeMacro
    {
        public ESTI24(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "F";
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

            double extrusionDepth = TA;
            string extrusionPlane = "C";

            double width = SA;
            double offsetAlfa = width * Math.Abs(Math.Tan(ParALFA.ToRad()));
            double radBeta = MirrorInizialeFinale ? - ParBETA.ToRad() : ParBETA.ToRad();
            
            ProgramPoint startChamfer = new ProgramPoint(offsetAlfa, (ParALFA > 0 && MirrorInizialeFinale) || (ParALFA < 0 && !MirrorInizialeFinale) ? width : 0);            
            ProgramPoint endChamfer = new ProgramPoint(0, (ParALFA > 0 && MirrorInizialeFinale) || (ParALFA < 0 && !MirrorInizialeFinale) ? 0 : width, 0, 0);

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(startChamfer);
            macroPoint.Add(new ProgramPoint(0, width));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino piano C
            //
            
            if (radBeta > 0)
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radBeta, extrusionDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            else
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, -radBeta, extrusionDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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