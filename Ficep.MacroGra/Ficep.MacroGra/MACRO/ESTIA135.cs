using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA135 : EyeMacro
    {
        public ESTIA135(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = SB, offsetY = SB / 2, width = SA;
            // Angle is the torch inclination in the wing cut. (REG_01 del file.MAC)
            double tanAngle = (ParA - ParB) / (width - ParC - TB);

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(tanAngle), TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(tanAngle) + ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(tanAngle), TB + ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - (TB + ParR) * Math.Tan(tanAngle), TB + ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParB, width - TB - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}