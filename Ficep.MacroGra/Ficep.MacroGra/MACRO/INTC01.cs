using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class INTC01 : EyeMacro
	{

		public INTC01(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQFR";
        }

        public bool CreateMacroR()
        {
            //  Verifico che il profilo sia tra quelli abilitati
            if (CodePrf != "R")
                return false;

            double radiusInnerCircle = SA / 2 - TA;
            double radiusCylinderFeature = ParF / 2;
            // Minimum lenght that the subtracted cylinder have to be: TA + x
            // x = Rin - sqrt(Rin ^ 2 - (D/2)^2)
            // Rin = radius inner cylinder
            // D = diameter cylinder feature
            double minimumLenght = TA + radiusInnerCircle - Math.Sqrt((radiusInnerCircle * radiusInnerCircle) - (radiusCylinderFeature * radiusCylinderFeature));
            double lenght = minimumLenght + Surplus;
            Point3D rotationCenter = new Point3D(0, SA / 2, SA / 2);

            Brep feature = Brep.CreateCylinder(ParF / 2, lenght, TolBrep);
            // Translate the feature at the top of the raw part
            feature.Translate(ParE, SA / 2, SA - lenght + Surplus / 2);

            if (ParC == 1)
            {
                // Add the cylinder feature at the top
                Features.Add(new EyeFeature(feature));
            }
            if (ParA == 1)
            {
                // Add the cylinder feature at the mid right
                Brep temp = (Brep)feature.Clone();
                temp.Rotate(Math.PI / 2, Vector3D.AxisX, rotationCenter);
                Features.Add(new EyeFeature(temp));
            }
            if (ParD == 1)
            {
                // Add the cylinder feature at the bottom
                Brep temp = (Brep)feature.Clone();
                temp.Rotate(Math.PI, Vector3D.AxisX, rotationCenter);
                Features.Add(new EyeFeature(temp));
            }
            if (ParB == 1)
            {
                // Add the cylinder feature at the mid left
                Brep temp = (Brep)feature.Clone();
                temp.Rotate(Math.PI * 3 / 2, Vector3D.AxisX, rotationCenter);
                Features.Add(new EyeFeature(temp));
            }

            return true;
        }

        //
        //  La macro tipo INTxx viene creata nel caso INIZIALE se piano C,
        //  INIZIALE ALA ALTA se piani A/B
        //  I 4 casi possibili vengono ottenuti dalla gestione delle variabili:
        //
        //  mirrorInizialeFinale -> specula da INIZIALE a FINALE
        //  mirrorSideASideB -> specula da lato A a lato B
        //  mirrorAltoBasso -> specula da posizione ala ALTA a ala BASSA (solo per macro SAAxx)
        //
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

            //  Il profilo R richiede una gestione a parte
            if (CodePrf == "R")
                return CreateMacroR();

            ///////////////////////////////
            //      CORPO: variabile
            ///////////////////////////////
            double radius = ParC / 2;
            Brep feature = null;
            // Check which is the side
            string extrusionPlane = Side;

            double offsetY = 0;

            if (CodePrf == "I" && Side != "C")
            {
                offsetY = SB / 2;
            }

            // Find the centre of the circle in the XY plane
            Point2D centre = CodePrf == "Q" && (Side == "A" || Side == "B") ? new Point2D(ParA, (SB - ParB) + offsetY) : new Point2D(ParA, ParB + offsetY);

            double extrusionDepth = 0;
            if (extrusionPlane == "A" || extrusionPlane == "B" && CodePrf != "L")
                extrusionDepth = TB + Radius;
            else
                extrusionDepth = TA + Radius;

            if (radius > 0)
            {
                if (CodePrf == "Q" && extrusionPlane == "C")
                {
                    if (ParG == 0 || ParG == 2)
                    {
                        EyeGeometryUtils.AddCircleExtrusion(centre, radius, "C", extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);
                        Features.Add(new EyeFeature(feature));
                    }
                    if (ParG == 1 || ParG == 2)
                    {
                        EyeGeometryUtils.AddCircleExtrusion(centre, radius, "D", extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);

                        Features.Add(new EyeFeature(feature));
                    }
                }
                else
                {
                    EyeGeometryUtils.AddCircleExtrusion(centre, radius, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref feature, Surplus);

                    Features.Add(new EyeFeature(feature));
                }
            }

            return true;
        }
    }
}
