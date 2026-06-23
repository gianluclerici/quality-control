using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTA20 : EyeMacro
    {
        public INTA20(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQ";
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

            // AddCircleExtrusion non accetta la lista breps
            Brep feature = null;

            double extrusionDepth = TB;
            string extrusionPlane = Side;

            Point2D centre;

            double holeRadius = ParC / 2;

            double counterX = 0;
            while (counterX <= ParF)
            {
                double counterY = 0;
                while (counterY <= ParG)
                {
                    centre = new Point2D(counterX * ParD + ParA, counterY * ParE + ParB);
                    EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
                    Features.Add(new EyeFeature(feature));

                    counterY += 1;
                }
                counterX += 1;
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}