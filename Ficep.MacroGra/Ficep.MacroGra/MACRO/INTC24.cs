using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;

namespace Ficep.MacroGra
{
    public class INTC24 : EyeMacro
    {
        public INTC24(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "U";
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
            Brep brep = null;

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            Point2D centre = new Point2D(ParA + ParC / 2, ParB);

            //
            // Estrusione anima
            //

            EyeGeometryUtils.AddSlotExtrusion(centre, ParC, ParR, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolAngle, ref brep, Surplus);
            Features.Add(new EyeFeature(brep));

            centre.X += MacroName == "INTC24" ? - ParC / 2 : ParC / 2;

            EyeGeometryUtils.AddCircleExtrusion(centre, ParD, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref brep, Surplus);
            Features.Add(new EyeFeature(brep));
            
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            return true;
        }

    }
}