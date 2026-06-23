using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI167 : EyeMacro
    {
        public ESTI167(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = SB, width = SA;

            double webAngle = Math.Atan(ParA + ParB - ParE - ParF / width);

            double safetyOffset = width * Math.Abs(Math.Tan(webAngle));

            //
            // Estrusione piano B
            //
            double extrusionDepth = width / 2;
            string extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(ParE, topY - ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParF, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(-safetyOffset, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(-safetyOffset, topY - ParG, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -webAngle);
            macroPoint.Clear();

            //
            // Estrusione piano A
            //
            extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(ParA, topY - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(-safetyOffset, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(-safetyOffset, topY - ParC, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, webAngle);
                        
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}