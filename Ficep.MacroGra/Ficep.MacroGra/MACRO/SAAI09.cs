using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;

namespace Ficep.MacroGra
{
    public class SAAI09 : EyeMacro
    {

        public SAAI09(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            //  Centro della base del cilindro che descrive il contorno da estrudere
            //
            Point2D centre = new Point2D(ParA, SB + ParB);
            //
            //  ESTRUSIONE
            //
            double extrusionDepth = TB + Radius;
            string extrusionPlane = Side;
            Brep brep = null;

            if (ParR > 0)
                EyeGeometryUtils.AddCircleExtrusion(centre, ParR, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref brep, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.Add(new EyeFeature(brep));

            return true;
        }

    }
}
