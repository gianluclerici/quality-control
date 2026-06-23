using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI57 : EyeMacro
    {
        public ESTI57(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;
            string extrusionPlane = "A";

            double reg_01 = Math.Sqrt(ParR * ParR - SB * SB / 4);
            double reg_02 = ParR - reg_01;
            double reg_03 = Math.Sqrt(ParR * ParR - TA * TA / 4);

            double offsetY = SB / 2, topY = SB;

            //
            // Estrusione ala semicerchio
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(reg_03 - reg_01, offsetY - TA / 2, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(reg_03 - reg_01, offsetY + TA / 2, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0, ParR));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            //Estrusione anima
            //
            extrusionDepth = SB;
            extrusionPlane = "C";

            double width = SA;

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(reg_02 + ParS, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(reg_02, TB + ParS, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(reg_02, width - (TB + ParS), 0, 0));
            macroPoint.Add(new ProgramPoint(reg_02 + ParS, width - TB, 0, 0, ParS));
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