using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI02 : EyeMacro
    {
        public ESTI02(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IU";
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
            Brep feature = null;

            double topY = SB, width = SA, offsetY = SB / 2;

            double tanAlfa = VX == "I" ? Math.Tan(ParALFA.ToRad()) : -Math.Tan(ParALFA.ToRad());

            double lowWingOffsetX = CodePrf == "I" ? offsetY * tanAlfa : TolWebFlange, //   offset di TolWegFlange per evitase che i solidi sulle ali e quello sull'anima si tocchino
                highWingOffsetX = CodePrf == "I" ? - offsetY * tanAlfa : - topY * tanAlfa;

            //
            // Estrusione ala FF
            //
            double extrusionDepth = TB;
            string extrusionPlane = "A";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + lowWingOffsetX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + highWingOffsetX, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala FM
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF + lowWingOffsetX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF + highWingOffsetX, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione anima FF
            //
            extrusionDepth = SB;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB));
            macroPoint.Add(new ProgramPoint(ParE, ParD - (ParD - ParB) * ParE / (ParA - ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //  Cylinder extrusion FF
            Point2D centre = new Point2D(ParA, ParB);
            double radius = ParR;
            EyeGeometryUtils.AddCircleExtrusion(centre, radius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
            Features.Add(new EyeFeature(feature));

            //
            // Estrusione anima FM
            //
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, width - ParG, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL, width - (ParI - (ParI - ParG) * ParL / (ParF - ParS)), 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - (ParI + ParH), 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            //  Cylinder extrusion FM
            centre = new Point2D(ParF, width - ParG);
            EyeGeometryUtils.AddCircleExtrusion(centre, radius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
            Features.Add(new EyeFeature(feature));

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}