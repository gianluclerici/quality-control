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
    public class ESTIA146 : EyeMacro
    {
        public ESTIA146(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB - ParR, width - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB, width - (ParD - ParR), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParB, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB - ParC, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB - ParC, width + 2 * Surplus, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width + 2 * Surplus, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            //
            // Estrusione ala Opposite Side
            //
            if (ParE > 0)
            {
                extrusionDepth = ParD - ParR;
                extrusionPlane = Side == "A" ? "B" : "A";

                Brep feature = null;
            
                double centreX = ParA + ParB - ParC - Math.Sqrt(ParE * ParE - offsetY * offsetY);
            
                Point2D centre = new Point2D(centreX, offsetY);
            
                double radius = ParE;
            
                EyeGeometryUtils.AddCircleExtrusion(centre, radius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
                Features.Add(new EyeFeature(feature));
            }

            ///////////////////////////////
            //      CODA: mobile in questo caso. Anticipato Features.Addrange per poter generare correttamente estrusione anima
            ///////////////////////////////           

            return true;
        }

    }
}