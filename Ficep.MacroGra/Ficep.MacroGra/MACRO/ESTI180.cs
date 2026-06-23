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
    public class ESTI180 : EyeMacro
    {
        public ESTI180(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            Brep feature = null;

            double extrusionDepth;
            string extrusionPlane = "";

            double topY = SB, offsetY = SB / 2, width = SA;

            double tanBeta = Math.Tan(ParBETA.ToRad()), absTanBeta = Math.Abs(tanBeta);

            // CASO 1: intersezione con vertice filo mobile + anima
            // CASO 2: intersezione con la sola anim
            // CASO 3: intersezione con vertice filo fisso + anima

            // Taglio ala FF ( per CASO 1 e 2)
            if ((ParA + ParR > width && ParA - ParR > TB) || (ParA + ParR < width - TB && ParA - ParR > TB))
            {                
                extrusionDepth = ParA - ParR/4;
                extrusionPlane = "A";

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(topY * absTanBeta, ParBETA > 0 ? 0 : topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
                //COME posso gestire la ala mobile inquesto caso?
            }

            // Taglio ala FM ( per CASO 2 e 3)
            if ((ParA + ParR < width - TB && ParA - ParR > TB) || (ParA + ParR < width - TB && ParA - ParR < 0))
            {
                extrusionDepth = width - (ParA + ParR/4);
                extrusionPlane = "B";

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(topY * absTanBeta, ParBETA > 0 ? 0 : topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

                //COME posso gestire la ala fissa inquesto caso?
            }

            //
            //  Estrusione Ellittica
            //

            //  L'ellisse è ottenuto ruotando il cilindro attorno al suo semiasse minore.
            //  Ne consegue che ParR deve essere obbligatoriamente minore di ParC

            if (ParR < ParC)
            {
                Point2D centre = new Point2D(ParB, ParA); // X e Y invertiti

                double minorSemiAxis = ParR;

                double cosAngle = ParR / ParC, radAngle = Math.Acos(cosAngle);

                extrusionDepth = SB / cosAngle + 2 * minorSemiAxis * Math.Tan(radAngle);
                extrusionPlane = "C";

                EyeGeometryUtils.AddCircleExtrusion(centre, minorSemiAxis, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);

                if (!radAngle.IsEqualTo(0, TolAngle))
                    EyeGeometryUtils.RotateSolid(radAngle, ParB, offsetY, "A", Wp, ref feature);

                Features.Add(new EyeFeature(feature));
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}