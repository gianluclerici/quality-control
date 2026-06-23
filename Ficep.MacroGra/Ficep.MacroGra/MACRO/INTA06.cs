using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTA06 : EyeMacro
    {
        public INTA06(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = TB;
            string extrusionPlane = Side;

            double topY = SB, offsetY = SB / 2;

            double REG_02 = ParR - (offsetY - ParB / 2);
            double REG_01 = Math.Sqrt(ParR * ParR - REG_02 * REG_02);
            double REG_04 = (offsetY) * Math.Tan(ParALFA.ToRad());
            //
            // Estrusione ala Side
            //
            macroPoint.Add(new ProgramPoint(ParA - REG_01 - REG_04, topY, 0));
            macroPoint.Add(new ProgramPoint(ParA + REG_01 - REG_04, topY, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - REG_01 - REG_04, topY, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(ParA - REG_01 + REG_04, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + REG_01 + REG_04, 0, 0, 0, -ParR));
            macroPoint.Add(new ProgramPoint(ParA - REG_01 + REG_04, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}