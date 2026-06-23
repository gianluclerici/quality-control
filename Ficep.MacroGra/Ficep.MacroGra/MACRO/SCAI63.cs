using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI63 : EyeMacro
    {

        public SCAI63(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //

            //	REG_01 is the torch inclination angle in the wing cut.
            double REG_01 = Math.Atan((ParC - ParA) / ParD);

            macroPoint.Add(new ProgramPoint(ParA + TB * Math.Tan(REG_01), TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParB * Math.Cos(REG_01), ParD - ParB * Math.Sin(REG_01), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + TB * Math.Tan(REG_01), TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala
            //

            extrusionDepth = TB;
            extrusionPlane = Side;

            macroPoint.Add(new ProgramPoint(ParA, SB / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, SB / 2, 0, 0, ParB / 2));
            macroPoint.Add(new ProgramPoint(ParA, SB / 2, 0, 0, ParB / 2));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, - REG_01);


            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}