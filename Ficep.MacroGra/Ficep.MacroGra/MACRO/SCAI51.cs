using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI51 : EyeMacro
    {
        public SCAI51(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "U";
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

            double topY = SB;

            double angleBC = !ParC.IsEqualTo(0, TolLinear) ? Math.Atan(ParB / ParC) : 0;
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParC + ParE, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala Side
            //
            extrusionDepth = TB;
            extrusionPlane = Side;

            macroPoint.Add(new ProgramPoint(0, topY));
            macroPoint.Add(new ProgramPoint(ParA + ParB, topY));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParS, topY));
            macroPoint.Add(new ProgramPoint(ParA + ParB, topY - ParS, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParA + ParB, 0));
            macroPoint.Add(new ProgramPoint(0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, angleBC);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}