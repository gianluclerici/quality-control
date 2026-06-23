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
    public class ESTIA72 : EyeMacro
    {
        public ESTIA72(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = SB,
                   offsetY = CodePrf == "I" ? SB / 2 : 0,
                   width = SA;

            double reg_01 = Math.Sqrt(ParR * ParR - (CodePrf == "I" ? offsetY * offsetY : topY * topY));
            double reg_02 = ParR - reg_01;

            double radAlfa = ParALFA.ToRad();

            double tanAlfa = Math.Tan(radAlfa);
            double sinAlfa = Math.Sin(radAlfa);
            double cosAlfa = Math.Cos(radAlfa);
            double offsetX = width * tanAlfa;

            //
            // Estrusione animaNEW
            //
            double extrusionDepth = topY;
            string extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(TB * tanAlfa + reg_02 + ParS, TB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(TB * tanAlfa + reg_02 + ParS * sinAlfa, TB + ParS * cosAlfa, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(offsetX - TB * tanAlfa + reg_02 - ParS * sinAlfa, width - TB - ParS * cosAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX - TB * tanAlfa + reg_02 + ParS, width - TB, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //  Estrusione cilindrica passante
            extrusionDepth = SA;
            extrusionPlane = Side;

            Point2D centre = new(-reg_01 + TolWebFlange, offsetY);
            double radius = ParR;

            EyeGeometryUtils.AddCircleExtrusion(centre, radius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus, - radAlfa);

            Features.Add(new EyeFeature(feature));

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
            return true;
        }

    }
}