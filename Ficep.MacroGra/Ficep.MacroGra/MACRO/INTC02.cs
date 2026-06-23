using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
	public class INTC02 : EyeMacro
	{

		public INTC02(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQFR";
        }
        public bool CreateMacroR()
        {
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            List<Brep> breps = new List<Brep>();

            double totalLenght = ParF + ParR,
                   width = ParG,
                   innerRadius = SA / 2 - TA,
                   outerRadius = SA / 2;

            if (ParC == 1)
            {
                EyeUtils.CreateRadialAperture(width, totalLenght, 0, innerRadius, outerRadius, TolBrep, false, out breps, ParR, ParE, Surplus);
                Features.Add(new EyeFeature(breps[0]));

            }
            if (ParA == 1)
            {
                EyeUtils.CreateRadialAperture(width, totalLenght, 90, innerRadius, outerRadius, TolBrep, false, out breps, ParR, ParE, Surplus);
                Features.Add(new EyeFeature(breps[0]));
            }
            if (ParD == 1)
            {
                EyeUtils.CreateRadialAperture(width, totalLenght, 180, innerRadius, outerRadius, TolBrep, false, out breps, ParR, ParE, Surplus);
                Features.Add(new EyeFeature(breps[0]));
            }
            if (ParB == 1)
            {
                EyeUtils.CreateRadialAperture(width, totalLenght, 270, innerRadius, outerRadius, TolBrep, false, out breps, ParR, ParE, Surplus);
                Features.Add(new EyeFeature(breps[0]));
            }

            return true;
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

            //  Il profilo R richiede una gestione a parte
            if (CodePrf == "R")
                return CreateMacroR();

            ///////////////////////////////
            //      CORPO: variabile
            ///////////////////////////////
            Brep feature = null;

            List<ICurve> curves = new List<ICurve>();

            CompositeCurve roundedRectangle = CompositeCurve.CreateRoundedRectangle(Plane.XY, ParC, ParD, ParR, true);
            double alfar = ParALFA * Math.PI / 180;

            //
            //  L'angolo di rotazione viene invertito di segno perchè
            //  non deve seguire il mirroring ma deve essere sempre
            //  visto positivo antiorario con l'osservatore che guarda
            //  il piano dall'esterno
            //
            if (MirrorSideASideB != MirrorAltoBasso)
                alfar = -alfar;
            
            roundedRectangle.Rotate(alfar, Vector3D.AxisZ);
            roundedRectangle.Translate(ParA, ParB);
            curves.Add(roundedRectangle.CurveList[0]);
            curves.Add(roundedRectangle.CurveList[2]);
            curves.Add(roundedRectangle.CurveList[1]);
            curves.Add(roundedRectangle.CurveList[4]);
            curves.Add(roundedRectangle.CurveList[3]);
            curves.Add(roundedRectangle.CurveList[6]);
            curves.Add(roundedRectangle.CurveList[5]);
            curves.Add(roundedRectangle.CurveList[7]);

            /////////////////////////////////////////////////////////
            //  Sostituire con EyeGeometryUtils.AddContourExtrusion
            //
            string extrusionPlane = Side;
            double offsetX = 0, offsetY = 0, offsetZ = 0;
            double extrusionDepth = 0;
            if (extrusionPlane == "A" || extrusionPlane == "B" && CodePrf != "L")
                extrusionDepth = TB + 2 * Surplus;
            else
                extrusionDepth = TA + 2 * Surplus;

            Vector3D amount = null;

            if (CodePrf == "Q" && extrusionPlane == "C")
            {
                if (ParG == 0 || ParG == 2)
                {
                    EyeGeometryUtils.GetSolidSubtractOffsetAmount(Wp, "C", extrusionDepth, TolBrep, TolAngle, Surplus, ref amount, ref offsetX, ref offsetY, ref offsetZ);
                    // Subtract the extruded solid to the rawpart
                    EyeUtils.SolidSubtract(curves, Wp, "C", amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso);

                    Features.Add(new EyeFeature(feature));
                }
                if (ParG == 1 || ParG == 2)
                {
                    EyeGeometryUtils.GetSolidSubtractOffsetAmount(Wp, "D", extrusionDepth, TolBrep, TolAngle, Surplus, ref amount, ref offsetX, ref offsetY, ref offsetZ);
                    // Subtract the extruded solid to the rawpart
                    EyeUtils.SolidSubtract(curves, Wp, "D", amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso);

                    Features.Add(new EyeFeature(feature));
                }
            }
            else
            {
                EyeGeometryUtils.GetSolidSubtractOffsetAmount(Wp, extrusionPlane, extrusionDepth, TolBrep, TolAngle, Surplus, ref amount, ref offsetX, ref offsetY, ref offsetZ);
                // Subtract the extruded solid to the rawpart
                EyeUtils.SolidSubtract(curves, Wp, extrusionPlane, amount, offsetX, offsetY, offsetZ, TolBrep, ref feature, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso);

                Features.Add(new EyeFeature(feature));
            }
            //////////////////////////////////////////////////

            return true;
        }
    }
}
