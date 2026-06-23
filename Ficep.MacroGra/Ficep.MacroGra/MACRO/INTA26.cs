using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;

namespace Ficep.MacroGra
{
    public class INTA26 : EyeMacro
    {
        public INTA26(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "L";
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

            double extrusionDepth = Side == "A" ? TA : TB;
            string extrusionPlane = Side;

            Point2D centre;

            double counterX = 0;
            while (counterX <= ParG)
            {
                double counterY = 0;
                while (counterY <= ParH)
                {
                    centre = new Point2D(ParA + ParE * counterX, ParB + ParF * counterY);
                    EyeGeometryUtils.AddSlotExtrusion(centre, ParC - 2 * ParR, ParR, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolAngle, ref brep, Surplus);
                    Features.Add(new EyeFeature(brep));

                    counterY += 1;
                }
                counterX += 1;
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            return true;
        }

    }
}