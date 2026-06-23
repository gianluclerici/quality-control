
using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI58 : EyeMacro
    {

        public SCAI58(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double REG_01 = Math.Sqrt(ParR * ParR - (ParB - ParR - ParC) * (ParB - ParR - ParC));

            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB - ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR, ParB, 0, 0, ParR));

            if (ParC < ParB)
            {
                macroPoint.Add(new ProgramPoint(ParA - ParR - REG_01, ParC, 0, 0, ParR));
            }

            if (ParS != 0)
            {
                macroPoint.Add(new ProgramPoint(ParS, ParC, 0, 0));
                macroPoint.Add(new ProgramPoint(0, ParC + Math.Abs(ParS), 0, 0, -ParS));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParE, ParC, 0, 0));
                macroPoint.Add(new ProgramPoint(0, ParC + ParD, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala Side Basso
            //
            extrusionPlane = Side;
            extrusionDepth = TB + Radius;


            macroPoint.Add(new ProgramPoint(ParA + ParI, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, SB / 2 - ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala Side Alto
            //

            macroPoint.Add(new ProgramPoint(ParA, SB / 2 + ParG, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParF, SB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, SB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}