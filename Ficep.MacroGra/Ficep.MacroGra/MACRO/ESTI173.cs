using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI173 : EyeMacro
    {
        public ESTI173(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = TA;
            string extrusionPlane = "C";

            double width = SA, topY = SB;

            double angleB = Math.Atan((ParE - ParA) / width);
            double angleA = Math.Atan((ParG - ParC) / width);

            //
            // Estrusione piano C
            //
            macroPoint.Add(new ProgramPoint(0, width));
            macroPoint.Add(new ProgramPoint(ParA, width, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, width - ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione piano D
            //
            extrusionPlane = "D";

            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(new ProgramPoint(ParG, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParH, width - ParF, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Estrusione piano FM
            //
            extrusionPlane = "B";
            extrusionDepth = Radius;

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione piano FF
            //
            extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}