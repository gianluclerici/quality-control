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
    public class INTC20 : EyeMacro
    {
        public INTC20(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUQ";
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

            double extrusionDepth = TA + Radius;
            string extrusionPlane = Side;

            Point2D centre;

            double holeRadius = ParC / 2;

            //
            // Estrusione anima
            //

            double counterX = 0;

            double sinAlfa = Math.Sin(ParALFA.ToRad()), cosAlfa = Math.Cos(ParALFA.ToRad());
            while (counterX <= ParF)
            {
                double counterY = 0;
                while (counterY <= ParG)
                {
                    centre = new Point2D(counterX * ParD * cosAlfa + counterY * (-ParE * sinAlfa) + ParA, counterX * ParD * sinAlfa + counterY * ParE * cosAlfa + ParB);
                    if (CodePrf == "Q" && extrusionPlane == "C")
                    {
                        if (ParI == 0 || ParI == 2)
                        {
                            EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, "C", extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
                            Features.Add(new EyeFeature(feature));
                        }
                        if (ParI == 1 || ParI == 2)
                        {
                            EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, "D", extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);

                            Features.Add(new EyeFeature(feature));
                        }
                    }
                    else
                    {
                        EyeGeometryUtils.AddCircleExtrusion(centre, holeRadius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);

                        Features.Add(new EyeFeature(feature));
                    }

                    counterY++;
                }
                counterX++;
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}