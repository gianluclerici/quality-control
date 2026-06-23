
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class ESTIA03 : EyeMacro
	{
		public ESTIA03(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUQL";
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

            double webWidth = CodePrf == "L" ? SB : SA;
            double radALFA = ParALFA.ToRad();
            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(webWidth * Math.Tan(radALFA), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, webWidth, 0, 0));

            //
            //  ESTRUSIONE
            //
            string extrusionPlane = "C";

            double extrusionDepth = 0.0;
            if (CodePrf == "L")
                extrusionDepth = SA;
            else
                extrusionDepth = SB;

            List<Brep> breps = new List<Brep>();
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino 
            //
            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0),
                         endChamfer = new ProgramPoint(0, SB, 0, 0);

            extrusionPlane = Side == "A" ? "B" : "A";
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                double depth = ParA / Math.Tan(ParBETA.ToRad());
                if(EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), depth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamfer))
                    breps.Add(chamfer);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
