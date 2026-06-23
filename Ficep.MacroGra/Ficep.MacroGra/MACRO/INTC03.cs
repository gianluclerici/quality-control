
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;
using Ficep.Utils;
using devDept.Geometry;

namespace Ficep.MacroGra
{
	public class INTC03 : EyeMacro
	{

		public INTC03(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IQLUFR";
        }
        public bool CreateMacroR()
        {
            List<Brep> breps = new List<Brep>();

            double totalLenght = Params.F + 2 * Params.R,
                   innerRadius = Wp.Prf.SA / 2 - Wp.Prf.TA,
                   outerRadius = Wp.Prf.SA / 2;

            EyeUtils.CreateRadialSlotAperture(totalLenght, 0, Params.R, innerRadius, outerRadius, TolBrep, false, out breps, Params.E);
            if (Params.C == 1)
            {
                EyeUtils.CreateRadialSlotAperture(totalLenght, 0, Params.R, innerRadius, outerRadius, TolBrep, false, out breps, Params.E);
                foreach (Brep brep in breps)
                    Features.Add(new EyeFeature(brep));
            }
            if (Params.A == 1)
            {
                EyeUtils.CreateRadialSlotAperture(totalLenght, 90, Params.R, innerRadius, outerRadius, TolBrep, false, out breps, Params.E);
                foreach (Brep brep in breps)
                    Features.Add(new EyeFeature(brep));
            }
            if (Params.D == 1)
            {
                EyeUtils.CreateRadialSlotAperture(totalLenght, 180, Params.R, innerRadius, outerRadius, TolBrep, false, out breps, Params.E);
                foreach (Brep brep in breps)
                    Features.Add(new EyeFeature(brep));
            }
            if (Params.B == 1)
            {
                EyeUtils.CreateRadialSlotAperture(totalLenght, 270, Params.R, innerRadius, outerRadius, TolBrep, false, out breps, Params.E);
                Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
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
            
            double extrusionDepth;
            string extrusionPlane = Side;
            Brep brep = null;

            double offsetY = 0;

            if (CodePrf == "I" && Side != "C")
                offsetY = SB / 2;

            if (CodePrf == "Q" && (Side == "A" || Side == "B"))
                offsetY = SB - 2 * ParB;

            double radALFA = CodePrf == "L" && Side == "B" ? - ParALFA.ToRad() : ParALFA.ToRad();
            Point2D slotCentre = new Point2D(ParA, ParB + offsetY);


            if (extrusionPlane == "A" || extrusionPlane == "B" && CodePrf != "L")
                extrusionDepth = TB + Radius;
            else
                extrusionDepth = TA;

            if (CodePrf == "Q" && extrusionPlane == "C")
            {
                if (ParG == 0 || ParG == 2)
                {
                    EyeGeometryUtils.AddSlotExtrusion(slotCentre, ParC, ParR, "C", extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolAngle, ref brep, Surplus);
                    // If Alfa is different from 0 rotate the slot in the plane around the slot centre
                    if (!radALFA.IsEqualTo(0, TolAngle))
                        EyeGeometryUtils.RotateSolid(radALFA, ParA, ParB, "C", Wp, ref brep);

                    Features.Add(new EyeFeature(brep));
                }
                if (ParG == 1 || ParG == 2)
                {
                    EyeGeometryUtils.AddSlotExtrusion(slotCentre, ParC, ParR, "D", extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolAngle, ref brep, Surplus);
                    // If Alfa is different from 0 rotate the slot in the plane around the slot centre
                    if (!radALFA.IsEqualTo(0, TolAngle))
                        EyeGeometryUtils.RotateSolid(radALFA, ParA, ParB, "D", Wp, ref brep);

                    Features.Add(new EyeFeature(brep));
                }
            }
            else
            {
                EyeGeometryUtils.AddSlotExtrusion(slotCentre, ParC, ParR, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolAngle, ref brep, Surplus);
                // If Alfa is different from 0 rotate the slot in the plane around the slot centre
                if (!radALFA.IsEqualTo(0, TolAngle))
                    EyeGeometryUtils.RotateSolid(radALFA, slotCentre.X, MirrorAltoBasso ? (offsetY - ParB) : slotCentre.Y, extrusionPlane, Wp, ref brep);

                Features.Add(new EyeFeature(brep));
            }

            Features.Add(new EyeFeature(brep));

            return true;
        }
    }
}
