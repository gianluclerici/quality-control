using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTC31 : EyeMacro
    {
        public INTC31(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            Brep feature = null;

            double extrusionDepth = TA + Radius;
            string extrusionPlane = Side;

            Point2D centre = new Point2D(ParA, ParB);

            double holeRadius = ParC / 2;

            double chamferDepth = TB - ParD;

             if (!chamferDepth.IsEqualTo(TB, TolLinear))
            {
                EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
                Features.Add(new EyeFeature(feature));
            }

            //
            // Cianfrino esterno
            //
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                ProgramPoint chamferCentre = new ProgramPoint(ParA, ParB, 0, ParC / 2);

                if (EyeGeometryUtils.AddExternalCircularChamfer(chamferCentre, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}