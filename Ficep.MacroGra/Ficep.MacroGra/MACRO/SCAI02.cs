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
    public class SCAI02 : EyeMacro
    {

        public SCAI02(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
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

            // POLIGONO
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, ParD - ParE * (ParD - ParB) / ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB - ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (TB - ParF) * Math.Tan(ParALFA.ToRad()), 0, 0, 0));

            //
            //  ESTRUSIONE
            //
            double extrusionDepth = 0;
            string extrusionPlane = "C";

            if (CodePrf == "L")
                extrusionDepth = SA;
            else
                extrusionDepth = SB;

            List<Brep> breps = new List<Brep>();

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            double radius = ParR;
            if (radius > 0)
            {
                // CILINDRO
                Brep feature = null;
                Point2D centre = new Point2D(ParA, ParB);

                EyeGeometryUtils.AddCircleExtrusion(centre, radius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);

                breps.Add(feature);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
