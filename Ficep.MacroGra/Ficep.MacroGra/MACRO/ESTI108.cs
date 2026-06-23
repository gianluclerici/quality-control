using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI108 : EyeMacro
    {
        public ESTI108(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            string extrusionPlane = "A";


            double topY = SB, offsetY = SB / 2, width = SA;

            //  extDist è la distanza X del centro circonferenza (esterno al pezzo).
            double extDist = Math.Sqrt(ParR * ParR - ParC * ParC) - ParB;
            //	intDist è la distanza X del punto anima superiore (interno al pezzo).
            double intDist = 0;
            if( ParR > (ParC - offsetY + TA / 2))
            {
                intDist = Math.Sqrt(ParR * ParR - (ParC - offsetY + TA / 2) * (ParC - offsetY + TA / 2)) - extDist;
            }

            //
            // Estrusione ALA A
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Estrusione ALA B
            //

            extrusionPlane = "B";

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ANIMA
            //
            extrusionDepth = SB;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(intDist, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(intDist, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);


            //  TEST CIRCLE
            Brep feature = null;
            extrusionPlane = "A";
            extrusionDepth = SA;

            //Direzioni X e Y invertite per il point 2D (solo su piano "C")
            Point2D centre = new Point2D(- extDist, ParC);

            double radius = ParR;

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